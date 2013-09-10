using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Upnp.Extensions;
using Upnp.Gena;
using Upnp.Ssdp;

namespace Upnp.Upnp
{
    public class UpnpServer : IDisposable
    {
        public UpnpRoot Root { get; private set; }

        protected readonly ISsdpServer SsdpServer;
        protected readonly GenaServer GenaServer;
        
        public UpnpServer(UpnpRoot root, ISsdpServer ssdp = null, GenaServer gena = null)
        {
            this.Root = root;
            this.Root.ChildDeviceAdded += OnChildDeviceAdded;

            var sockets = SsdpSocketFactory.BuildSockets().ToArray();

            this.SsdpServer = ssdp ?? new SsdpServer(sockets);
            this.GenaServer = gena ?? new GenaServer();

            BuildAdvertisements();
        }

        private void OnChildDeviceAdded(object sender, EventArgs<UpnpDevice> eventArgs)
        {
            BuildAdvertisementsForDevice(eventArgs.Value);
        }

        protected ISsdpAnnouncer CreateAdvertisement(string notificationType, string usn)
        {
            var ad = this.SsdpServer.CreateAnnouncer(notificationType, usn);
            
            if(this.SsdpServer.IsListening)
                ad.Start();

            return ad;
        }

        protected void BuildAdvertisements()
        {
            CreateAdvertisement("upnp:rootdevice", string.Format("{0}::upnp:rootdevice", this.Root.RootDevice.UDN));

            BuildAdvertisementsForDevice(this.Root.RootDevice);
        }

        private void BuildAdvertisementsForDevice(UpnpDevice device)
        {
            var notificationType = device.UDN;
            var type = device.Type.ToString();

            var ad1 = CreateAdvertisement(notificationType, notificationType);
            var ad2 = CreateAdvertisement(type, string.Format("{0}::{1}", device.UDN, type));

            SetupOnRemovedHandler(device, ad1, ad2);

            foreach (var service in device.Services)
                BuildAdvertisementForService(service);

            foreach (var child in device.Devices)
                BuildAdvertisementsForDevice(child);
        }

        private void SetupOnRemovedHandler(UpnpDevice device, ISsdpAnnouncer ad1, ISsdpAnnouncer ad2)
        {
            EventHandler<EventArgs<UpnpDevice>> onRemoved = null;
            onRemoved = (sender, args) =>
            {
                ad1.Shutdown();
                ad2.Shutdown();

                this.SsdpServer.RemoveAnnouncer(ad1);
                this.SsdpServer.RemoveAnnouncer(ad2);

                device.Removed -= onRemoved;
            };

            device.Removed += onRemoved;
        }

        private void BuildAdvertisementForService(UpnpService service)
        {
            var ad = CreateAdvertisement (service.Type.ToString (), string.Format ("{0}::{1}", service.Device.UDN, service.Type));

            EventHandler<EventArgs<UpnpService>> onRemoved = null;
            onRemoved = (sender, args) =>
            {
                ad.Shutdown();

                this.SsdpServer.RemoveAnnouncer(ad);

                service.Removed -= onRemoved;
            };

            service.Removed += onRemoved;
        }
  
        public void StopListening()
        {
            this.SsdpServer.StopListening();
        }

        public void StartListening(params IPAddress[] groups)
        {
            this.SsdpServer.StartListening(groups);
        }

        public void Dispose()
        {
            this.Root.ChildDeviceAdded -= OnChildDeviceAdded;
            this.SsdpServer.Dispose();
        }
    }
}
