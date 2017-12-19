namespace VanceStubbs.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;

    public partial class StubsTests
    {
        [TestFixture]
        public class BlackHole
        {
            [Test]
            public void InterfaceListGeneric()
            {
                IList<string> inst = VanceStubbs.Stubs.BlackHole<IList<string>>();
                Assert.AreEqual(inst.Count, 0);
                Assert.AreEqual(inst[0], null);
                inst.RemoveAt(0);
            }
        }
    }
}
