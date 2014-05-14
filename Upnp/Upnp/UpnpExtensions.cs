using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Upnp.Extensions;

namespace Upnp.Upnp
{
    public static class UpnpExtensions
    {
        #region Device Extensions 

        public static IEnumerable<UpnpDevice> FindByDeviceType(this UpnpRoot @this, UpnpType type)
        {
            return @this.RootDevice.FindByDeviceType(type);
        }

        public static IEnumerable<UpnpDevice> EnumerateDevices(this UpnpRoot @this)
        {
            return @this.RootDevice.EnumerateDevices();
        }

        public static IEnumerable<UpnpDevice> FindByDeviceType(this UpnpDevice @this, UpnpType type)
        {
            return @this.EnumerateDevices().Where(d => d.Type.Equals(type));
        }

        public static IEnumerable<UpnpDevice> EnumerateDevices(this UpnpDevice @this)
        {
            return new[] { @this }.Concat(@this.Devices.SelectMany(d => d.EnumerateDevices()));
        }

        public static IEnumerable<UpnpDevice> FindByUdn(this UpnpRoot root, string udn)
        {
            return root.RootDevice.FindByUdn(udn);
        }

        public static IEnumerable<UpnpDevice> FindByUdn(this UpnpDevice device, string udn)
        {
            return device.EnumerateDevices().Where(d => d.UDN == udn);
        }

        #endregion

        #region Service Extensions

        public static IEnumerable<UpnpService> EnumerateServices(this UpnpRoot @this)
        {
            return @this.RootDevice.EnumerateServices();
        }

        public static IEnumerable<UpnpService> EnumerateServices(this UpnpDevice @this)
        {
            return @this.EnumerateDevices().SelectMany(device => device.Services);
        }

        public static IEnumerable<UpnpService> FindByServiceId(this UpnpRoot root, string id)
        {
            return root.RootDevice.FindByServiceId(id);
        }

        public static IEnumerable<UpnpService> FindByServiceId(this UpnpDevice device, string id)
        {
            return device.EnumerateServices().Where(s => s.Id == id);
        }

        public static IEnumerable<UpnpService> FindByServiceType(this UpnpRoot root, UpnpType type)
        {
            return root.RootDevice.FindByServiceType(type);
        }

        public static IEnumerable<UpnpService> FindByServiceType(this UpnpDevice device, UpnpType type)
        {
            return device.EnumerateServices().Where(service => service.Type.Equals(type));
        }

        #endregion
    }
}
