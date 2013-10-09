using System.Net;

namespace Upnp.Net
{
    /// <summary>
    /// Class that represents data transferred over the network
    /// </summary>
    public class NetworkData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkData"/> class.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="localEp">the local endpoint</param>
        /// <param name="remoteEp">The remote ep.</param>
        public NetworkData(byte[] buffer, IPEndPoint localEp, IPEndPoint remoteEp)
            : this(buffer, buffer.Length, localEp, remoteEp)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkData"/> class.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="length">The length.</param>
        /// <param name="localEp"></param>
        /// <param name="remoteEp">The remote ep.</param>
        public NetworkData(byte[] buffer, int length, IPEndPoint localEp, IPEndPoint remoteEp)
        {
            this.Buffer = buffer;
            this.Length = length;
            this.LocalEndpoint = localEp;
            this.RemoteEndpoint = remoteEp;
        }

        /// <summary>
        /// Gets or sets the buffer.
        /// </summary>
        /// <value>
        /// The buffer.
        /// </value>
        public byte[] Buffer
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        public int Length
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the remote endpoint.
        /// </summary>
        /// <value>
        /// The remote endpoint.
        /// </value>
        public IPEndPoint LocalEndpoint
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the remote IP endpoint.
        /// </summary>
        public IPEndPoint RemoteEndpoint
        {
            get;
            protected set;
        }
    }
}
