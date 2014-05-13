using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Upnp.Upnp
{
    public static class UpnpExtensions
    {
        public static IEnumerable<UpnpDevice> FindByDeviceType(this UpnpRoot @this, UpnpType type)
        {
            var root = @this.RootDevice;
            if (root == null)
                yield break;

            if (root.Type.Equals(type))
                yield return root;

            foreach (var device in root.FindByDeviceType(type))
            {
                yield return device;
            }
        }

        public static IEnumerable<UpnpDevice> EnumerateDevices(this UpnpRoot @this)
        {
            return @this.RootDevice.EnumerateDevices();
        }

        public static IEnumerable<UpnpService> EnumerateServices(this UpnpRoot @this)
        {
            return @this.EnumerateDevices().SelectMany(d => d.Services);
        }

        public static IEnumerable<UpnpDevice> FindByDeviceType(this UpnpDevice @this, UpnpType type)
        {
            return @this.EnumerateDevices().Where(d => d.Type.Equals(type));
        }

        public static IEnumerable<UpnpDevice> EnumerateDevices(this UpnpDevice @this)
        {
            return new[] { @this }.Concat(@this.Devices.SelectMany(d => d.EnumerateDevices()));
        }

        public static IEnumerable<UpnpService> EnumerateServices(this UpnpDevice @this)
        {
            return @this.EnumerateDevices().SelectMany(device => device.Services);
        }
    }
}
