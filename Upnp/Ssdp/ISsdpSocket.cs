using System;
using System.Net;

namespace Upnp.Ssdp
{
    public interface ISsdpSocket : IDisposable
    {

        /// <summary>
        /// Occurs when an SSDP message is received.
        /// </summary>
        event EventHandler<EventArgs<SsdpMessage>> SsdpMessageReceived;

        /// <summary>
        /// Gets a value indicating whether this instance is listening.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is listening; otherwise, <c>false</c>.
        /// </value>
        bool IsListening
        {
            get;
        }

        /// <summary>
        /// Gets or sets a value indicating whether broadcast is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable broadcast]; otherwise, <c>false</c>.
        /// </value>
        bool EnableBroadcast { get; set; }

        /// <summary>
        /// Gets the local endpoint.
        /// </summary>
        IPEndPoint LocalEndpoint { get; }

        Uri Location { get; }


        void StartListening();
        void StopListening();

        void JoinMulticastGroup(IPAddress remoteEp);
        void DropMulticastGroup(IPAddress address);

        int Send(byte[] dgram, int bytes, IPEndPoint endPoint);
        int Send(byte[] dgram, int bytes, string hostname, int port);
        int Send(byte[] dgram, int bytes);

        void Close();
    }
}