namespace VanceStubbs.Tests
{
    using System;
    using NUnit.Framework;
    using VanceStubbs.Tests.Types;

    [TestFixture]
    public class ProxyFactory
    {
        [Test]
        public void BasicStatefulProxy()
        {
            Func<ISimpleInterface, int, ISimpleInterface> f = VanceStubbs.ProxyFactory
                .For<ISimpleInterface>()
                .WithState<int>()
                .WithPreExitHandler((ISimpleInterface @this, int state, object o) => o is int x ? x + state : o)
                .WithPostEntryHandler((ISimpleInterface @this, int state, object[] parameters) => { })
                .Create();
            var v = new SimpleInterfaceImplementation();
            var proxy = f(v, 42);
            Assert.AreEqual(41, proxy.ReturnInt());
        }

        [Test]
        public void BasicStatefulObjectProxy()
        {
            Func<ISimpleInterface, object, ISimpleInterface> f = VanceStubbs.ProxyFactory
                .For<ISimpleInterface>()
                .WithState<object>()
                .WithPreExitHandler((ISimpleInterface @this, object state, object o) => o is int x ? x + 42 : o)
                .WithPostEntryHandler((ISimpleInterface @this, object state, object[] parameters) => { })
                .Create();
            var v = new SimpleInterfaceImplementation();
            var proxy = f(v, null);
            Assert.AreEqual(41, proxy.ReturnInt());
        }

        [Test]
        public void BasicStatelessProxy()
        {
            Func<ISimpleInterface, ISimpleInterface> f = VanceStubbs.ProxyFactory
                .For<ISimpleInterface>()
                .Stateless()
                .WithPreExitHandler((ISimpleInterface @this, object o) =>
                {
                    Console.WriteLine("lol");
                    var x = o as int?;
                    if (x.HasValue)
                    {
                        return x.Value + 42;
                    }
                    else
                    {
                        return o;
                    }
                })
                .WithPostEntryHandler((ISimpleInterface @this, object[] parameters) => { })
                .Create();
            var v = new SimpleInterfaceImplementation();
            var proxy = f(v);
            Assert.AreEqual(41, v.ReturnInt());
        }
    }
}
