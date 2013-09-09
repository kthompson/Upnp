using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Upnp.Ssdp
{
    public static class SsdpSocketFactory
    {
        public static IEnumerable<ISsdpSocket> BuildSockets(params int[] ports)
        {
            return BuildSockets("device.xml", ports);
        }
        
        public static IEnumerable<ISsdpSocket> BuildSockets(string locationPath = "device.xml", params int[] ports)
        {
            if (ports.Length == 0)
                ports = new[] {Protocol.DefaultPort};

            return from ni in NetworkInterface.GetAllNetworkInterfaces()
                   from ua in ni.GetIPProperties().UnicastAddresses
                   from port in ports
                   let ip = ua.Address
                   //TODO: remove the ipv4 check
                   where !IPAddress.IsLoopback(ip) && ip.AddressFamily == AddressFamily.InterNetwork
                   let uri = new UriBuilder { Scheme = "http", Host = ip.ToString(), Path = locationPath }.Uri
                   let ep = new IPEndPoint(ip, port)
                   select (ISsdpSocket) new SsdpSocket(ep, uri);
        }

        public static ISsdpSocket BuildLocal(string location = "http://localhost/device.xml")
        {
            return new SsdpSocket(new IPEndPoint(IPAddress.Any, Protocol.DefaultPort), new Uri(location));
        }
    }
}
