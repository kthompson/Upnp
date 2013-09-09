using System;
using System.Linq;
using System.Text;
using Upnp.Net;

namespace Upnp.Gena
{
    public class GenaMessage  : HttpMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenaMessage"/> class.
        /// </summary>
        /// <param name="isRequest">if set to <c>true</c> [is request].</param>
        protected GenaMessage(bool isRequest)
            : base(isRequest)
        {
        }

        /// <summary>
        /// Creates the request.
        /// </summary>
        /// <returns></returns>
        public new static GenaMessage CreateRequest()
        {
            return new GenaMessage(true);
        }

        /// <summary>
        /// Creates the response.
        /// </summary>
        /// <returns></returns>
        public new static GenaMessage CreateResponse()
        {
            return new GenaMessage(false);
        }
        
        public string EventUrl
        {
            get { return this.DirectiveObj; }
            set { this.DirectiveObj = value; }
        }

        public string SubscriptionId
        {
            get { 
                var sid = this.Headers["sid"]; 
                if(sid == null)
                    return null;
                
                return sid;                
            }
            set
            {
                if(value == null)
                {
                    this.Headers.Remove("sid");
                    return;
                }

                if (value.StartsWith("uuid:"))
                    this.Headers["sid"] = value;
                else
                    this.Headers["sid"] = string.Format("uuid:{0}", value);
            }
        }

        public string NotificationType
        {
            get { return this.Headers["nt"]; }
            set { this.Headers["nt"] = value; }
        }

        public Uri[] Callbacks
        {
            get
            {
                var callbacks = this.Headers["callback"];
                if(string.IsNullOrEmpty(callbacks))
                    return null;

                return callbacks.Split(',').Select(cb => cb.Trim().Trim('<', '>')).Select(cb => new Uri(cb)).ToArray();
            }
            set
            {
                if(value == null)
                {
                    this.Headers.Remove("callback");
                    return;
                }

                var sb = new StringBuilder();

                foreach (var callback in value)
                {
                    sb.AppendFormat("<{0}>, ", callback);
                }

                if (sb.Length > 0)
                    sb.Length -= 2;

                this.Headers["callback"] = sb.ToString();
            }

        }
        
        public TimeSpan? Timeout
        {
            get
            {
                var value = this.Headers["timeout"];

                if (value == null)
                    return null;

                string lower = value.ToLower();
                if (lower == "infinite" || lower == "second-infinite")
                    return TimeSpan.MaxValue;

                if (lower.StartsWith("second-"))
                {
                    value = value.Substring(7);
                    int seconds;
                    if (int.TryParse(value, out seconds))
                        return TimeSpan.FromSeconds(seconds);
                }
                
                return null;
            }
            set
            {
                if(value == null)
                {
                    this.Headers.Remove("timeout");
                    return;
                }

                var timeout = value.Value;

                string timeoutString;
                if (timeout == TimeSpan.MaxValue) //infinite
                {
                    timeoutString = "Infinite";
                }
                else
                {
                    timeoutString = "Second-" + timeout.TotalSeconds;
                }

                this.Headers["timeout"] = timeoutString;
            }
        }

        public string UserAgent
        {
            get { return this.Headers["server"]; }
            set
            {
                if(string.IsNullOrEmpty(value ))
                {
                    this.Headers.Remove("server");
                    return;
                }

                this.Headers["server"] = value;
            }
        }

        public bool IsSubscribe
        {
            get { return this.Directive.ToLower() == "subscribe" && string.IsNullOrEmpty(this.Headers["sid"]); }
            
        }

        public bool IsRenewal
        {
            get { return this.Directive.ToLower() == "subscribe" && !string.IsNullOrEmpty(this.Headers["sid"]); }
        }

        public bool IsUnsubscribe
        {
            get { return this.Directive.ToLower() == "unsubscribe"; }

        }

        public DateTime? Date
        {
            get
            {
                if (string.IsNullOrEmpty(this.Headers["date"]))
                    return null;

                DateTime date;

                if (DateTime.TryParse(this.Headers["date"], out date))
                    return date;

                return null;
            }
            set 
            { 
                if(value == null)
                {
                    this.Headers.Remove("date");
                    return;
                }

                this.Headers["date"] = value.Value.ToString("r");
            }
        }
    }
}
