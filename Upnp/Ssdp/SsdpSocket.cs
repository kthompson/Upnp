using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Upnp.Net;

namespace Upnp.Ssdp
{
    public class SsdpSocket : UdpServer, ISsdpSocket
    {
        public Uri Location { get; private set; }

        #region Constructors

        public SsdpSocket(IPAddress addr, Uri location = null)
            : this(new IPEndPoint(addr, Protocol.DefaultPort), location)
        {
        }


        public SsdpSocket(IPEndPoint localEp, Uri location = null)
            : base(localEp)
        {
            this.Location = location ?? new UriBuilder { Scheme = "http", Host = localEp.Address.ToString(), Path = "device.xml"}.Uri;
        }

        #endregion

        public new void JoinMulticastGroup(IPAddress addr)
        {
            try
            {
                this.JoinMulticastGroup(addr, this.LocalEndpoint.Address);
            }
            catch (SocketException)
            {
                // If we're already joined to this group we'll throw an error so just ignore it
            }
        }

        protected override Socket CreateSocket(IPEndPoint localEp)
        {
            var sock = base.CreateSocket(localEp);
            sock.ReceiveBufferSize = 4096;
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            return sock;
        }

        protected override void OnDataReceived(NetworkData args)
        {
            base.OnDataReceived(args);

            // Queue this response to be processed
            ThreadPool.QueueUserWorkItem(data =>
            {
                try
                {
                    // Parse our message and fire our event
                    var msg = SsdpMessage.Parse(args.Buffer, args.Length, args.RemoteIPEndpoint);
                    this.OnSsdpMessageReceived(msg);
                }
                catch (ArgumentException ex)
                {
                    System.Diagnostics.Trace.TraceError("Failed to parse SSDP response: {0}", ex.ToString());
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.TraceError("Failed to parse SSDP response: {0}", exception.ToString());
                }
            });

        }

        #region Events

        /// <summary>
        /// Occurs when an SSDP message is received.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SsdpMessageReceived;

        /// <summary>
        /// Called when an SSDP message is received.
        /// </summary>
        /// <param name="msg">The message.</param>
        protected virtual void OnSsdpMessageReceived(SsdpMessage msg)
        {
            var handler = this.SsdpMessageReceived;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(msg));
        }

        #endregion
    }
}
