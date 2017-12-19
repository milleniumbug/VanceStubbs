namespace VanceStubbs.Tests
{
    using System;
    using System.Globalization;
    using NUnit.Framework;

    [TestFixture]
    public partial class StubsTests
    {
        [Test]
        public void Undefined()
        {
            Assert.Throws<NotImplementedException>(() =>
            {
                Console.WriteLine(new DateTime(2, 3, 5, VanceStubbs.Stubs.Undefined<Calendar>()));
            });
        }
    }
}
