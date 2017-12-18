namespace VanceStubbs.Tests
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using NUnit.Framework;
    using VanceStubbs.Tests.Types;

    [TestFixture]
    public class StubsTests
    {
        [Test]
        public void BlackHole()
        {
            IList<string> inst = VanceStubbs.Stubs.BlackHole<IList<string>>();
            Assert.AreEqual(inst.Count, 0);
            Assert.AreEqual(inst[0], null);
            inst.RemoveAt(0);
        }

        [Test]
        public void WhiteHole()
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
        public void WhiteHoleAbstractClass()
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

        [Test]
        public void Undefined()
        {
            Assert.Throws<NotImplementedException>(() =>
            {
                Console.WriteLine(new DateTime(2, 3, 5, VanceStubbs.Stubs.Undefined<Calendar>()));
            });
        }

        [Test]
        public void NotifyPropertyChangedProxy()
        {
            var proxy = VanceStubbs.Stubs.NotifyPropertyChangedProxy<IGetSetNotifyProperty>();
            proxy.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(IGetSetNotifyProperty.Value))
                {
                    Assert.Pass();
                }
            };
            proxy.Value = 42;
            Assert.Fail();
        }
    }
}
