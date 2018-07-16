namespace VanceStubbs.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using VanceStubbs.Tests.Types;

    public partial class StubsTests
    {
        [TestFixture]
        public class BlackHole
        {
            [Test]
            public void InterfaceListGeneric()
            {
                IList<string> inst = VanceStubbs.Stubs.Factory.BlackHole<IList<string>>();
                Assert.AreEqual(inst.Count, 0);
                Assert.AreEqual(inst[0], null);
                inst.RemoveAt(0);
            }

            [Test]
            public void AllMethods()
            {
                IAllMethods inst = VanceStubbs.Stubs.Factory.BlackHole<IAllMethods>();
                inst.Void();
                Assert.AreEqual(inst.Bool(), false);
                Assert.AreEqual(inst.Byte(), (byte)0);
                Assert.AreEqual(inst.Char(), (char)0);
                Assert.AreEqual(inst.IntPtr(), IntPtr.Zero);
            }
        }
    }
}
