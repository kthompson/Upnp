using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using Upnp.Soap;

namespace Upnp.Tests
{
    [TestFixture]
    public class SoapTests
    {
        [Test]
        public void SerializationTest1()
        {
            var request1 = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<soapenv:Envelope\n        xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"\n        xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"\n        xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\n  <soapenv:Header>\n    <ns1:RequestHeader\n         soapenv:actor=\"http://schemas.xmlsoap.org/soap/actor/next\"\n         soapenv:mustUnderstand=\"0\"\n         xmlns:ns1=\"https://www.google.com/apis/ads/publisher/v201403\">\n      <ns1:authentication xsi:type=\"ns1:OAuth\">\n         <ns1:parameters>Bearer ****</ns1:parameters>\n      </ns1:authentication>\n      <ns1:networkCode>123456</ns1:networkCode>\n      <ns1:applicationName>DfpApi-Java-2.1.0-dfp_test</ns1:applicationName>\n    </ns1:RequestHeader>\n  </soapenv:Header>\n  <soapenv:Body>\n    <getAdUnitsByStatement xmlns=\"https://www.google.com/apis/ads/publisher/v201403\">\n      <filterStatement>\n        <query>WHERE parentId IS NULL LIMIT 500</query>\n      </filterStatement>\n    </getAdUnitsByStatement>\n  </soapenv:Body>\n</soapenv:Envelope>";
            var envelope = FromString(request1);

            Assert.AreEqual("getAdUnitsByStatement", envelope.Method);
            Assert.AreEqual("https://www.google.com/apis/ads/publisher/v201403", envelope.Namespace);
            Assert.AreEqual(1, envelope.Arguments.Count);
            Assert.IsTrue(envelope.Arguments.ContainsKey("filterStatement"));
            Assert.IsTrue(envelope.Headers.ContainsKey("RequestHeader"));
            var input = envelope.Arguments["filterStatement"].Trim();
            Assert.IsTrue(Regex.IsMatch(input, "<query(.*)?>WHERE parentId IS NULL LIMIT 500</query>"));
        }

        [Test]
        public void SerializationTest2()
        {
            var response1 = "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">\n  <soap:Header>\n    <ResponseHeader xmlns=\"https://www.google.com/apis/ads/publisher/v201403\">\n      <requestId>xxxxxxxxxxxxxxxxxxxx</requestId>\n      <responseTime>1063</responseTime>\n    </ResponseHeader>\n  </soap:Header>\n  <soap:Body>\n    <getAdUnitsByStatementResponse xmlns=\"https://www.google.com/apis/ads/publisher/v201403\">\n      <rval>\n        <totalResultSetSize>1</totalResultSetSize>\n        <startIndex>0</startIndex>\n        <results>\n          <id>2372</id>\n          <name>RootAdUnit</name>\n          <description></description>\n          <targetWindow>TOP</targetWindow>\n          <status>ACTIVE</status>\n          <adUnitCode>1002372</adUnitCode>\n          <inheritedAdSenseSettings>\n            <value>\n              <adSenseEnabled>true</adSenseEnabled>\n              <borderColor>FFFFFF</borderColor>\n              <titleColor>0000FF</titleColor>\n              <backgroundColor>FFFFFF</backgroundColor>\n              <textColor>000000</textColor>\n              <urlColor>008000</urlColor>\n              <adType>TEXT_AND_IMAGE</adType>\n              <borderStyle>DEFAULT</borderStyle>\n              <fontFamily>DEFAULT</fontFamily>\n              <fontSize>DEFAULT</fontSize>\n            </value>\n          </inheritedAdSenseSettings>\n        </results>\n      </rval>\n    </getAdUnitsByStatementResponse>\n  </soap:Body>\n</soap:Envelope>";
            var envelope = FromString(response1);

            Assert.AreEqual("getAdUnitsByStatementResponse", envelope.Method);
            Assert.AreEqual("https://www.google.com/apis/ads/publisher/v201403", envelope.Namespace);
            Assert.AreEqual(1, envelope.Arguments.Count);
            Assert.IsTrue(envelope.Arguments.ContainsKey("rval"));
            Assert.IsTrue(envelope.Headers.ContainsKey("ResponseHeader"));
        }

        [Test]
        public void RoundTripTest1()
        {
            var envelope = new SoapEnvelope
            {
                Arguments = new Dictionary<string, string>
                {
                    {"filterStatement", "test"},
                },
                Headers = new Dictionary<string, string>
                {
                    {"header1", "value"}
                },
                Method = "SomeMethod",
                Namespace = "https://somenamespace.com"
            };

            var s = new StringWriter();

            using (var writer = new XmlTextWriter(s))
            {
                //writer.Formatting = Formatting.Indented;
                //writer.Indentation = 2;
                //writer.IndentChar = ' ';
                envelope.WriteXml(writer);
            }

            var xml = s.ToString();

            var copy = FromString(xml);


            Assert.AreEqual(envelope.Method, copy.Method);
            Assert.AreEqual(envelope.Arguments.Count, copy.Arguments.Count);
            Assert.AreEqual(envelope.Headers.Count, copy.Headers.Count);

            foreach (var header in copy.Headers.Keys)
                Assert.AreEqual(envelope.Headers[header], copy.Headers[header]);

            foreach (var argument in copy.Arguments.Keys)
                Assert.AreEqual(envelope.Arguments[argument], copy.Arguments[argument]);
        }

        private SoapEnvelope FromString(string xml)
        {
            var x = new SoapEnvelope();
            using (var reader = new XmlTextReader(new StringReader(xml)))
            {
                x.ReadXml(reader);
            }

            return x;
        }
    }
}
