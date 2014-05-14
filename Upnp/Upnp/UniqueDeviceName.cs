using System;

namespace Upnp.Upnp
{
    public class UniqueDeviceName
    {
        public Guid Uuid { get; set; }

        public override string ToString()
        {
            return "uuid:" + this.Uuid;
        }

        public static implicit operator UniqueDeviceName(string udn)
        {
            if (udn == null)
                return null;

            return Parse(udn);
        }

        public static implicit operator String(UniqueDeviceName udn)
        {
            if (udn == null)
                return null;

            return udn.ToString();
        }

        public static UniqueDeviceName Parse(string udn)
        {
            if (string.IsNullOrEmpty(udn))
                throw new FormatException("The supplied udn is not valid");

            return new UniqueDeviceName {Uuid = udn.StartsWith("uuid:") ? new Guid(udn.Substring(5)) : new Guid(udn)};
        }

        public override int GetHashCode()
        {
            return this.Uuid.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UniqueDeviceName)obj);
        }

        protected bool Equals(UniqueDeviceName other)
        {
            return Uuid.Equals(other.Uuid);
        }
    }
}
