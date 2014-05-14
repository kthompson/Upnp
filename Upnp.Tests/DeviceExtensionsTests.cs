using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using Upnp.Upnp;

namespace Upnp.Tests
{
    [TestFixture]
    public class DeviceExtensionsTests
    {
        [Test]
        public void RootCanEnumerateDevicesAndServices()
        {
            var ddf = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<root xmlns=\"urn:schemas-upnp-org:device-1-0\">\n  <specVersion>\n    <major>1</major>\n    <minor>0</minor>\n  </specVersion>\n  <device>\n    <deviceType>urn:schemas-upnp-org:device:Basic:1</deviceType>\n    <friendlyName>SM5200</friendlyName>\n    <manufacturer>Pelco</manufacturer>\n    <manufacturerURL>http://www.pelco.com</manufacturerURL>\n    <modelDescription>SM5200 Product</modelDescription>\n    <modelName>SM5200</modelName>\n    <modelNumber>SM5200</modelNumber>\n    <modelURL>http://www.pelco.com/</modelURL>\n    <serialNumber>000000</serialNumber>\n    <productSerial>[Added During Production Assembly]</productSerial>\n    <MAC>00:0B:AB:48:D6:7D</MAC>\n    <UDN>uuid:39011bdc-e9e7-408c-94c3-b14b2f4570a8</UDN>\n    <serviceList/>\n    <deviceList>\n      <device>\n        <deviceType>urn:schemas-pelco-com:device:Pelco:1</deviceType>\n        <friendlyName>System Manager 5200</friendlyName>\n        <manufacturer>Pelco</manufacturer>\n        <manufacturerURL>http://www.pelco.com/product/</manufacturerURL>\n        <UDN>uuid:7df1e4c6-ad15-407e-bfdf-48b9708e612b</UDN>\n        <serviceList>\n          <service>\n            <serviceType>urn:schemas-pelco-com:service:SoftwareUpdate:1</serviceType>\n            <serviceId>urn:pelco-com:serviceId:SoftwareUpdate-1</serviceId>\n            <SCPDURL>SCPD/SoftwareUpdate.xml</SCPDURL>\n            <controlURL>/control/SoftwareUpdate-1</controlURL>\n            <eventSubURL>/event/SoftwareUpdate-1</eventSubURL>\n          </service>\n          <service>\n            <serviceType>urn:schemas-pelco-com:service:PelcoConfiguration:1</serviceType>\n            <serviceId>urn:pelco-com:serviceId:PelcoConfiguration-1</serviceId>\n            <SCPDURL>SCPD/PelcoConfiguration.xml</SCPDURL>\n            <controlURL>/control/PelcoConfiguration-1</controlURL>\n            <eventSubURL>/event/PelcoConfiguration-1</eventSubURL>\n          </service>\n          <service>\n            <serviceType>urn:schemas-pelco-com:service:DiagnosticReporting:1</serviceType>\n            <serviceId>urn:pelco-com:serviceId:DiagnosticReporting-1</serviceId>\n            <SCPDURL>SCPD/DiagnosticReporting.xml</SCPDURL>\n            <controlURL>/control/DiagnosticReporting-1</controlURL>\n            <eventSubURL>/event/DiagnosticReporting-1</eventSubURL>\n          </service>\n        </serviceList>\n        <deviceList>\n          <device>\n            <deviceType>urn:schemas-pelco-com:device:SystemManagerDevice:1</deviceType>\n            <friendlyName>Endura System Manager 5200</friendlyName>\n            <UDN>uuid:9afeb125-4410-445d-ad54-6c02cf05d396</UDN>\n          </device>\n        </deviceList>\n      </device>\n    </deviceList>\n  </device>\n</root>";

            var root = new UpnpRoot();
            using (var reader = new XmlTextReader(new StringReader(ddf)))
            {
                root.ReadXml(reader);
            }

            var devices = root.EnumerateDevices().ToList();
            Assert.AreEqual(3, devices.Count);

            var services = root.EnumerateServices().ToList();
            Assert.AreEqual(3, services.Count);

            var rootServices = root.RootDevice.EnumerateServices().ToList();
            Assert.AreEqual(3, rootServices.Count);

            var smDeviceType = UpnpType.Parse("urn:schemas-pelco-com:device:SystemManagerDevice:1");
            var sm = root.FindByDeviceType(smDeviceType).FirstOrDefault();
            Assert.IsNotNull(sm);
            Assert.AreEqual(smDeviceType, sm.Type);
            Assert.AreEqual("uuid:9afeb125-4410-445d-ad54-6c02cf05d396", sm.UDN.ToString());

            sm = root.FindByUdn("uuid:9afeb125-4410-445d-ad54-6c02cf05d396").FirstOrDefault();
            Assert.IsNotNull(sm);
            Assert.AreEqual(smDeviceType, sm.Type);
            Assert.AreEqual("uuid:9afeb125-4410-445d-ad54-6c02cf05d396", sm.UDN.ToString());

            var serviceType = UpnpType.Parse("urn:schemas-pelco-com:service:PelcoConfiguration:1");
            var service = root.FindByServiceId("urn:pelco-com:serviceId:PelcoConfiguration-1").FirstOrDefault();
            Assert.IsNotNull(service);
            Assert.AreEqual("urn:schemas-pelco-com:service:PelcoConfiguration:1", service.Type.ToString());
            Assert.AreEqual("urn:pelco-com:serviceId:PelcoConfiguration-1", service.Id);

            service = root.FindByServiceType(serviceType).FirstOrDefault();
            Assert.IsNotNull(service);
            Assert.AreEqual(serviceType, service.Type);
            Assert.AreEqual("urn:pelco-com:serviceId:PelcoConfiguration-1", service.Id);
        }
    }
}
