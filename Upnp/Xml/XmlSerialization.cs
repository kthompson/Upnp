using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Upnp.Xml
{
    #region IXmlSerializable<T> Interface

    public interface IXmlSerializable<T> : IXmlCloneable<T>, IXmlEquatable<T>, IXmlSerializable where T : class, IXmlSerializable<T>, new()
    {
        string Serialize();
        void Deserialize(string xml);
        T Create(string xml);
    }

    public interface IXmlCloneable<out T> : ICloneable where T : class, IXmlCloneable<T>, IXmlSerializable, new()
    {
        new T Clone();
    }

    public interface IXmlEquatable<T> : IEquatable<T> where T : class, IXmlEquatable<T>, IXmlSerializable, new()
    {
        new bool Equals(T other);
        int GetHashCode();
    }

    #endregion

    #region XmlSerializable<T>

    public abstract class XmlSerializable<T> : IXmlSerializable<T> where T : XmlSerializable<T>, new()
    {
        #region Implementation of IXmlSerializable<T>

        public string Serialize()
        {
            return XmlSerializationHelper.Serialize((T)this);
        }

        public void Deserialize(string xml)
        {
            XmlSerializationHelper.Deserialize(xml, (T)this);
        }

        public static T Create(string xml)
        {
            return XmlSerializationHelper.Create<T>(xml);
        }

        T IXmlSerializable<T>.Create(string xml)
        {
            return Create(xml);
        }

        #endregion

        #region Implementation of ICloneable<out T>

        public T Clone()
        {
            return XmlSerializationHelper.Clone((T)this);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion

        #region Implementation of IXmlEquatable<T>

        public bool Equals(T other)
        {
            return XmlSerializationHelper.Equals((T)this, other);
        }

        bool IEquatable<T>.Equals(T other)
        {
            return Equals(other);
        }

        public new int GetHashCode()
        {
            return XmlSerializationHelper.GetHashCode((T)this);
        }

        #endregion

        #region Implementation of IXmlSerializable

        public abstract XmlSchema GetSchema();
        public abstract void ReadXml(XmlReader reader);
        public abstract void WriteXml(XmlWriter writer);

        #endregion
    }

    #endregion

    #region XmlSerializationHelper

    public static class XmlSerializationHelper
    {
        public static string Serialize(Action<XmlWriter> writeMethod)
        {
            using (var sw = new StringWriter())
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    writeMethod(writer);
                    return sw.ToString();
                }
            }
        }

        public static string Serialize<T>(T item, XmlWriterSettings settings = null) where T : class, IXmlSerializable, new()
        {
            if (item == null)
                return null;

            return Serialize(item.WriteXml);
        }

        public static void Deserialize<T>(string xml, T item) where T : class, IXmlSerializable, new()
        {
            if (String.IsNullOrEmpty(xml) || item == null)
                return;

            using (var sr = new StringReader(xml))
            {
                using (var reader = new XmlTextReader(sr))
                {
                    item.ReadXml(reader);
                }
            }
        }

        public static T Create<T>(string xml) where T : class, IXmlSerializable, new()
        {
            var item = new T();
            Deserialize(xml, item);
            return item;
        }

        public static T Clone<T>(T item) where T : class, IXmlSerializable, new()
        {
            if (item == null)
                return null;

            var xml = Serialize(item);
            var clone = new T();
            Deserialize(String.Copy(xml), clone);

            return clone;
        }

        public static T FromDisk<T>(string path) where T : class, IXmlSerializable, new()
        {
            var item = new T();
            using (var reader = new XmlTextReader(path))
            {
                item.ReadXml(reader);
            }

            return item;
        }

        public static void ToDisk<T>(string path, T item) where T : class, IXmlSerializable, new()
        {
            using (var fs = new FileStream(path, FileMode.Create))
            {
                using (var writer = new XmlTextWriter(fs, Encoding.UTF8))
                {
                    writer.Formatting = Formatting.Indented;
                    item.WriteXml(writer);
                    writer.Flush();
                }
            }
        }

        public static bool Equals<T>(T first, T second) where T : class, IXmlSerializable, new()
        {
            var firstXml = Serialize(first);
            var secondXml = Serialize(second);
            return String.Equals(firstXml, secondXml);
        }

        public static int GetHashCode<T>(T item) where T : class, IXmlSerializable, new()
        {
            var xml = Serialize(item);
            return xml.GetHashCode();
        }
    }

    #endregion
}
