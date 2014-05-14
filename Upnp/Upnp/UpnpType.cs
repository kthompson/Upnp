using System;

namespace Upnp.Upnp
{
    public class UpnpType : IEquatable<UpnpType>
    {

        #region Constructors

        public UpnpType()
            : this(string.Empty, string.Empty, string.Empty, new Version())
        {
        }

        public UpnpType(string domain, string kind, string type, Version version)
        {
            this.Domain = domain;
            this.Kind = kind;
            this.Type = type;
            this.Version = version;
        }

        public static UpnpType Parse(string urn)
        {
            if(string.IsNullOrEmpty(urn))
                throw new ArgumentNullException("urn");

            string[] parts = urn.Split(':');
            if (parts.Length != 5)
                throw new FormatException("urn must be a ");

            return new UpnpType(parts[1], parts[2], parts[3], Version.Parse(parts[4] + ".0"));
        }

        #endregion

        #region Object Overrides

        public bool Equals(UpnpType other)
        {
            if (ReferenceEquals(null, other)) 
                return false;

            if (ReferenceEquals(this, other)) 
                return true;

            return string.Equals(Domain, other.Domain) && 
                   string.Equals(Kind, other.Kind) && 
                   string.Equals(Type, other.Type) && 
                   Equals(Version, other.Version);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) 
                return false;

            if (ReferenceEquals(this, obj)) 
                return true;

            if (obj.GetType() != this.GetType()) 
                return false;

            return Equals((UpnpType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Domain != null ? Domain.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Kind != null ? Kind.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Version != null ? Version.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return string.Format("urn:{0}:{1}:{2}:{3}", this.Domain, this.Kind, this.Type, this.VersionString);
        }

        #endregion

        #region Properties

        public string Domain
        {
            get;
            set;
        }

        public string Kind
        {
            get;
            set;
        }

        public string Type
        {
            get;
            set;
        }

        public Version Version
        {
            get;
            set;
        }

        public string VersionString
        {
            get { return (this.Version.Minor == 0 ? this.Version.Major.ToString() : this.Version.ToString(2)); }
            set { this.Version = Version.Parse(value + ".0"); }
        }

        #endregion

    }
}
