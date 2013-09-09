using System.ComponentModel;
using System.Xml.Serialization;

namespace Upnp.Gena
{
    public interface IGenaProperty : INotifyPropertyChanged, IXmlSerializable
    {
        string Name { get; }
        string ServiceId { get; }
    }

    public interface IGenaProperty<T> : IGenaProperty
    {
        T Value { get; set; }
    }
}