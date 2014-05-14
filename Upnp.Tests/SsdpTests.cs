using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Upnp.Ssdp;

namespace Upnp.Tests
{
    [TestFixture]
    public class ClientTests
    {
        readonly object mutex = new object();

        [Test]
        public void AnnoucementTest()
        {
            using (var client = new SsdpClient())
            using (var server = new SsdpServer())
            {
                client.ServiceFound += TestResultFound;
                client.StartListening();

                lock (mutex)
                {
                    server.CreateAnnouncer("urn:schemas-upnp-org:service:test:1", "uuid:979F4CE8-64AF-4653-B207-D7514908356F::urn:schemas-upnp-org:service:test:1");
                    server.StartListening();

                    if (!Monitor.Wait(mutex, TimeSpan.FromSeconds(30)))
                    {
                        Assert.Fail("The announcement timed out.");
                    }
                }
            }
        }

        [Test]
        public void AsyncSearchTest()
        {
            using (var client = new SsdpClient())
            using (var server = new SsdpServer())
            {
                server.CreateAnnouncer("urn:schemas-upnp-org:service:test:1", "uuid:979F4CE8-64AF-4653-B207-D7514908356F::urn:schemas-upnp-org:service:test:1");
                server.StartListening();

                client.StartListening();

                var search = client.CreateSearch(false);
                search.SearchType = "urn:schemas-upnp-org:service:test:1";
                search.ResultFound += (sender, e) =>
                {
                    lock (mutex)
                    {
                        if (IsTestAnnouncement(e.Value))
                            Monitor.Pulse(mutex);
                    }
                };

                lock (mutex)
                {
                    search.SearchAsync();

                    if (!Monitor.Wait(mutex, TimeSpan.FromSeconds(30)))
                    {
                        Assert.Fail("The announcement timed out.");
                    }
                }
            }
        }

        [Test]
        public void RootDeviceAsyncSearchTest()
        {
            using (var client = new SsdpClient())
            using (var server = new SsdpServer())
            {
                server.CreateAnnouncer("upnp:rootdevice", "uuid:979F4CE8-64AF-4653-B207-D7514908356F::upnp:rootdevice");
                server.StartListening();

                client.StartListening();

                var search = client.CreateSearch(false);
                search.SearchType = "upnp:rootdevice";
                search.ResultFound += RootResultFound;

                lock (mutex)
                {
                    search.SearchAsync();

                    if (!Monitor.Wait(mutex, TimeSpan.FromSeconds(30)))
                    {
                        Assert.Fail("The announcement timed out.");
                    }
                }
            }
        }

        [Test]
        public void RootDeviceSearchTest()
        {
            using (var client = new SsdpClient())
            using (var server = new SsdpServer())
            {
                server.CreateAnnouncer("upnp:rootdevice", "uuid:979F4CE8-64AF-4653-B207-D7514908356F::upnp:rootdevice");
                server.StartListening();
                client.StartListening();

                var search = client.CreateSearch(false);
                search.SearchType = "upnp:rootdevice";
                var results = search.Search();
                Assert.AreEqual(0, results.Count(msg => msg.Type != "upnp:rootdevice"));
                Assert.That(results.Exists(IsRootAnnouncement));
            }
        }

        [Test] 
        public void SearchShouldRespondToSearchTypeAll()
        {
            using (var client = new SsdpClient())
            using (var server = new SsdpServer())
            {
                server.CreateAnnouncer("urn:schemas-upnp-org:service:test:1", "uuid:979F4CE8-64AF-4653-B207-D7514908356F::urn:schemas-upnp-org:service:test:1");
                server.StartListening();

                client.StartListening();

                var search = client.CreateSearch(false);
                search.SearchType = Protocol.SsdpAll;
                var results = search.Search();
                Assert.That(results.Exists(IsTestAnnouncement));
            }
        }

        [Test]
        public void SearchShouldRespondToDefaultSearchType()
        {
            using (var client = new SsdpClient())
            using (var server = new SsdpServer())
            {
                server.CreateAnnouncer("urn:schemas-upnp-org:service:test:1", "uuid:979F4CE8-64AF-4653-B207-D7514908356F::urn:schemas-upnp-org:service:test:1");
                server.StartListening();

                client.StartListening();

                var search = client.CreateSearch(false);
                var results = search.Search();
                Assert.That(results.Exists(IsTestAnnouncement));
            }
        }

        void TestResultFound(object sender, EventArgs<SsdpMessage> e)
        {
            lock (mutex)
            {
                if (IsTestAnnouncement(e.Value))
                    Monitor.Pulse(mutex);
            }
        }

        void RootResultFound(object sender, EventArgs<SsdpMessage> e)
        {
            lock (mutex)
            {
                if (IsRootAnnouncement(e.Value))
                    Monitor.Pulse(mutex);
            }
        }

        static bool IsRootAnnouncement(SsdpMessage msg)
        {
            return msg.IsRoot &&
                   msg.Type == "upnp:rootdevice" &&
                   msg.USN == "uuid:979F4CE8-64AF-4653-B207-D7514908356F::upnp:rootdevice";
        }

        static bool IsTestAnnouncement(SsdpMessage msg)
        {
            return msg.IsService && 
                   msg.Type == "urn:schemas-upnp-org:service:test:1" && 
                   msg.USN == "uuid:979F4CE8-64AF-4653-B207-D7514908356F::urn:schemas-upnp-org:service:test:1";
        }

        [Test]
        public void SynchronousAnnouncementTest()
        {
            using (var client = new SsdpClient())
            using (var server = new SsdpServer())
            {
                client.Filter = message => message.Type == "urn:schemas-upnp-org:service:test:1";
                client.StartListening();

                server.CreateAnnouncer("urn:schemas-upnp-org:service:test:1", "uuid:979F4CE8-64AF-4653-B207-D7514908356F::urn:schemas-upnp-org:service:test:1");
                server.StartListening();

                var result = client.FindFirst(TimeSpan.FromSeconds(30));
                Assert.IsNotNull(result);
                Assert.That(IsTestAnnouncement(result));
            }
        }

        [Test]
        public void FilteredAnnouncementTest()
        {
            using (var client = new SsdpClient())
            using (var server = new SsdpServer())
            {
                client.ServiceFound += TestResultFound;
                client.Filter = message => message.Type == "urn:schemas-upnp-org:service:test:1";
                client.StartListening();

                lock (mutex)
                {
                    server.CreateAnnouncer("urn:schemas-upnp-org:service:test:1", "uuid:979F4CE8-64AF-4653-B207-D7514908356F::urn:schemas-upnp-org:service:test:1");
                    server.StartListening();

                    if (!Monitor.Wait(mutex, TimeSpan.FromSeconds(30)))
                    {
                        Assert.Fail("The announcement timed out.");
                    }
                }
            }
        }

        [Test]
        public void FilteredAnnouncementTest2()
        {
            using (var client = new SsdpClient())
            using (var server = new SsdpServer())
            {
                client.Filter = message => message.Type == "urn:schemas-upnp-org:service:test:1";
                client.StartListening();

                client.ServiceFound += (sender, args) =>
                {
                    lock (mutex)
                    {
                        if (args.Value.Type != "urn:schemas-upnp-org:service:test:1")
                        {
                            Monitor.Pulse(mutex);
                        }
                    }
                };
                

                lock (mutex)
                {
                    server.CreateAnnouncer("upnp:test:fail", "uuid:upnp-tests:test1");
                    server.CreateAnnouncer("upnp", "uuid:upnp-tests:test2");
                    server.CreateAnnouncer("urn:schemas-upnp-org:service:test:1", "uuid:979F4CE8-64AF-4653-B207-D7514908356F::urn:schemas-upnp-org:service:test:1");
                    server.StartListening();
                    
                    if (Monitor.Wait(mutex, TimeSpan.FromSeconds(10)))
                    {
                        Assert.Fail("The client received invalid announcements.");
                    }
                }
            }
        }
    }
}
