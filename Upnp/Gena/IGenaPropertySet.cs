using System.Xml.Serialization;

namespace Upnp.Gena
{
    public interface IGenaPropertySet : IXmlSerializable
    {
        void Add(IGenaProperty property);
    }
}