using System;

namespace Upnp.Ssdp
{
    public interface ISsdpClient : ISsdpListener
    {
        /// <summary>
        /// Creates a search.
        /// </summary>
        /// <returns></returns>
        SsdpSearch CreateSearch(bool requireUniqueLocation);

        /// <summary>
        /// Occurs when [search response].
        /// </summary>
        event EventHandler<EventArgs<SsdpMessage>> SearchResponse;
    }
}