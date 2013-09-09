using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using Upnp.Timers;

namespace Upnp.Ssdp
{
    /// <summary>
    /// Server class for sending out SSDP announcements and responding to searches
    /// </summary>
    public class SsdpServer : SsdpListener, ISsdpServer
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpServer"/> class.
        /// </summary>
        public SsdpServer(params ISsdpSocket[] sockets)
            : base(sockets)
        {
            this.Announcers = new Dictionary<ISsdpAnnouncer, bool>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates the announcer.
        /// </summary>
        /// <param name="respondToSearches">if set to <c>true</c> [respond to searches].</param>
        /// <returns></returns>
        public ISsdpAnnouncer CreateAnnouncer(bool respondToSearches = true)
        {
            lock (this.Announcers)
            {
                var announcer = new SsdpAnnouncer(this.Sockets.ToArray());
                this.Announcers.Add(announcer, respondToSearches);
                return announcer;
            }
        }

        public ISsdpAnnouncer CreateAnnouncer(string type, string usn, bool respondToSearches = true)
        {
            var ad = CreateAnnouncer(respondToSearches);
            ad.NotificationType = type;
            ad.USN = usn;
            
            return ad;
        }

        /// <summary>
        /// Removes the announcer.
        /// </summary>
        /// <param name="announcer">The announcer.</param>
        public void RemoveAnnouncer(ISsdpAnnouncer announcer)
        {
            lock (this.Announcers)
            {
                this.Announcers.Remove(announcer);
            }
        }

        /// <summary>
        /// Sets the search responses.
        /// </summary>
        /// <param name="announcer">The announcer.</param>
        /// <param name="respondToSearches">if set to <c>true</c> [respond to searches].</param>
        public void SetRespondsToSearches(ISsdpAnnouncer announcer, bool respondToSearches)
        {
            lock (this.Announcers)
            {
                if (!this.Announcers.ContainsKey(announcer))
                    throw new KeyNotFoundException();

                this.Announcers[announcer] = respondToSearches;
            }
        }

        /// <summary>
        /// Gets the matching responders.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <returns></returns>
        public IEnumerable<ISsdpAnnouncer> GetMatchingResponders(SsdpMessage msg)
        {
            lock (this.Announcers)
            {
                foreach (var pair in this.Announcers.Where(pair => pair.Value))
                {
                    if (msg.SearchType == Protocol.SsdpAll)
                        yield return pair.Key;

                    if (msg.SearchType.StartsWith("uuid:") && msg.SearchType == pair.Key.USN)
                        yield return pair.Key;

                    if (msg.SearchType == pair.Key.NotificationType)
                        yield return pair.Key;
                }
            }
        }

        public override void StopListening()
        {
            base.StopListening();

            lock (this.Announcers)
            {
                foreach (var announcer in Announcers.Keys)
                    announcer.Shutdown();

                Announcers.Clear();
            }
        }

        public override void StartListening(params IPAddress[] groups)
        {
            base.StartListening(groups);

            lock (this.Announcers)
            {
                foreach (var announcer in Announcers.Keys)
                    announcer.Start();
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Sends the search response message.
        /// </summary>
        protected void SendSearchResponse(SsdpMessage msg, ISsdpAnnouncer announcer)
        {
            this.Sockets.ForEachSocket(socket =>
            {
                // If we were stopped then don't bother sending this message
                if (!socket.IsListening)
                    return;

                var aliveResponse = Protocol.CreateAliveResponse(socket.Location.ToString(), msg.SearchType, announcer.USN, announcer.MaxAge, Protocol.DefaultUserAgent);
                var bytes = Encoding.ASCII.GetBytes(aliveResponse); 

                Trace.WriteLine(string.Format("Sending SearchResponse [{0}, {1}] from {2} to {3}", msg.SearchType, msg.USN, socket.LocalEndpoint, msg.Source), AppInfo.Application);
                socket.Send(bytes, bytes.Length, msg.Source);
            });
        }

        #endregion

        #region Events

        protected override void OnSsdpMessageReceived(SsdpMessage msg)
        {
            base.OnSsdpMessageReceived(msg);
            
            // Ignore any advertisements
            if (msg.IsAdvertisement)
                return;

            // Set up our dispatcher to send the response to each matching announcer that supports responding
            foreach (var announcer in this.GetMatchingResponders(msg))
            {
                var temp = announcer;
                Dispatcher.Add(() => this.SendSearchResponse(msg, temp), TimeSpan.FromSeconds(new Random().Next(0, msg.MaxAge)));
            }
        }

        #endregion

        #region Properties

        protected static readonly TimeoutDispatcher Dispatcher = new TimeoutDispatcher();

        protected Dictionary<ISsdpAnnouncer, bool> Announcers
        {
            get;
            private set;
        }

        public bool IsListening
        {
            get
            {
                if (this.Sockets.Count == 0)
                    return false;

                return this.Sockets.First().IsListening;
            }
        }

        #endregion

        #region IDisposable Implementation
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this.Announcers)
                {
                    foreach (var announcer in this.Announcers.Keys)
                        announcer.Dispose();

                    this.Announcers.Clear();
                }
            }

            base.Dispose(disposing);
        }
        #endregion

    }
}
