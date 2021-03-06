﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using Upnp.Net;
using Upnp.Timers;

namespace Upnp.Ssdp
{
    /// <summary>
    /// Class representing an SSDP Search
    /// </summary>
    internal class SsdpSearch : ISsdpSearch
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpSearch"/> class.
        /// </summary>
        public SsdpSearch(ISsdpSocket socket = null)
        {
            if (socket == null)
            {
                socket = new SsdpSocket();
                this.OwnsServer = true;
            }

            this.Server = socket;
            this.HostEndpoint = Protocol.DiscoveryEndpoints.IPv4;
            this.SearchType = Protocol.SsdpAll;
            this.Mx = Protocol.DefaultMx;
        }

        protected bool OwnsServer { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Finds the first result.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <returns></returns>
        public SsdpMessage FindFirst(params IPEndPoint[] destinations)
        {
            object syncRoot = new object();
            SsdpMessage result = null;
            EventHandler<EventArgs<SsdpMessage>> resultHandler = null;

            // Create our handler to make all the magic happen
            resultHandler = (sender, e) =>
                {
                    lock (syncRoot)
                    {
                        // If we already got our first result then ignore this
                        if (result != null)
                            return;

                        // This is our first result so set our value, remove the handler, and cancel the search
                        result = e.Value;
                        this.ResultFound -= resultHandler;
                        this.CancelSearch();
                    }
                };

            try
            {
                lock (this.SearchLock)
                {
                    // Add our handler and start the async search
                    this.ResultFound += resultHandler;
                    this.SearchAsync(destinations);
                }

                // Wait until our search is complete
                this.WaitForSearch();
            }
            finally
            {
                // Make sure we remove our handler when we're done
                this.ResultFound -= resultHandler;
            }

            return result;
        }

        /// <summary>
        /// Searches the specified destinations.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <returns></returns>
        public List<SsdpMessage> Search(params IPEndPoint[] destinations)
        {
            var results = new List<SsdpMessage>();
            EventHandler<EventArgs<SsdpMessage>> resultHandler = (sender, e) =>
            {
                lock (results)
                {
                    results.Add(e.Value);
                }
            };

            EventHandler completeHandler = (sender, e) =>
            {
                lock (results)
                {
                    Monitor.PulseAll(results);
                }
            };

            try
            {
                lock (this.SearchLock)
                {
                    // Add our handlers and start the async search
                    this.ResultFound += resultHandler;
                    this.SearchComplete += completeHandler;
                    this.SearchAsync(destinations);
                }

                // Wait until our search is complete
                lock (results)
                {
                    Monitor.Wait(results);
                }

                // Return the results
                return results;
            }
            finally
            {
                // Make sure we remove our handlers when we're done
                this.ResultFound -= resultHandler;
                this.SearchComplete -= completeHandler;
            }
        }

        public void SearchAsync(params IPEndPoint[] destinations)
        {
            lock (this.SearchLock)
            {
                // If we're already searching then this is not allowed so throw an error
                if (this.IsSearching)
                    throw new InvalidOperationException("Search is already in progress.");

                this.IsSearching = true;
                this.Server.SsdpMessageReceived += OnSsdpMessageReceived;

                // TODO: Come up with a good calculation for this
                // Just double our mx value for the timeout for now
                this.CreateSearchTimeout(TimeSpan.FromSeconds(this.Mx*2));
            }

            // If no destinations were specified then default to the IPv4 discovery
            if (destinations == null || destinations.Length == 0)
                destinations = new[] {Protocol.DiscoveryEndpoints.IPv4};
            
            // Start the server and join our igmp groups
            this.Server.StartListening();

            // Do we really need to join the multicast group to send out multicast messages? Seems that way...
            foreach (IPEndPoint dest in destinations.Where(ep => IPAddressHelpers.IsMulticast(ep.Address)))
                this.Server.JoinMulticastGroupAllInterfaces(dest.Address);

            // If we're sending out any searches to the broadcast address then be sure to enable broadcasts
            if (!this.Server.EnableBroadcast)
                this.Server.EnableBroadcast = destinations.Any(ep => ep.Address.Equals(IPAddress.Broadcast));


            // Now send out our search data
            foreach (var dest in destinations)
            {
                // Make sure we respect our option as to whether we use the destination as the host value
                var host = (this.UseRemoteEndpointAsHost ? dest : this.HostEndpoint);
                var req = Protocol.CreateSearchRequest(host, this.SearchType, this.Mx);
                var bytes = Encoding.ASCII.GetBytes(req);
                var dest1 = dest;

                // TODO: Should we make this configurable?
                // NOTE: It's recommended to send two searches

                Trace.WriteLine(string.Format("Sending ssdp:discover [{0}, {1}] from {2} to {3}", this.SearchType, this.Mx, this.Server.LocalEndpoint, dest1), AppInfo.Application);
                this.Server.Send(bytes, bytes.Length, dest1);
                this.Server.Send(bytes, bytes.Length, dest1);
            }
        }

        /// <summary>
        /// Cancels the search.
        /// </summary>
        public void CancelSearch()
        {
            lock (this.SearchLock)
            {
                // If we're not searching then nothing to do here
                if (!this.IsSearching)
                    return;

                // If we were called from the timeout then this will be null
                if (this.CurrentSearchTimeout != null)
                {
                    this.CurrentSearchTimeout.Dispose();
                    this.CurrentSearchTimeout = null;
                }

                // Cleanup our handler and notify everyone that we're done
                this.Server.SsdpMessageReceived -= OnSsdpMessageReceived;

                this.IsSearching = false;
                this.OnSearchComplete();
            }
        }

        /// <summary>
        /// Waits for the current search to complete.
        /// </summary>
        public void WaitForSearch()
        {
            // Create an object for signaling and an event handler to signal it
            object signal = new object();
            EventHandler handler = (sender, args) =>
                {
                    lock (signal)
                    {
                        Monitor.Pulse(signal);
                    }
                };

            try
            {
                lock (signal)
                {
                    lock (this.SearchLock)
                    {
                        // If we're not searching then nothing to do here
                        if (!this.IsSearching)
                            return;

                        // Attach our handler
                        this.SearchComplete += handler;
                    }

                    // Wait for our event handler to trigger our signal
                    Monitor.Wait(signal);
                }
            }
            finally
            {
                // Make sure to remove our handler
                this.SearchComplete -= handler;
            }
        }

        #endregion

        #region Protected Methods

        protected void CreateSearchTimeout(TimeSpan timeout)
        {
            lock (this.SearchLock)
            {
                this.CurrentSearchTimeout = Dispatcher.Add(() =>
                    {
                        lock (this.SearchLock)
                        {
                            // Search may have already been cancelled
                            if (!this.IsSearching)
                                return;

                            // Make sure we remove ourself before calling CancelSearch
                            this.CurrentSearchTimeout = null;

                            // Cancel search will clean up everything
                            // Seems kind of wrong but it does exactly what we want
                            // If we add a special cancel event or something then we'll want to change this
                            this.CancelSearch();
                        }
                    }, timeout);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Called when [server data received].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs{T}.Core.Net.NetworkData&gt;"/> instance containing the event data.</param>
        protected virtual void OnSsdpMessageReceived(object sender, EventArgs<SsdpMessage> e)
        {
            // Queue this response to be processed
            ThreadPool.QueueUserWorkItem(data =>
                {
                    try
                    {
                        this.OnResultFound(e.Value);
                    }
                    catch (ArgumentException ex)
                    {
                        Trace.TraceError("Failed to parse SSDP response: {0}", ex.ToString());
                    }
                });
        }

        /// <summary>
        /// Occurs when [search complete].
        /// </summary>
        public event EventHandler SearchComplete;

        /// <summary>
        /// Called when [search complete].
        /// </summary>
        protected virtual void OnSearchComplete()
        {
            var handler = this.SearchComplete;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when [result found].
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> ResultFound;

        /// <summary>
        /// Called when [result found].
        /// </summary>
        /// <param name="result">The result.</param>
        protected virtual void OnResultFound(SsdpMessage result)
        {
            // This is a search so ignore any advertisements we get
            if (result.IsAdvertisement)
                return;

            // If this is not a notify message then ignore it
            if (result.IsRequest)
                return;

            // Check to make sure this message matches our filter
            var filter = this.Filter;
            if (!filter(result))
                return;

            var handler = this.ResultFound;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(result));
        }

        #endregion

        #region Properties

        protected static readonly TimeoutDispatcher Dispatcher = new TimeoutDispatcher();
        protected readonly object SearchLock = new object();

        /// <summary>
        /// Gets or sets the current search timeout.
        /// </summary>
        /// <value>
        /// The current search timeout.
        /// </value>
        protected IDisposable CurrentSearchTimeout { get; set; }

        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        protected ISsdpSocket Server { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is searching.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is searching; otherwise, <c>false</c>.
        /// </value>
        public bool IsSearching { get; protected set; }

        /// <summary>
        /// Gets or sets the filter.
        /// </summary>
        /// <value>
        /// The filter.
        /// </value>
        public Func<SsdpMessage, bool> Filter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use remote endpoint as host].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [use remote endpoint as host]; otherwise, <c>false</c>.
        /// </value>
        public bool UseRemoteEndpointAsHost { get; set; }

        /// <summary>
        /// Gets or sets the host endpoint.
        /// </summary>
        /// <value>
        /// The host endpoint.
        /// </value>
        public IPEndPoint HostEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the type of the search.
        /// </summary>
        /// <value>
        /// The type of the search.
        /// </value>
        public string SearchType { get; set; }

        /// <summary>
        /// Gets or sets the mx.
        /// </summary>
        /// <value>
        /// The mx.
        /// </value>
        public ushort Mx { get; set; }

        #endregion

        public virtual void Dispose()
        {
            // Only close the server if we created it
            if (this.OwnsServer)
                this.Server.Close();
        }
    }
}
