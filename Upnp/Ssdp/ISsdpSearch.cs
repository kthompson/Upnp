using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Upnp.Ssdp
{
    public interface ISsdpSearch : IDisposable
    {
        /// <summary>
        /// Finds the first result.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <returns></returns>
        SsdpMessage FindFirst(params IPEndPoint[] destinations);

        /// <summary>
        /// Searches the specified destinations.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <returns></returns>
        List<SsdpMessage> Search(params IPEndPoint[] destinations);

        /// <summary>
        /// Searches asynchronously.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        void SearchAsync(params IPEndPoint[] destinations);

        /// <summary>
        /// Cancels the search.
        /// </summary>
        void CancelSearch();

        /// <summary>
        /// Waits for the current search to complete.
        /// </summary>
        void WaitForSearch();

        /// <summary>
        /// Occurs when [search complete].
        /// </summary>
        event EventHandler SearchComplete;

        /// <summary>
        /// Occurs when [result found].
        /// </summary>
        event EventHandler<EventArgs<SsdpMessage>> ResultFound;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is searching.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is searching; otherwise, <c>false</c>.
        /// </value>
        bool IsSearching { get; }

        /// <summary>
        /// Gets or sets the filter.
        /// </summary>
        /// <value>
        /// The filter.
        /// </value>
        Func<SsdpMessage, bool> Filter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use remote endpoint as host].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [use remote endpoint as host]; otherwise, <c>false</c>.
        /// </value>
        bool UseRemoteEndpointAsHost { get; set; }

        /// <summary>
        /// Gets or sets the host endpoint.
        /// </summary>
        /// <value>
        /// The host endpoint.
        /// </value>
        IPEndPoint HostEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the type of the search.
        /// </summary>
        /// <value>
        /// The type of the search.
        /// </value>
        string SearchType { get; set; }

        /// <summary>
        /// Gets or sets the mx.
        /// </summary>
        /// <value>
        /// The mx.
        /// </value>
        ushort Mx { get; set; }
    }
}