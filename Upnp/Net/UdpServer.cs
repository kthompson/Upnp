using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Upnp.Net
{
    /// <summary>
    /// UDP Server designed for listening and eventing all data received
    /// </summary>
    public class UdpServer : UdpClient
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServer"/> class.
        /// </summary>
        /// <param name="localEp">The local ep.</param>
        public UdpServer(IPEndPoint localEp)
            : base(localEp.AddressFamily)
        {
            this.Client = CreateSocket(localEp);
            this.Client.Bind(localEp);
            this.Buffer = new byte[0x10000];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts listening.
        /// </summary>
        public void StartListening()
        {
            using (this.GetReadLock())
            {
                if (this.IsListening)
                    return;

                this.IsListening = true;
                this.BeginReceive();
            }
        }

        /// <summary>
        /// Stops listening.
        /// </summary>
        public void StopListening()
        {
            using (this.GetWriteLock())
            {
                if (!this.IsListening)
                    return;

                this.IsListening = false;
            }
        }

        #endregion

        #region Events

        public event EventHandler<EventArgs<NetworkData>> DataReceived;

        /// <summary>
        /// Called when [data received].
        /// </summary>
        /// <param name="args">The args.</param>
        protected virtual void OnDataReceived(NetworkData args)
        {
            var handler = this.DataReceived;
            if (handler != null)
                handler(this, new EventArgs<NetworkData>(args));
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Creates the socket.
        /// </summary>
        /// <param name="localEp">The local ep.</param>
        /// <returns></returns>
        protected virtual Socket CreateSocket(IPEndPoint localEp)
        {
            return new Socket(localEp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        /// <summary>
        /// Begins receiving.
        /// </summary>
        protected void BeginReceive()
        {
            if (this.LocalEndpoint == null)
                return;

            EndPoint remoteEp = new IPEndPoint((this.LocalEndpoint.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any), 0);
            var flags = SocketFlags.None;
            this.Client.BeginReceiveMessageFrom(this.Buffer, 0, this.Buffer.Length, flags, ref remoteEp, ar =>
            {
                IPEndPoint localEp;
                byte[] buffer;

                using (this.GetReadLock())
                {
                    // If the server is stopped then return now
                    if (!this.IsListening)
                        return;
                    try
                    {
                        // Complete our receive by getting the data
                        IPPacketInformation packetInfo;
                        int size = this.Client.EndReceiveMessageFrom(ar, ref flags, ref remoteEp, out packetInfo);
                        localEp = new IPEndPoint(packetInfo.Address, this.LocalEndpoint.Port);

                        var unicast = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().Select(iface =>
                            {
                                var props = iface.GetIPProperties();
                                if (props == null)
                                    return null;

                                var ipv4 = props.GetIPv4Properties();
                                if (ipv4 != null && ipv4.Index == packetInfo.Interface)
                                    return props.UnicastAddresses.FirstOrDefault((addr) => addr.Address.AddressFamily == AddressFamily.InterNetwork);

                                var ipv6 = props.GetIPv6Properties();
                                if (ipv6 != null && ipv6.Index == packetInfo.Interface)
                                    return props.UnicastAddresses.FirstOrDefault((addr) => addr.Address.AddressFamily == AddressFamily.InterNetworkV6);

                                return null;
                            }).FirstOrDefault((result) => result != null);

                        if (unicast != null)
                            localEp.Address = unicast.Address;

                        // Copy the data out of the shared buffer into a buffer just for this data
                        buffer = new byte[size];
                        Array.Copy(this.Buffer, buffer, size);
                    }

                    catch (SocketException)
                    {
                        // An error occurred while receiving the data so stop receiving
                        return;
                    }
                    catch (ObjectDisposedException)
                    {
                        // Socket was closed/disposed so stop listening
                        return;
                    }

                    // Continue receiving data
                    this.BeginReceive();
                }

                // Send our event forward
                this.OnDataReceived(new NetworkData(buffer, localEp, remoteEp as IPEndPoint));

            }, null);
        }

        #endregion

        #region Locking
                
        private readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Gets the write lock.
        /// </summary>
        /// <returns></returns>
        protected IDisposable GetWriteLock()
        {
            return new Disposable(() => Lock.EnterWriteLock(), () => Lock.ExitWriteLock());
        }

        /// <summary>
        /// Gets the read lock.
        /// </summary>
        /// <returns></returns>
        protected IDisposable GetReadLock()
        {
            return new Disposable(() => Lock.EnterReadLock(), () => Lock.ExitReadLock());
        }

        #endregion

        #region Properties

        protected byte[] Buffer
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is listening.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is listening; otherwise, <c>false</c>.
        /// </value>
        public bool IsListening
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the local endpoint.
        /// </summary>
        public IPEndPoint LocalEndpoint
        {
            get
            {
                if (this.Client == null)
                    return null;

                return this.Client.LocalEndPoint as IPEndPoint;
            }
        }

        /// <summary>
        /// Gets or sets the multicast TTL.
        /// </summary>
        /// <value>
        /// The multicast TTL.
        /// </value>
        public short MulticastTtl
        {
            get
            {
                return (short)this.Client.GetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive);
            }
            set
            {
                this.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether address re-use is allowed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if address re-use is allowed; otherwise, <c>false</c>.
        /// </value>
        public bool ReuseAddress
        {
            get
            {
                return ((int)this.Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress) == 1);
            }
            set
            {
                this.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, (value ? 1 : 0));
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Net.Sockets.UdpClient"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                this.StopListening();
            }
        }

        #endregion

    }
        
}
