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
        }
    }
}
