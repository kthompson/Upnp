using System;

namespace Upnp.Ssdp
{
    public interface ISsdpClient : ISsdpListener
    {
        /// <summary>
        /// Creates a search.
        /// </summary>
        /// <returns></returns>
        ISsdpSearch CreateSearch(bool requireUniqueLocation);

        /// <summary>
        /// Finds the first SsdpMessage that matches our Filter.
        /// </summary>
        /// <param name="waitForTime">The wait for time.</param>
        /// <returns></returns>
        SsdpMessage FindFirst(TimeSpan waitForTime);

        /// <summary>
        /// Occurs when [search response].
        /// </summary>
        event EventHandler<EventArgs<SsdpMessage>> SearchResponse;
    }
}