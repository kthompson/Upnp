﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using Upnp.Xml;

namespace Upnp.Upnp
{
    public class UpnpRoot : IXmlSerializable
    {
        public UpnpRoot(int majorVersion = 1, int minorVersion = 1)
        {
            this.UpnpMajorVersion = majorVersion;
            this.UpnpMinorVersion = minorVersion;
        }

        #region Object Overrides

        public override string ToString()
        {
            return (this.DeviceDescriptionUrl ?? this.UrlBase).ToString();
        }

        #endregion

        #region IXmlSerializable Implementation

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (!reader.ReadToDescendant("root"))
                throw new InvalidDataException();

            if (reader.HasAttributes)
                this.ConfigurationId = int.Parse(reader.GetAttribute("configId") ?? "-1");

            XmlHelper.ParseXml(reader, new XmlParseSet
            {
                {"specVersion", () => XmlHelper.ParseXml(reader, new XmlParseSet {
                    {"major", () => this.UpnpMajorVersion = reader.ReadElementContentAsInt()},
                    {"minor", () => this.UpnpMinorVersion = reader.ReadElementContentAsInt()}
                })
                },
                {"URLBase", () => this.UrlBase = new Uri(reader.ReadString())},
                {"device", () =>
                {
                    this.RootDevice = new UpnpDevice();
                    this.RootDevice.ReadXml(reader);
                }}
            });
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("root");
            writer.WriteAttributeString("xmlns", "urn:schemas-upnp-org:device-1-0");
            if (this.ConfigurationId != -1)
                writer.WriteAttributeString("configId", this.ConfigurationId.ToString());

            writer.WriteStartElement("specVersion");
            writer.WriteElementString("major", this.UpnpMajorVersion.ToString());
            writer.WriteElementString("minor", this.UpnpMinorVersion.ToString());
            writer.WriteEndElement();
            writer.WriteElementString("URLBase", this.UpnpMinorVersion == 0 ? this.UrlBase.ToString() : string.Empty);

            if (this.RootDevice != null)
                this.RootDevice.WriteXml(writer);

            writer.WriteEndElement();
        }

        #endregion

        #region Properties

        public int UpnpMajorVersion
        {
            get;
            set;
        }

        public int UpnpMinorVersion
        {
            get;
            set;
        }

        private Uri _deviceDescriptionUrl;

        public Uri DeviceDescriptionUrl
        {
            get { return _deviceDescriptionUrl; }
            set
            {
                _deviceDescriptionUrl = value;

                if (_deviceDescriptionUrl == null)
                {
                    this.UrlBase = null;
                    return;
                }

                var builder = new UriBuilder(_deviceDescriptionUrl) { Path = string.Empty };
                this.UrlBase = builder.Uri;
            }
        }

        public Uri UrlBase
        {
            get;
            set;
        }

        public int ConfigurationId
        {
            get;
            set;
        }

        private UpnpDevice _rootDevice;
        public UpnpDevice RootDevice
        {
            get { return this._rootDevice; }
            set
            {
                if (this._rootDevice != null)
                    this._rootDevice.Root = null;

                this._rootDevice = value;
                if (value != null)
                    value.Root = this;
            }
        }


        #endregion

        public event EventHandler<EventArgs<UpnpDevice>> ChildDeviceAdded;

        public event EventHandler<EventArgs<UpnpDevice>> ChildDeviceRemoved;

        protected internal void OnChildDeviceAdded(UpnpDevice child)
        {
            var handler = ChildDeviceAdded;
            if (handler != null) 
                handler(this, new EventArgs<UpnpDevice>(child));
        }

        protected internal void OnChildDeviceRemoved(UpnpDevice child)
        {
            var handler = ChildDeviceRemoved;
            if (handler != null) 
                handler(this, new EventArgs<UpnpDevice>(child));
        }


        public static UpnpRoot LoadFromDeviceDescriptionUrl(string url)
        {
            var root = new UpnpRoot();
            using (var reader = new XmlTextReader(url))
            {
                root.ReadXml(reader);
            }

            root.DeviceDescriptionUrl = new Uri(url);

            return root;
        }
    }
}
