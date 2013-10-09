﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Upnp.Ssdp
{
    /// <summary>
    /// Class to combine the functionality of SsdpListener and SsdpSearch
    /// </summary>
    public class SsdpClient : SsdpListener, ISsdpClient
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpClient"/> class.
        /// </summary>
        public SsdpClient(ISsdpSocket socket = null)
            : base(socket)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a search.
        /// </summary>
        /// <returns></returns>
        public virtual ISsdpSearch CreateSearch(bool requireUniqueLocation)
        {
            var search = new SsdpSearch();

            var dict = new Dictionary<string, SsdpMessage>();
            search.Filter = msg =>
            {
                lock (dict)
                {
                    // Restrict duplicate search responses based on location or UDN/USN
                    // The reason for this is that there is potential for devices to share the same UDN
                    // However, each unique location is definitely a separate result
                    // And there's no potential for two devices to share the same location
                    string key = (requireUniqueLocation ? msg.Location : msg.USN);
                    if (dict.ContainsKey(key))
                        return false;

                    dict.Add(key, msg);
                    return true;
                }
            };

            search.ResultFound += (sender, e) =>
            {
                this.OnSsdpMessageReceived(sender, e);
                this.OnSearchResponse(sender, e);   
            };

            return search;
        }


        /// <summary>
        /// Finds the first SsdpMessage that matches our Filter.
        /// </summary>
        /// <param name="waitForTime">The wait for time.</param>
        /// <returns></returns>
        public SsdpMessage FindFirst(TimeSpan waitForTime)
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
                    this.SsdpMessageReceived -= resultHandler;

                    Monitor.Pulse(syncRoot);
                }
            };

            try
            {
                
                // Add our handler and start the async search
                this.SsdpMessageReceived += resultHandler;

                // Wait until our search is complete
                lock (syncRoot)
                {
                    Monitor.Wait(syncRoot, waitForTime);
                }
            }
            finally
            {
                // Make sure we remove our handler when we're done
                this.SsdpMessageReceived -= resultHandler;
            }

            return result;
        }
        #endregion

        #region Events

        /// <summary>
        /// Occurs when [search response].
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SearchResponse;
        
        /// <summary>
        /// Called when [search response].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs{T}.Discovery.Ssdp.SsdpMessage&gt;"/> instance containing the event data.</param>
        protected virtual void OnSearchResponse(object sender, EventArgs<SsdpMessage> e)
        {
            var handler = this.SearchResponse;
            if (handler != null)
                handler(sender, e);
        }

        #endregion
    }
}
