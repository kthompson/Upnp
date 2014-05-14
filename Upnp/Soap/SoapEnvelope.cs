using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Upnp.Xml;

namespace Upnp.Soap
{
    public class SoapEnvelope : IXmlSerializable
    {
        private const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/"; // soap 1.1

        public Dictionary<string, string> Headers { get; set; }
        public string Method { get; set; }
        public string Namespace { get; set; }
        public Dictionary<string, string> Arguments { get; set; }

        public SoapEnvelope()
        {
            this.Headers = new Dictionary<string, string>();
            this.Arguments = new Dictionary<string, string>();
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.LocalName != "Envelope" && !reader.ReadToDescendant("Envelope", SoapEnvelopeNamespace))
                throw new InvalidDataException();

            XmlHelper.ParseXml(reader, new XmlParseSet
            {
                {"Header", () => XmlHelper.ParseXmlDictionary(reader.ReadSubtree(), this.Headers)},
                {"Body", () => XmlHelper.ParseXml(reader.ReadSubtree(), new XmlParseSet { 
                    () => {
                        this.Namespace = reader.NamespaceURI;
                        this.Method = reader.LocalName;
                        XmlHelper.ParseXmlDictionary(reader, this.Arguments);
                }})}
            });
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("s", "Envelope", SoapEnvelopeNamespace);
            writer.WriteAttributeString("s:encodingStyle", "http://schemas.xmlsoap.org/soap/encoding/"); // Soap 1.1

            if (this.Headers != null && this.Headers.Count != 0)
            {
                writer.WriteStartElement("Header", SoapEnvelopeNamespace);

                foreach (var key in this.Headers.Keys)
                {
                    writer.WriteStartElement(key);
                    writer.WriteRaw(this.Headers[key]);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }


            writer.WriteStartElement("Body", SoapEnvelopeNamespace);
            writer.WriteStartElement("u", Method, Namespace);

            if (Arguments != null)
            {
                foreach (string key in Arguments.Keys)
                {
                    string data = Arguments[key];

                    // Check to see if this is an xml element and if so just write out the raw data
                    if (data.StartsWith("<" + key) && data.EndsWith("</" + key + ">"))
                    {
                        writer.WriteRaw(data);
                    }
                    else
                    {
                        // Otherwise we'll need to write the start element and the value
                        writer.WriteStartElement(key);
                        writer.WriteRaw(data);
                        writer.WriteEndElement();
                    }
                }
            }

            writer.WriteEndElement(); //end method
            writer.WriteEndElement(); //end body
            writer.WriteEndElement(); //end envelope
        }
    }
}
