﻿using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Upnp.Xml;

namespace Upnp.Upnp
{
    public class UpnpArgument : IXmlSerializable
    {

        #region Object Overrides

        public override string ToString()
        {
            return string.Format("{0} [{1}]", this.Name, this.Direction);
        }

        #endregion

        #region IXmlSerializable Implementation

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.LocalName != "argument" && !reader.ReadToDescendant("argument"))
                throw new InvalidDataException();

            XmlHelper.ParseXml(reader, new XmlParseSet
            {
                {"name", () => this.Name = reader.ReadString()},
                {"direction", () => this.Direction = reader.ReadString()},
                {"relatedStateVariable", () => this.RelatedStateVariable = reader.ReadString()}
            });
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteStartElement("argument");
            writer.WriteElementString("name", this.Name);
            writer.WriteElementString("direction", this.Direction);
            writer.WriteElementString("relatedStateVariable", this.RelatedStateVariable);
            writer.WriteEndElement();
        }

        #endregion

        #region Properties

        public UpnpAction Action
        {
            get;
            protected internal set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string Direction
        {
            get;
            set;
        }

        public string RelatedStateVariable
        {
            get;
            set;
        }

        public string Value
        {
            get;
            set;
        }

        #endregion

    }
}
