﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Upnp.Xml;

namespace Upnp.Gena
{
    public abstract class GenaProperty<T> : IGenaProperty<T>
    {
        protected GenaProperty(string serviceId, string name, T value = default(T))
        {
            this.ServiceId = serviceId;
            this.Name = name;
            this._value = value;
        }

        public string Name { get; private set; }
        public string ServiceId { get; private set; }

        private T _value;
        public T Value
        {
            get { return _value; }
            set
            {
                if ((object)_value == (object) value) 
                    return;

                this._value = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged()
        {
            var handler = PropertyChanged;
            if (handler != null) 
                handler(this, new PropertyChangedEventArgs(this.Name));
        }

        protected abstract T ReadValueXml(XmlReader reader);
        protected abstract void WriteValueXml(XmlWriter writer, T value);

        public XmlSchema GetSchema()
        {
            return null;
        }

        public virtual void ReadXml(XmlReader reader)
        {
            if (reader.LocalName != "property" && !reader.ReadToDescendant("property"))
                throw new InvalidDataException();

            XmlHelper.ParseXml(reader, new Dictionary<string, Action>
            {
                {this.Name, () => this.Value = ReadValueXml(reader)},
            });
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("property");
            writer.WriteStartElement(this.Name);
            WriteValueXml(writer, this.Value);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
    }
}
