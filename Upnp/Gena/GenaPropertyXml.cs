using System.Xml;

namespace Upnp.Gena
{
    public class GenaPropertyXml : GenaProperty<string>
    {
        public GenaPropertyXml(string serviceId, string name, string value = null)
            : base(serviceId, name, value)
        {
        }

        protected override string ReadValueXml(XmlReader reader)
        {
            return reader.ReadElementContentAsString();
        }

        protected override void WriteValueXml(XmlWriter writer, string value)
        {
            writer.WriteRaw(value);
        }
    }
}