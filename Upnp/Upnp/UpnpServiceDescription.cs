using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using Upnp.Extensions;
using Upnp.Xml;

namespace Upnp.Upnp
{
    public class UpnpServiceDescription : IXmlSerializable
    {
        public UpnpService Service { get; private set; }

        public UpnpServiceDescription(UpnpService service)
        {
            this.Service = service;
        }

        #region IXmlSerializable Implementation

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.LocalName != "scpd" && !reader.ReadToDescendant("scpd"))
                throw new InvalidDataException();

            var dict = new Dictionary<string, Action>
            {
                {"specVersion", () => XmlHelper.ParseXml(reader, new Dictionary<string, Action>
                    {
                        {"major", () => this.VersionMajor = reader.ReadElementContentAsInt()},
                        {"minor", () => this.VersionMinor = reader.ReadElementContentAsInt()}
                    })
                },
                {"actionList", () => XmlHelper.ParseXmlCollection(reader, this.Service.Actions, "action", () => new UpnpAction())},
                {"serviceStateTable", () => XmlHelper.ParseXmlCollection(reader, this.Service.Variables, "stateVariable", () => new UpnpStateVariable())}
            };

            XmlHelper.ParseXml(reader, dict);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("scpd");
            writer.WriteAttributeString("xmlns", "urn:schemas-upnp-org:service-1-0");
            writer.WriteStartElement("specVersion");
            writer.WriteElementString("major", this.VersionMajor.ToString());
            writer.WriteElementString("minor", this.VersionMinor.ToString());
            writer.WriteEndElement();
            writer.WriteCollection(this.Service.Actions, "actionList", true);
            writer.WriteCollection(this.Service.Variables, "serviceStateTable", true);
            writer.WriteEndElement();
        }

        #endregion

        #region Properties

        public int VersionMajor
        {
            get;
            set;
        }

        public int VersionMinor
        {
            get;
            set;
        }

        #endregion

    }
}
