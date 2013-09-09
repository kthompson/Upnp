using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Upnp.Collections;
using Upnp.Timers;

namespace Upnp.Ssdp
{
    /// <summary>
    /// Class for announcing SSDP messages (Alive/ByeByes)
    /// </summary>
    class SsdpAnnouncer : ISsdpAnnouncer
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpAnnouncer" /> class.
        /// </summary>
        /// <param name="sockets">The sockets.</param>
        public SsdpAnnouncer(params ISsdpSocket[] sockets)
        {
            this.Sockets = sockets;
            this.UserAgent = Protocol.DefaultUserAgent;
            this.MaxAge = Protocol.DefaultMaxAge;
            this.NotificationType = string.Empty;
            this.USN = string.Empty;
            this.RemoteEndPoints = new SyncCollection<IPEndPoint>
                {
                    new IPEndPoint(Protocol.DiscoveryEndpoints.IPv4, Protocol.DefaultPort), 
                    new IPEndPoint(Protocol.DiscoveryEndpoints.Broadcast, Protocol.DefaultPort)
                };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            lock (this.SyncRoot)
            {
                // If we're already running then ignore this request
                if (this.IsRunning)
                    return;

                /* UPnP Spec 1.1.2: Devices SHOULD wait a random interval (e.g. between 0 and 100milliseconds) before sending an initial
                 * set of advertisements in order to reduce the likelihood of network storms; this random interval SHOULD also be applied 
                 * on occasions where the device obtains a new IP address or a new UPnP-enabled interface is installed.
                 * 
                 * Due to the unreliable nature of UDP, devices SHOULD send the entire set of discovery messages more than once with some 
                 * delay between sets e.g. a few hundred milliseconds.
                 */
                var random = new Random();

                this.TimeoutTokens.Add(Dispatcher.Add(() =>
                {
                    this.SendSyncAliveMessage();
                        
                    // add second advert for 200ms from now:
                    lock (this.SyncRoot)
                    {
                        this.TimeoutTokens.Add(Dispatcher.Add(() => this.SendSyncAliveMessage(), TimeSpan.FromMilliseconds(200)));
                    }
                }, TimeSpan.FromMilliseconds(random.Next(0, 100))));
                

                StartAnnouncer();
            }
        }

        private void StartAnnouncer()
        {
            lock (this.SyncRoot)
            {
                // Create a new timeout to send out SSDP alive messages
                // Also make sure we kick the first one off semi-instantly
                this.TimeoutTokens.Add(Dispatcher.Add(() =>
                {
                    if (!SendSyncAliveMessage())
                    {
                        Trace.WriteLine(string.Format("Stopping Dispatcher for {0}", this.USN), AppInfo.Application);
                        return;
                    }

                    
                    StartAnnouncer();
                }, GetNextAdvertWaitTime()));
            }
        }

        private bool SendSyncAliveMessage()
        {
            lock (this.SyncRoot)
            {
                if (!this.IsRunning)
                    return false;

                this.SendAliveMessage();
            }

            return true;
        }

        private TimeSpan GetNextAdvertWaitTime()
        {
            /* UPnP Spec 1.1.2: In addition, the device MUST re-send its advertisements periodically prior to expiration of the duration specified in
             * the CACHE-CONTROL header field; it is RECOMMENDED that such refreshing of advertisements be done at a randomly-distributed interval of 
             * less than one-half of the advertisement expiration time, so as to provide the opportunity for recovery from lost advertisements before 
             * the advertisement expires, and to distribute over time the advertisement refreshment of multiple devices on the network in order to 
             * avoid spikes in network traffic.
             */
            return TimeSpan.FromSeconds(new Random().Next(this.MaxAge/3, this.MaxAge/2));
        }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public void Shutdown()
        {
            lock (this.SyncRoot)
            {
                // If we're already running then ignore this request
                if (!this.IsRunning)
                    return;

                // Kill our timeout token
                foreach (var token in this.TimeoutTokens)
                    token.Dispose();

                this.TimeoutTokens.Clear();
                this.TimeoutTokens = null;

                // Now send our bye bye message
                this.SendByeByeMessage();
            }
        }

        /// <summary>
        /// Sends the alive message.
        /// </summary>
        public void SendAliveMessage()
        {
            ForEachRemoteEndPoint((ep, socket) =>
            {
                Trace.WriteLine(string.Format("DeviceAlive [{0}, {1}] from {2} to {3}", this.NotificationType, this.USN, socket.LocalEndpoint, ep), AppInfo.Application);

                var notify = Protocol.CreateAliveNotify(ep, socket.Location.ToString(), this.NotificationType, this.USN, this.MaxAge, this.UserAgent);
                var bytes = Encoding.ASCII.GetBytes(notify);

                socket.Send(bytes, bytes.Length, ep);
            });
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Fors the each remote end point.
        /// </summary>
        /// <param name="action">The action.</param>
        protected void ForEachRemoteEndPoint(Action<IPEndPoint, ISsdpSocket> action)
        {
            Parallel.ForEach(this.RemoteEndPoints.ToArray(), ep =>
            {
                foreach (var socket in Sockets)
                {
                    var localEp = socket.LocalEndpoint;
                    if (localEp == null || localEp.AddressFamily != ep.AddressFamily)
                        continue;

                    action(ep, socket);
                }
            });
        }

        /// <summary>
        /// Sends the bye bye message.
        /// </summary>
        protected void SendByeByeMessage()
        {
            ForEachRemoteEndPoint((ep, socket) =>
            {
                var bytes = Encoding.ASCII.GetBytes(Protocol.CreateByeByeNotify(ep, this.NotificationType, this.USN));
                Trace.WriteLine(string.Format("DeviceByeBye [{0}, {1}] from {2} to {3}", this.NotificationType, this.USN, socket.LocalEndpoint, ep), AppInfo.Application);
                socket.Send(bytes, bytes.Length, ep);
            });
        }
        
        #endregion
        
        #region Properties

        protected readonly object SyncRoot = new object();
        protected static readonly TimeoutDispatcher Dispatcher = new TimeoutDispatcher();
        protected List<IDisposable> TimeoutTokens = new List<IDisposable>();

        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        protected ISsdpSocket[] Sockets
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning
        {
            get { return this.TimeoutTokens != null && this.TimeoutTokens.Count > 0; }
        }

        /// <summary>
        /// Gets the remote end points.
        /// </summary>
        public SyncCollection<IPEndPoint> RemoteEndPoints
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the type of the notification.
        /// </summary>
        /// <value>
        /// The type of the notification.
        /// </value>
        public string NotificationType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the USN.
        /// </summary>
        /// <value>
        /// The USN.
        /// </value>
        public string USN
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the max age.
        /// </summary>
        /// <value>
        /// The max age.
        /// </value>
        public ushort MaxAge
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        /// <value>
        /// The user agent.
        /// </value>
        public string UserAgent
        {
            get;
            set;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Shutdown();
        }

        #endregion

    }

}
