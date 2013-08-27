using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Upnp.Xml
{
    public class XmlStateMachine : IEnumerable
    {
        readonly Dictionary<string, Action<XmlReader>> _actions = new Dictionary<string, Action<XmlReader>>();

        public void Add(string key, Action<XmlReader> value)
        {
            _actions.Add(key, value);
        }

        public void Add(Action<XmlReader> value)
        {
            Add(XmlHelper.DefaultParseElementName, value);
        }

        public void Add(string key, XmlStateMachine value)
        {
            _actions.Add(key, reader => value.Add(reader, key));
        }

        public void Add(XmlStateMachine value)
        {
            Add(XmlHelper.DefaultParseElementName, value);
        }

        public void Add(XmlReader reader, string endElement = null)
        {
            XmlHelper.ParseXml(reader, _actions.ToDictionary(kv => kv.Key, kv => (Action)(() => kv.Value(reader))), endElement);
        }

        public void Parse(XmlReader reader, string endElement = null)
        {
            Add(reader, endElement);
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotSupportedException();    
        }
    }

    public static class XmlHelper
    {

        public const string DefaultParseElementName = "@default";

        public static void ParseXml(XmlReader reader, Dictionary<string, Action> actions, string endElement = null)
        {
            // If no end element was specified then just read until the end tag of our current element
            if (string.IsNullOrEmpty(endElement))
            {
                // Make sure we can actually figure out the end element
                if (!reader.IsStartElement())
                    throw new ArgumentException();

                endElement = reader.LocalName;
            }
            
            // Nothing to read so just return
            if (reader.LocalName == endElement && reader.IsEmptyElement)
                return;

            // Check to see if there's a default action to perform if no specific action is found
            Action defaultAction = null;
            actions.TryGetValue(DefaultParseElementName, out defaultAction);

            // Read until the end (or we find our end tag)
            while (reader.Read())
            {
                // If this is not a start element then we can skip it
                if (!reader.IsStartElement())
                {
                    // If we found our end element then exit
                    if (reader.LocalName == endElement)
                        break;

                    continue;
                }

                // Try to find the action we should use for this element
                Action action = null;
                if (actions.TryGetValue(reader.LocalName, out action))
                    action();
                else if (defaultAction != null)
                    defaultAction();
            }
        }

        public static void ParseXmlCollection<T>(XmlReader reader, ICollection<T> collection, string elementName, Func<T> creator)
            where T : IXmlSerializable
        {
            var dict = new Dictionary<string, Action>()
            {
                {elementName, () =>
                    {
                        T value = creator();
                        collection.Add(value);
                        value.ReadXml(reader);
                    }
                }
            };

            ParseXml(reader, dict);
        }

       public static void ParseXmlCollection<T>(XmlReader reader, ICollection<T> collection, string elementName = DefaultParseElementName)
           where T : IXmlSerializable, new()
       {
           var dict = new Dictionary<string, Action>()
            {
                {elementName, () =>
                    {
                        T value = new T();
                        collection.Add(value);
                        value.ReadXml(reader);
                    }
                }
            };

           ParseXml(reader, dict);
       }
    }
}
