using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;

namespace Upnp.Gena
{
    public class GenaPropertySet : IGenaPropertySet
    {
        private readonly Dictionary<string, IGenaProperty> _properties = new Dictionary<string, IGenaProperty>();

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("propertyset");

            foreach (var property in _properties.Values)
                property.WriteXml(writer);

            writer.WriteEndElement(); //propertyset
        }

        public void Add(IGenaProperty property)
        {
            _properties[property.Name] = property;
        }
    }
}