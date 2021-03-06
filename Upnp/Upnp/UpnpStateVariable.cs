﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using Upnp.Xml;

namespace Upnp.Upnp
{
    public class UpnpStateVariable : IXmlSerializable
    {

        #region Object Overrides

        public override string ToString()
        {
            return string.Format("{0} [{1}]", this.Name, this.DataType);
        }

        #endregion

        #region IXmlSerializable Implementation

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            if (reader.LocalName != "stateVariable" && !reader.ReadToDescendant("stateVariable"))
                throw new InvalidDataException();

            if (reader.HasAttributes)
                this.SendEvents = ((reader.GetAttribute("sendEvents") ?? "no") == "yes");

            XmlHelper.ParseXml(reader, new XmlParseSet
            {
                {"name", () => this.Name = reader.ReadString()},
                {"dataType", () => this.DataType = reader.ReadString()}
            });
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteStartElement("stateVariable");
            writer.WriteElementString("name", this.Name);
            writer.WriteElementString("dataType", this.DataType);
            writer.WriteEndElement();
        }

        #endregion

        #region Properties

        public bool SendEvents
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string DataType
        {
            get;
            set;
        }

        #endregion

    }
}
