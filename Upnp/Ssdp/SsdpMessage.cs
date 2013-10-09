using System;
using System.IO;
using System.Net;
using Upnp.Extensions;
using Upnp.Net;

namespace Upnp.Ssdp
{
    /// <summary>
    /// Class representing an SSDP request/response message
    /// </summary>
    public class SsdpMessage : HttpMessage
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpMessage" /> class.
        /// </summary>
        /// <param name="isRequest"></param>
        /// <param name="destination">The destination.</param>
        /// <param name="source">The source.</param>
        private SsdpMessage(bool isRequest, IPEndPoint destination, IPEndPoint source)
            : base(isRequest)
        {
            this.Source = source;
            this.Destination = destination;
        }

        #endregion

        #region Protected Methods

        protected override void FromStream(TextReader reader)
        {
            base.FromStream(reader);

            // Parse out the type and UDN from the data
            this.Type = this.Headers.ValueOrDefault("NT", string.Empty);
            this.UDN = this.USN;
            int index = this.UDN.IndexOf("::");
            if (index != -1)
            {
                if (string.IsNullOrEmpty(this.Type))
                    this.Type = this.UDN.Substring(index + 2);

                this.UDN = this.UDN.Substring(0, index);
            }

            // Parse out the max age from the cache control
            var cacheControl = this.Headers.ValueOrDefault("CACHE-CONTROL", string.Empty).ToUpper();
            var temp = 0;
            this.MaxAge = 0;
            if (cacheControl.StartsWith("MAX-AGE") && int.TryParse(cacheControl.Substring(8), out temp))
                this.MaxAge = temp;
            else
            {
                var mx = this.Headers.ValueOrDefault("MX", string.Empty);
                if (int.TryParse(mx, out temp))
                    this.MaxAge = temp;    
            }
                
            

            string date = this.Headers.ValueOrDefault("DATE", string.Empty);
            DateTime tempDt;
            if (string.IsNullOrEmpty(date) || !DateTime.TryParse(date, out tempDt))
                this.DateGenerated = DateTime.Now;
            else
                this.DateGenerated = tempDt;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public IPEndPoint Source
        {
            get;
            protected set;
        }

         
        /// <summary>
        /// Gets or sets the destination.
        /// </summary>
        /// <value>
        /// The destination.
        /// </value>
        public IPEndPoint Destination
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets a value indicating whether this is an alive message.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this is an alive message; otherwise, <c>false</c>.
        /// </value>
        public bool IsAlive
        {
            get { return (this.Headers.ValueOrDefault("NTS", string.Empty).ToLower() == Protocol.SsdpAliveNts.ToLower()); }
        }

        /// <summary>
        /// Gets a value indicating whether this is an alive message.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this is an alive message; otherwise, <c>false</c>.
        /// </value>
        public bool IsByeBye
        {
            get { return (this.Headers.ValueOrDefault("NTS", string.Empty).ToLower() == Protocol.SsdpByeByeNts.ToLower()); }
        }

        /// <summary>
        /// Gets the USN.
        /// </summary>
        public string USN
        {
            get { return this.Headers.ValueOrDefault("USN", string.Empty); }
        }

        /// <summary>
        /// Gets or sets the UDN.
        /// </summary>
        /// <value>
        /// The UDN.
        /// </value>
        public string UDN
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the max age.
        /// </summary>
        /// <value>
        /// The max age.
        /// </value>
        public int MaxAge
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the date generated.
        /// </summary>
        /// <value>
        /// The date generated.
        /// </value>
        public DateTime DateGenerated
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the location.
        /// </summary>
        public string Location
        {
            get { return this.Headers.ValueOrDefault("LOCATION", string.Empty); }
        }

        /// <summary>
        /// Gets the server.
        /// </summary>
        public string Server
        {
            get { return this.Headers.ValueOrDefault("SERVER", string.Empty); }
        }

        /// <summary>
        /// Gets the type of the search.
        /// </summary>
        /// <value>
        /// The type of the search.
        /// </value>
        public string SearchType
        {
            get { return this.Headers.ValueOrDefault("ST", string.Empty); }
        }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public string Type
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets a value indicating whether this is a device message.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this is a device message; otherwise, <c>false</c>.
        /// </value>
        public bool IsDevice
        {
            get { return (this.Type.IndexOf(":device:") != -1); }
        }

        /// <summary>
        /// Gets a value indicating whether this is  service message.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this is a service message; otherwise, <c>false</c>.
        /// </value>
        public bool IsService
        {
            get { return (this.Type.IndexOf(":service:") != -1); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is advertisement.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is advertisement; otherwise, <c>false</c>.
        /// </value>
        public bool IsAdvertisement
        {
            get { return string.IsNullOrEmpty(this.SearchType); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is root.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is root; otherwise, <c>false</c>.
        /// </value>
        public bool IsRoot
        {
            get { return (this.Type == "upnp:rootdevice"); }
        }

        #endregion

        /// <summary>
        /// Parses the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <returns></returns>
        public static SsdpMessage Parse(Stream stream, IPEndPoint source, IPEndPoint destination)
        {
            using (var reader = new StreamReader(stream))
            {
                return Parse(reader, source, destination);
            }
        }

        /// <summary>
        /// Parses the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="length">The length.</param>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <returns></returns>
        public static SsdpMessage Parse(byte[] buffer, int length, IPEndPoint source, IPEndPoint destination)
        {
            using (var stream = new MemoryStream(buffer, 0, length))
            {
                return Parse(stream, source, destination);
            }
        }

        /// <summary>
        /// Parses the specified reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <returns></returns>
        public static SsdpMessage Parse(TextReader reader, IPEndPoint source, IPEndPoint destination)
        {
            var message = new SsdpMessage(true, source, destination);
            message.FromStream(reader);
            return message;
        }
    }
}
