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
    public class UpnpSerializationTests
    {
        [Test]
        public void BasicDescriptionFileSerialization()
        {
            var ddf = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<root xmlns=\"urn:schemas-upnp-org:device-1-0\">\n  <specVersion>\n    <major>1</major>\n    <minor>0</minor>\n  </specVersion>\n  <device>\n    <deviceType>urn:schemas-upnp-org:device:Basic:1</deviceType>\n    <friendlyName>SM5200</friendlyName>\n    <manufacturer>Pelco</manufacturer>\n    <manufacturerURL>http://www.pelco.com</manufacturerURL>\n    <modelDescription>SM5200 Product</modelDescription>\n    <modelName>SM5200</modelName>\n    <modelNumber>SM5200</modelNumber>\n    <modelURL>http://www.pelco.com/</modelURL>\n    <serialNumber>000000</serialNumber>\n    <productSerial>[Added During Production Assembly]</productSerial>\n    <MAC>00:0B:AB:48:D6:7D</MAC>\n    <UDN>uuid:39011bdc-e9e7-408c-94c3-b14b2f4570a8</UDN>\n    <serviceList/>\n    <deviceList>\n      <device>\n        <deviceType>urn:schemas-pelco-com:device:Pelco:1</deviceType>\n        <friendlyName>System Manager 5200</friendlyName>\n        <manufacturer>Pelco</manufacturer>\n        <manufacturerURL>http://www.pelco.com/product/</manufacturerURL>\n        <UDN>uuid:7df1e4c6-ad15-407e-bfdf-48b9708e612b</UDN>\n        <serviceList>\n          <service>\n            <serviceType>urn:schemas-pelco-com:service:SoftwareUpdate:1</serviceType>\n            <serviceId>urn:pelco-com:serviceId:SoftwareUpdate-1</serviceId>\n            <SCPDURL>SCPD/SoftwareUpdate.xml</SCPDURL>\n            <controlURL>/control/SoftwareUpdate-1</controlURL>\n            <eventSubURL>/event/SoftwareUpdate-1</eventSubURL>\n          </service>\n          <service>\n            <serviceType>urn:schemas-pelco-com:service:PelcoConfiguration:1</serviceType>\n            <serviceId>urn:pelco-com:serviceId:PelcoConfiguration-1</serviceId>\n            <SCPDURL>SCPD/PelcoConfiguration.xml</SCPDURL>\n            <controlURL>/control/PelcoConfiguration-1</controlURL>\n            <eventSubURL>/event/PelcoConfiguration-1</eventSubURL>\n          </service>\n          <service>\n            <serviceType>urn:schemas-pelco-com:service:DiagnosticReporting:1</serviceType>\n            <serviceId>urn:pelco-com:serviceId:DiagnosticReporting-1</serviceId>\n            <SCPDURL>SCPD/DiagnosticReporting.xml</SCPDURL>\n            <controlURL>/control/DiagnosticReporting-1</controlURL>\n            <eventSubURL>/event/DiagnosticReporting-1</eventSubURL>\n          </service>\n        </serviceList>\n        <deviceList>\n          <device>\n            <deviceType>urn:schemas-pelco-com:device:SystemManagerDevice:1</deviceType>\n            <friendlyName>Endura System Manager 5200</friendlyName>\n            <UDN>uuid:9afeb125-4410-445d-ad54-6c02cf05d396</UDN>\n          </device>\n        </deviceList>\n      </device>\n    </deviceList>\n  </device>\n</root>";
            
            var root = new UpnpRoot();
            using (var reader = new XmlTextReader(new StringReader(ddf)))
            {
                root.ReadXml(reader);
            }

            Assert.AreEqual(1, root.UpnpMajorVersion);
            Assert.AreEqual(0, root.UpnpMinorVersion);
            Assert.IsNull(root.UrlBase);

            var rootDevice = root.RootDevice;
            Assert.IsNotNull(rootDevice);
            Assert.AreEqual("urn:schemas-upnp-org:device:Basic:1", rootDevice.Type.ToString());
            Assert.AreEqual("SM5200", rootDevice.FriendlyName);
            Assert.AreEqual("Pelco", rootDevice.Manufacturer);
            Assert.AreEqual("http://www.pelco.com", rootDevice.ManufacturerUrl);
            Assert.AreEqual("SM5200 Product", rootDevice.ModelDescription);
            Assert.AreEqual("SM5200", rootDevice.ModelName);
            Assert.AreEqual("SM5200", rootDevice.ModelNumber);
            Assert.AreEqual("http://www.pelco.com/", rootDevice.ModelUrl);
            Assert.AreEqual("000000", rootDevice.SerialNumber);
            Assert.AreEqual("[Added During Production Assembly]", rootDevice.Properties["productSerial"]);
            Assert.AreEqual("00:0B:AB:48:D6:7D", rootDevice.Properties["MAC"]);
            Assert.AreEqual("uuid:39011bdc-e9e7-408c-94c3-b14b2f4570a8", rootDevice.UDN.ToString());
            Assert.AreEqual(0, rootDevice.Services.Count);
            Assert.AreEqual(1, rootDevice.Devices.Count);

            var device = rootDevice.Devices.FirstOrDefault();
            Assert.NotNull(device);

        }

        [Test]
        public void ScpdDeserializationTest()
        {
            var scpd = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n<scpd xmlns=\"urn:schemas-upnp-org:service-1-0\">\n  <specVersion>\n    <major>1</major>\n    <minor>0</minor>\n  </specVersion>\n  <actionList>\n    <action>\n      <name>Update</name>\n      <argumentList>\n        <argument>\n          <name>modelName</name>\n          <direction>in</direction>\n          <relatedStateVariable>ARG_TYPE_modelName</relatedStateVariable>\n        </argument>\n        <argument>\n          <name>manifestURL</name>\n          <direction>in</direction>\n          <relatedStateVariable>ARG_TYPE_manifestURL</relatedStateVariable>\n        </argument>\n        <argument>\n          <name>updateURL</name>\n          <direction>in</direction>\n          <relatedStateVariable>ARG_TYPE_updateURL</relatedStateVariable>\n        </argument>\n        <argument>\n          <name>updating</name>\n          <direction>out</direction>\n          <relatedStateVariable>ARG_TYPE_updating</relatedStateVariable>\n        </argument>\n      </argumentList>\n    </action>\n    <action>\n      <name>GetModules</name>\n      <argumentList>\n        <argument>\n          <name>moduleCatalog</name>\n          <direction>out</direction>\n          <relatedStateVariable>ARG_TYPE_moduleCatalog</relatedStateVariable>\n        </argument>\n      </argumentList>\n    </action>\n  </actionList>\n  <serviceStateTable>\n    <stateVariable sendEvents=\"no\">\n      <name>ARG_TYPE_modelName</name>\n      <dataType>string</dataType>\n    </stateVariable>\n    <stateVariable sendEvents=\"no\">\n      <name>ARG_TYPE_manifestURL</name>\n      <dataType>uri</dataType>\n    </stateVariable>\n    <stateVariable sendEvents=\"no\">\n      <name>ARG_TYPE_updateURL</name>\n      <dataType>uri</dataType>\n    </stateVariable>\n    <stateVariable sendEvents=\"no\">\n      <name>ARG_TYPE_moduleCatalog</name>\n      <dataType>string</dataType>\n    </stateVariable>\n    <stateVariable sendEvents=\"no\">\n      <name>ARG_TYPE_updating</name>\n      <dataType>bool</dataType>\n    </stateVariable>\n  </serviceStateTable>\n</scpd>";
            var root = new UpnpServiceDescription();
            using (var reader = new XmlTextReader(new StringReader(scpd)))
            {
                root.ReadXml(reader);
            }
        }

        [Test]
        public void UniqueDeviceNameOperators()
        {
            string udn = UniqueDeviceName.Parse("uuid:39011bdc-e9e7-408c-94c3-b14b2f4570a8");
            Assert.AreEqual("uuid:39011bdc-e9e7-408c-94c3-b14b2f4570a8", udn);

            UniqueDeviceName udn2 = "uuid:39011bdc-e9e7-408c-94c3-b14b2f4570a8";
            Assert.AreEqual("39011bdc-e9e7-408c-94c3-b14b2f4570a8", udn2.Uuid.ToString());

            Assert.IsTrue(udn == udn2);
            Assert.IsTrue(udn2 == udn);
            Assert.IsTrue("uuid:C6E51CDA-DE50-408F-9B05-646C95D78423" != udn2);
            Assert.IsTrue(udn2 != "uuid:C6E51CDA-DE50-408F-9B05-646C95D78423");

            UniqueDeviceName nullUdn = null;
            string nullString = null;

            Assert.IsFalse("uuid:C6E51CDA-DE50-408F-9B05-646C95D78423" == nullUdn);
            Assert.IsFalse(nullUdn == "uuid:C6E51CDA-DE50-408F-9B05-646C95D78423");

            Assert.IsTrue(nullString == nullUdn);
            Assert.IsTrue(nullUdn == nullString);

            nullUdn = (string)null;
            Assert.IsNull(nullUdn);
        }

        [Test, ExpectedException(typeof(FormatException))]
        public void UniqueDeviceNameParseNullThrows()
        {
            UniqueDeviceName.Parse(null);
            
            Assert.Fail();
        }

        [Test]
        public void UniqueDeviceNameShouldBeEqual()
        {
            var guid = Guid.NewGuid();
            var udn = new UniqueDeviceName { Uuid = guid };
            var udn2 = new UniqueDeviceName { Uuid = new Guid(guid.ToByteArray()) };

            Assert.IsTrue(udn.Equals(udn2));
            Assert.IsTrue(udn2.Equals(udn));

            Assert.IsTrue(udn2.GetHashCode() == udn.GetHashCode());

            Assert.IsFalse(udn.Equals(null));
            Assert.IsTrue(udn.Equals(udn));
            Assert.IsFalse(udn.Equals("hello"));
        }

        [Test]
        public void UpnpTypeShouldBeEqual()
        {
            var type = new UpnpType();
            var type2 = new UpnpType();

            Assert.IsEmpty(type.Domain);
            Assert.IsEmpty(type.Kind);
            Assert.IsEmpty(type.Type);
            Assert.AreEqual(0, type.Version.Major);
            Assert.AreEqual(0, type.Version.Minor);

            Assert.IsEmpty(type2.Domain);
            Assert.IsEmpty(type2.Kind);
            Assert.IsEmpty(type2.Type);
            Assert.AreEqual(0, type2.Version.Major);
            Assert.AreEqual(0, type2.Version.Minor);

            Assert.IsTrue(type.Equals(type));
            Assert.IsTrue(type.Equals(type2));
            Assert.IsTrue(type2.Equals(type));

            Assert.IsTrue(type.Equals((object)type));
            Assert.IsTrue(type.Equals((object)type2));
            Assert.IsTrue(type2.Equals((object)type));


            Assert.IsTrue(type.GetHashCode() == type2.GetHashCode());

            Assert.IsFalse(type.Equals(null));
            Assert.IsFalse(type.Equals((object)null));
            Assert.IsFalse(type.Equals("hello"));
        }

        [Test, ExpectedException(typeof(FormatException))]
        public void UpnpTypeParseThrowsFormatException([Values("urn:hi:hi:hi", "hello", "3245034895723")] string input)
        {
            UpnpType.Parse(input);

            Assert.Fail();
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void UpnpTypeParseThrowsArgumentNullException([Values(null, "")] string input)
        {
            UpnpType.Parse(input);

            Assert.Fail();
        }
    }
}
