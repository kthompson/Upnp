using System.Xml;

namespace Upnp.Gena
{
    public class GenaPropertyInt32 : GenaProperty<int>
    {
        public GenaPropertyInt32(string serviceId, string name, int value = 0)
            : base(serviceId, name, value)
        {
        }

        protected override int ReadValueXml(XmlReader reader)
        {
            return reader.ReadElementContentAsInt();
        }

        protected override void WriteValueXml(XmlWriter writer, int value)
        {
            writer.WriteValue(value);
        }
    }
}