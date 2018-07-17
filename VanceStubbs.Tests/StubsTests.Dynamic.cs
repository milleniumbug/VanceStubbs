namespace VanceStubbs.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    public partial class StubsTests
    {
        [TestFixture]
        public class Dynamic
        {
            [Test]
            public void TypeLookupBasic()
            {
                var d = new TypeDictionary<string>(new Dictionary<Type, string>
                {
                    { typeof(IEnumerable<int>), "enumerable" },
                    { typeof(int), "int" },
                    { typeof(IList<int>), "list" }
                });
                Assert.AreEqual("list", d[typeof(List<int>)]);
                Assert.AreEqual("int", d[typeof(int)]);
                Assert.AreEqual("enumerable", d[typeof(IReadOnlyList<int>)]);
            }

            [Test]
            public void TypeLookupString()
            {
                var d = new TypeDictionary<string>(new Dictionary<Type, string>
                {
                    { typeof(IEnumerable<int>), "enumerable" },
                    { typeof(string), "string" },
                });
                Assert.AreEqual("enumerable", d[typeof(List<int>)]);
                Assert.IsFalse(d.ContainsKey(typeof(void)));
                Assert.AreEqual("string", d[typeof(string)]);
            }

            [Test]
            public void TypeLookupObject()
            {
                var d = new TypeDictionary<string>(new Dictionary<Type, string>
                {
                    { typeof(object), "object" }
                });
                Assert.AreEqual("object", d[typeof(List<int>)]);
                Assert.AreEqual("object", d[typeof(string)]);
            }
        }
    }
}
