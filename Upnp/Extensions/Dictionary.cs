using System.Linq;
using System.Collections.Specialized;

namespace Upnp.Extensions
{
    /// <summary>
    /// Extension methods for Dictionary
    /// </summary>
    public static class Dictionary
    {
        public static string ValueOrDefault(this NameValueCollection collection, string key, string value = null)
        {
            // Find the current value and if it's either not null or the key exists return it
            // It's assumed that the indexer is a faster way of finding the key so it's used first
            string found = collection[key];
            if (found != null || collection.Keys.Cast<string>().Any(k => k == key))
                return found;

            // Otherwise return the default value
            return value;
        }

    }
}
