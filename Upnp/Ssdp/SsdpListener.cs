using System;
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
        protected SsdpSocketCollection Sockets
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
        /// Initializes a new instance of the <see cref="SsdpServer"/> class.
        /// </summary>
        public SsdpListener(params IPEndPoint[] endPoints)
            : this(endPoints.Select(ep => (ISsdpSocket)new SsdpSocket(ep)).ToArray())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpListener" /> class.
        /// </summary>
        /// <param name="sockets">The ssdp sockets.</param>
        public SsdpListener(ISsdpSocket[] sockets)
        {
            if (sockets.Length == 0)
                sockets = SsdpSocketFactory.BuildSockets().ToArray();

            this.Sockets = new SsdpSocketCollection(sockets);

            this.Sockets.ForEachSocket(socket => socket.SsdpMessageReceived += this.OnSsdpMessageReceived);
        }

        /// <summary>
        /// Starts listening on the specified remote endpoints.
        /// </summary>
        /// <param name="remoteEps">The remote eps.</param>
        public virtual void StartListening(params IPEndPoint[] remoteEps)
        {
            this.Sockets.StartListening(remoteEps);
        }

        ///// <summary>
        ///// Stops listening on the specified remote endpoints.
        ///// </summary>
        ///// <param name="remoteEps">The remote eps.</param>
        //public void StopListeningOn(params IPEndPoint[] remoteEps)
        //{
        //    // If nothing specified then just stop listening on all
        //    if (remoteEps == null || remoteEps.Length == 0)
        //    {
        //        this.StopListening();
        //        return;
        //    }

        //    ForEachSocket(socket =>
        //    {
        //        if (!socket.IsListening)
        //            return;

        //        // Drop all the multicast groups specified
        //        foreach (var ep in remoteEps.Where(ep => IPAddressHelpers.IsMulticast(ep.Address)))
        //        {
        //            try
        //            {
        //                socket.DropMulticastGroup(ep.Address);
        //            }
        //            catch (SocketException)
        //            {
        //                // If we're not part of this group then it will throw an error so just ignore it
        //            }
        //        }
        //    });
        //}

        /// <summary>
        /// Stops listening on all end points.
        /// </summary>
        public virtual void StopListening()
        {
            this.Sockets.StopListening();
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

            this.Sockets.Dispose();
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