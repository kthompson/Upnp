using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Upnp.Collections;

namespace Upnp.Tests
{
    [TestFixture]
    public class CollectionTests
    {
        [Test]
        public void CustomActionCollectionShouldRemoveAndClear()
        {
            var col = new CustomActionCollection<Tuple<string, int>>(_ => { }, _ => { })
            {
                Tuple.Create("hi1", 1),
                Tuple.Create("hi2", 2),
                Tuple.Create("hi3", 3)
            };

            Assert.AreEqual(3, col.Count);
            col.Remove(Tuple.Create("hi2", 2));

            Assert.AreEqual(2, col.Count);
            col.Clear();
            Assert.AreEqual(0, col.Count);
        }

        [Test]
        public void CustomActionCollectionShouldEnumerate()
        {
            var col = new CustomActionCollection<Tuple<string, int>>(_ => { }, _ => { })
            {
                Tuple.Create("hi1", 1),
                Tuple.Create("hi2", 2),
                Tuple.Create("hi3", 3)
            };

            Assert.AreEqual(3, col.Count);
            var i = 0;
            foreach (var tuple in col)
            {
                i++;
                Assert.AreEqual("hi" + i, tuple.Item1);
                Assert.AreEqual(i, tuple.Item2);
            }
            Assert.AreEqual(3, i);
        }

        [Test]
        public void CustomActionCollectionShouldContainItem()
        {
            var col = new CustomActionCollection<Tuple<string, string>>(_ => { }, _ => { });
            col.Add(Tuple.Create("hi", "hi"));

            Assert.AreEqual(1, col.Count);
            Assert.IsTrue(col.Contains(Tuple.Create("hi", "hi")));
            Assert.IsFalse(col.Contains(Tuple.Create("hi", "bye")));
        }



    }
}
