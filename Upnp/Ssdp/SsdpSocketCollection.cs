using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using Upnp.Net;

namespace Upnp.Ssdp
{
    public class SsdpSocketCollection : Collection<ISsdpSocket>, IDisposable
    {
        #region Constructors

        public SsdpSocketCollection(IEnumerable<ISsdpSocket> sockets)
        {
            foreach (var socket in sockets)
                this.Add(socket);
        }

        #endregion

        #region Protected Overrides

        protected override void InsertItem(int index, ISsdpSocket item)
        {
            lock (this.Items)
            {
                base.InsertItem(index, item);
            }
        }

        protected override void ClearItems()
        {
            lock (this.Items)
            {
                base.ClearItems();
            }
        }

        protected override void RemoveItem(int index)
        {
            lock (this.Items)
            {
                base.RemoveItem(index);    
            }
        }

        protected override void SetItem(int index, ISsdpSocket item)
        {
            lock (this.Items)
            {
                base.SetItem(index, item);
            }
        }

        #endregion

        #region Public Helper Methods

        /// <summary>
        /// Gets a value indicating whether this instance is listening.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is listening; otherwise, <c>false</c>.
        /// </value>
        public bool IsListening
        {
            get { return this.Any(socket => socket.IsListening); }
        }

        /// <summary>
        /// Stops listening on all sockets.
        /// </summary>
        public virtual void StopListening()
        {
            ForEachSocket(socket =>
            {
                if (!socket.IsListening)
                    return;

                socket.StopListening();
            });
        }

        /// <summary>
        /// Starts listening on the specified remote endpoints.
        /// </summary>
        /// <param name="groups">The remote eps.</param>
        public virtual void StartListening(params IPAddress[] groups)
        {
            if (groups == null || groups.Length == 0)
                groups = new[] { Protocol.DiscoveryEndpoints.IPv4.Address };

            ForEachSocket(socket =>
            {
                if (!socket.IsListening)
                    socket.StartListening();

                /* TODO: might need to update this slightly. It was this way in SsdpSearch:
                 * 
                 *             if (!this.Server.EnableBroadcast)
                 *                  this.Server.EnableBroadcast = destinations.Any(ep => ep.Address.Equals(IPAddress.Broadcast));
                 * 
                 */
                socket.EnableBroadcast = true;

                Trace.WriteLine(string.Format("Listening on {0}", socket.LocalEndpoint), AppInfo.Application);
                // Join all the multicast groups specified
                foreach (var group in groups.Where(ep => ep.AddressFamily == socket.LocalEndpoint.AddressFamily && IPAddressHelpers.IsMulticast(ep)))
                {
                    socket.JoinMulticastGroup(group);
                    Trace.WriteLine(string.Format("Interface {0} joined igmp group {1}", socket.LocalEndpoint, group), AppInfo.Application);
                }
            });
        }

        public void ForEachSocket(Action<ISsdpSocket> action)
        {
            lock (this.Items)
            {
                foreach (var socket in this)
                    action(socket);
            }
        }

        #endregion

        #region IDisposable Implementation

        protected  virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            lock (this.Items)
            {
                ForEachSocket(socket => socket.Close()); 
   
                this.ClearItems();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
