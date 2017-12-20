namespace VanceStubbs.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using VanceStubbs.Tests.Types;

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
            public void DontOverrideNonAbstract()
            {
                AbstractPropertyConcreteINPCEvent inst = VanceStubbs.Stubs.WhiteHole<AbstractPropertyConcreteINPCEvent>();
                Assert.Throws<NotImplementedException>(() =>
                {
                    var c = inst.Value;
                });
                inst.NonAbstractButVirtual = 42;
                Assert.AreEqual(42, inst.NonAbstractButVirtual);
            }

            [Test]
            public void Event()
            {
                IEvent e = VanceStubbs.Stubs.WhiteHole<IEvent>();
                Action a = () => { };
                e.Lol += a;
                e.Lol -= a;
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

            [Test]
            public void AllMethods()
            {
                IAllMethods inst = VanceStubbs.Stubs.WhiteHole<IAllMethods>();
            }
        }
    }
}
