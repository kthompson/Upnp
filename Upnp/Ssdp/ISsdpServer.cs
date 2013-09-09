using System.Collections.Generic;

namespace Upnp.Ssdp
{
    public interface ISsdpServer : ISsdpListener
    {
        /// <summary>
        /// Create an idle announcer.
        /// </summary>
        /// <param name="respondToSearches">if set to <c>true</c> [respond to searches].</param>
        /// <returns></returns>
        ISsdpAnnouncer CreateAnnouncer(bool respondToSearches = true);

        /// <summary>
        /// Create an idle announcer.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="usn">The usn.</param>
        /// <param name="respondToSearches">if set to <c>true</c> [respond to searches].</param>
        /// <returns></returns>
        ISsdpAnnouncer CreateAnnouncer(string type, string usn, bool respondToSearches = true);

        /// <summary>
        /// Removes the announcer.
        /// </summary>
        /// <param name="announcer">The announcer.</param>
        void RemoveAnnouncer(ISsdpAnnouncer announcer);

        /// <summary>
        /// Sets the search responses.
        /// </summary>
        /// <param name="announcer">The announcer.</param>
        /// <param name="respondToSearches">if set to <c>true</c> [respond to searches].</param>
        void SetRespondsToSearches(ISsdpAnnouncer announcer, bool respondToSearches);

        /// <summary>
        /// Gets the matching responders.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <returns></returns>
        IEnumerable<ISsdpAnnouncer> GetMatchingResponders(SsdpMessage msg);
    }
}