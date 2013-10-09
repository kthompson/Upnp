using System;
using System.Diagnostics;
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
        #region Constructors

        public SsdpSocket(int port)
            : base(new IPEndPoint(IPAddress.Any, port))
        {
        }

        public SsdpSocket(IPAddress addr = null, int port = 0)
            : base(new IPEndPoint(addr ?? IPAddress.Any, port))
        {
        }


        public SsdpSocket(IPEndPoint localEp)
            : base(localEp)
        {
        }

        #endregion

        public virtual void JoinMulticastGroupAllInterfaces(IPAddress group)
        {
            var localIps = IPAddressHelpers.GetUnicastAddresses(ip => ip.AddressFamily == group.AddressFamily);
            foreach (var addr in localIps)
            {
                try
                {
                    this.JoinMulticastGroup(group, addr);
                }
                catch (SocketException)
                {
                    // If we're already joined to this group we'll throw an error so just ignore it
                }
            }
        }

        protected override Socket CreateSocket(IPEndPoint localEp)
        {
            var sock = base.CreateSocket(localEp);
            sock.ReceiveBufferSize = 1024*1024; // one meg
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
                    var msg = SsdpMessage.Parse(args.Buffer, args.Length, args.LocalEndpoint, args.RemoteEndpoint);
                    this.OnSsdpMessageReceived(msg);
                }
                catch (ArgumentException ex)
                {
                    Trace.TraceError("Failed to parse SSDP response: {0}", ex.ToString());
                }
                catch (Exception exception)
                {
                    Trace.TraceError("Failed to parse SSDP response: {0}", exception.ToString());
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
