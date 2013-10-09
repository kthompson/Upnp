using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Upnp.Net;

namespace Upnp.Ssdp
{
    public class SsdpListener : ISsdpListener
    {
        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        protected ISsdpSocket Server
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the ssdp message filter.
        /// </summary>
        /// <value>
        /// The filter.
        /// </value>
        public Func<SsdpMessage, bool> Filter { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpListener" /> class.
        /// </summary>
        public SsdpListener(ISsdpSocket socket = null)
        {
            this.Server = socket ?? new SsdpSocket(new IPEndPoint(IPAddress.Any, 1900));
            this.Server.SsdpMessageReceived += this.OnSsdpMessageReceived;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is listening.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is listening; otherwise, <c>false</c>.
        /// </value>
        public bool IsListening
        {
            get { return this.Server.IsListening; }
        }

        /// <summary>
        /// Starts listening on the specified remote endpoints.
        /// </summary>
        /// <param name="remoteEps">The remote eps.</param>
        public virtual void StartListening(params IPAddress[] remoteEps)
        {
            if (remoteEps == null || remoteEps.Length == 0)
                remoteEps = new[] { Protocol.DiscoveryEndpoints.IPv4 };

            lock (this.Server)
            {
                if (!this.Server.IsListening)
                    this.Server.StartListening();

                this.Server.EnableBroadcast = true;

                // Join all the multicast groups specified
                foreach (var ep in remoteEps.Where(IPAddressHelpers.IsMulticast))
                    this.Server.JoinMulticastGroupAllInterfaces(ep);
        }
        }

        /// <summary>
        /// Stops listening on the specified remote endpoints.
        /// </summary>
        /// <param name="remoteEps">The remote eps.</param>
        public void StopListeningOn(params IPEndPoint[] remoteEps)
        {
            // If nothing specified then just stop listening on all
            if (remoteEps == null || remoteEps.Length == 0)
            {
                this.StopListening();
                return;
            }

            lock (this.Server)
            {
                if (!this.Server.IsListening)
                    return;

                // Drop all the multicast groups specified
                foreach (IPEndPoint ep in remoteEps.Where(ep => IPAddressHelpers.IsMulticast(ep.Address)))
                {
                    try
                    {
                        this.Server.DropMulticastGroup(ep.Address);
                    }
                    catch (SocketException)
                    {
                        // If we're not part of this group then it will throw an error so just ignore it
                    }
                }
            }
        }

        /// <summary>
        /// Stops listening on all end points.
        /// </summary>
        public virtual void StopListening()
        {
            lock (this.Server)
            {
                if (!this.Server.IsListening)
                    return;

                this.Server.StopListening();
        }
        }

        #region Events

        /// <summary>
        /// Occurs when an SSDP message is received.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SsdpMessageReceived;

        /// <summary>
        /// Occurs when a service is found.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> ServiceFound;

        /// <summary>
        /// Occurs when a device is found.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> DeviceFound;

        /// <summary>
        /// Occurs when a root is found.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> RootFound;

        /// <summary>
        /// Occurs when SSDP alive received.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> Alive;

        /// <summary>
        /// Occurs when SSDP bye bye.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> ByeBye;

        /// <summary>
        /// Called when an SSDP message is received.
        /// </summary>
        /// <param name="sender"> </param>
        /// <param name="e">The message.</param>
        protected void OnSsdpMessageReceived(object sender, EventArgs<SsdpMessage> e)
        {
            var msg = e.Value;

            //examine our filter and if its not true then just return
            var filter = this.Filter;
            if (filter != null && !filter(msg))
                return;

            OnSsdpMessageReceived(msg);

            if (msg.IsService)
                this.OnServiceFound(msg);

            if (msg.IsDevice)
                this.OnDeviceFound(msg);

            if (msg.IsRoot)
                this.OnRootFound(msg);

            if (msg.IsAlive)
                this.OnAlive(msg);
            
            if (msg.IsByeBye)
                this.OnByeBye(msg);
        }

        protected virtual void OnSsdpMessageReceived(SsdpMessage msg)
        {
            var handler = this.SsdpMessageReceived;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(msg));
        }

        protected virtual void OnServiceFound(SsdpMessage msg)
        {
            var handler = this.ServiceFound;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(msg));
        }

        protected virtual void OnRootFound(SsdpMessage msg)
        {
            var handler = this.RootFound;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(msg));
        }

        protected virtual void OnDeviceFound(SsdpMessage msg)
        {
            var handler = this.DeviceFound;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(msg));
        }

        /// <summary>
        /// Occurs when SSDP alive received
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void OnAlive(SsdpMessage msg)
        {
            var handler = this.Alive;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(msg));
        }

        /// <summary>
        /// Occurs when SSDP bye bye
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void OnByeBye(SsdpMessage msg)
        {
            var handler = this.ByeBye;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(msg));
        }

        #endregion

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if(!disposing)
                return;

            this.Server.Dispose();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
