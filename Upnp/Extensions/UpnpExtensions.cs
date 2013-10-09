using System;
using System.Collections.Generic;
using System.Linq;
using Upnp.Upnp;

namespace Upnp.Extensions
{
    public static class UpnpExtensions
    {

        #region Device Extensions

        public static IEnumerable<UpnpDevice> WhereDevice(this UpnpRoot root, Func<UpnpDevice, bool> condition)
        {
            return root.RootDevice.WhereDevice(condition);
        }

        public static IEnumerable<UpnpDevice> WhereDevice(this UpnpDevice device, Func<UpnpDevice, bool> condition)
        {
            if (condition(device))
                yield return device;

            foreach (var result in device.Devices.SelectMany((child) => child.WhereDevice(condition)))
                yield return result;
        }

        public static IEnumerable<UpnpDevice> FindByUdn(this UpnpRoot root, string udn)
        {
            return root.WhereDevice((device) => device.UDN == udn);
        }

        public static IEnumerable<UpnpDevice> FindByUdn(this UpnpDevice device, string udn)
        {
            return device.WhereDevice((d) => d.UDN == udn);
        }

        public static IEnumerable<UpnpDevice> FindByDeviceType(this UpnpRoot root, UpnpType type)
        {
            return root.WhereDevice((device) => device.Type == type);
        }

        public static IEnumerable<UpnpDevice> FindByDeviceType(this UpnpDevice device, UpnpType type)
        {
            return device.WhereDevice((d) => d.Type == type);
        }

        #endregion

        #region Service Extensions

        public static IEnumerable<UpnpService> WhereService(this UpnpRoot root, Func<UpnpService, bool> condition)
        {
            return root.RootDevice.WhereService(condition);
        }

        public static IEnumerable<UpnpService> WhereService(this UpnpDevice device, Func<UpnpService, bool> condition)
        {
            foreach (var result in device.Services.Where((service) => condition(service)))
                yield return result;

            foreach (var result in device.Devices.SelectMany((child) => child.WhereService(condition)))
                yield return result;
        }

        public static IEnumerable<UpnpService> FindByServiceId(this UpnpRoot root, string id)
        {
            return root.WhereService((service) => service.Id == id);
        }

        public static IEnumerable<UpnpService> FindByServiceId(this UpnpDevice device, string id)
        {
            return device.WhereService((service) => service.Id == id);
        }

        public static IEnumerable<UpnpService> FindByServiceType(this UpnpRoot root, UpnpType type)
        {
            return root.WhereService((service) => service.Type == type);
        }

        public static IEnumerable<UpnpService> FindByServiceType(this UpnpDevice device, UpnpType type)
        {
            return device.WhereService((service) => service.Type == type);
        }

        #endregion

    }
}
