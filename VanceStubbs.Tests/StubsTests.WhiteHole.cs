namespace VanceStubbs.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;

    public partial class StubsTests
    {
        [TestFixture]
        public class WhiteHole
        {
            [Test]
            public void InterfaceListGeneric()
            {
                IList<int> inst = VanceStubbs.Stubs.WhiteHole<IList<int>>();
                Assert.Throws<NotImplementedException>(() =>
                {
                    var c = inst.Count;
                });
                Assert.Throws<NotImplementedException>(() =>
                {
                    inst[0] = 42;
                });
                Assert.Throws<NotImplementedException>(() =>
                {
                    var i = inst[0];
                });
            }

            [Test]
            public void AbstractClass()
            {
                Stream inst = VanceStubbs.Stubs.WhiteHole<Stream>();
                Assert.Throws<NotImplementedException>(() =>
                {
                    var c = inst.CanRead;
                });
            }

            [Test]
            public void MultipleInstantiatedGenerics()
            {
                IList<string> inst = VanceStubbs.Stubs.WhiteHole<IList<string>>();
                Assert.Throws<NotImplementedException>(() =>
                {
                    inst.RemoveAt(0);
                });
            }
        }
    }
}
