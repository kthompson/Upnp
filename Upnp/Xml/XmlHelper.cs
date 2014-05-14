using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace Upnp.Xml
{
    public class XmlParseSet : IEnumerable
    {
        private readonly Dictionary<string, Action> _actions;
        private readonly List<Action<XmlReader>> _delayedActions;

        public XmlParseSet()
        {
            _delayedActions = new List<Action<XmlReader>>();
            this._actions = new Dictionary<string, Action>();
        }

        public void Add(Action action)
        {
            _actions.Add(XmlHelper.DefaultParseElementName, action);
        }

        public void Add(string element, Action action)
        {
            _actions.Add(element, action);
        }

        public void Add<T>(string element, ICollection<T> collection, string subElement, Func<T> activator) 
            where T : IXmlSerializable
        {
            _delayedActions.Add(reader => _actions.Add(element, () => XmlHelper.ParseXmlCollection(reader, collection, subElement, activator)));
        }

        public Dictionary<string, Action> ToDictionary(XmlReader reader)
        {
            foreach (var delayedAction in _delayedActions)
                delayedAction(reader);

            return _actions;
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotSupportedException();
        }
    }

    public static class XmlHelper
    {   
        public const string DefaultParseElementName = "@default";

        public static void ParseXml(XmlReader reader, XmlParseSet actions, string endElement = null)
        {
            var set = actions.ToDictionary(reader);
            ParseXml(reader, set, endElement);
        }

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
        
        public static void ParseXmlDictionary(XmlReader reader, IDictionary<string, string> collection, string elementName = DefaultParseElementName)
        {
            ParseXml(reader, new Dictionary<string, Action>
            {
                {elementName, () => collection[reader.LocalName] = reader.ReadInnerXml()}
            });
        }

        public static void ParseXmlCollection<T>(XmlReader reader, ICollection<T> collection, Func<T> creator)
            where T : IXmlSerializable
        {
            ParseXmlCollection(reader, collection, DefaultParseElementName, creator);
        }

        public static void ParseXmlCollection<T>(XmlReader reader, ICollection<T> collection, string elementName, Func<T> creator)
            where T : IXmlSerializable
        {
            var dict = new Dictionary<string, Action>
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
