using System;
using System.Net;

namespace Upnp.Ssdp
{
    public interface ISsdpListener : IDisposable
    {
        /// <summary>
        /// Gets the ssdp message filter.
        /// </summary>
        /// <value>
        /// The filter.
        /// </value>
        Func<SsdpMessage, bool> Filter { get; set; }
        
        /// <summary>
        /// Starts listening on the specified remote endpoints.
        /// </summary>
        /// <param name="groups">The remote eps.</param>
        void StartListening(params IPAddress[] groups);

        /// <summary>
        /// Stops listening on all end points.
        /// </summary>
        void StopListening();

        /// <summary>
        /// Occurs when an SSDP message is received.
        /// </summary>
        event EventHandler<EventArgs<SsdpMessage>> SsdpMessageReceived;

        /// <summary>
        /// Occurs when a service is found.
        /// </summary>
        event EventHandler<EventArgs<SsdpMessage>> ServiceFound;

        /// <summary>
        /// Occurs when a device is found.
        /// </summary>
        event EventHandler<EventArgs<SsdpMessage>> DeviceFound;

        /// <summary>
        /// Occurs when a root is found.
        /// </summary>
        event EventHandler<EventArgs<SsdpMessage>> RootFound;

        /// <summary>
        /// Occurs when SSDP alive received.
        /// </summary>
        event EventHandler<EventArgs<SsdpMessage>> Alive;

        /// <summary>
        /// Occurs when SSDP bye bye.
        /// </summary>
        event EventHandler<EventArgs<SsdpMessage>> ByeBye;
    }
}