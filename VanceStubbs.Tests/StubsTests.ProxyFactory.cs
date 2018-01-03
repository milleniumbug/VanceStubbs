namespace VanceStubbs.Tests
{
    using System;
    using System.Collections.Generic;
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
                .WithPreExitHandler((ISimpleInterface @this, object o) => o is int x ? x + 42 : o)
                .WithPostEntryHandler((ISimpleInterface @this, object[] parameters) => { })
                .Create();
            var v = new SimpleInterfaceImplementation();
            var proxy = f(v);
            Assert.AreEqual(41, v.ReturnInt());
        }

        [Test]
        public void Noop()
        {
            Func<ISimpleInterface, ISimpleInterface> f = VanceStubbs.ProxyFactory
                .For<ISimpleInterface>()
                .Stateless()
                .Create();
            var v = new SimpleInterfaceImplementation();
            var proxy = f(v);
            Assert.AreEqual(-1, v.ReturnInt());
        }

        [Test]
        public void Chaining()
        {
            Func<ISimpleInterface, List<string>, ISimpleInterface> f = VanceStubbs.ProxyFactory
                .For<ISimpleInterface>()
                .WithState<List<string>>()
                .WithPostEntryHandler((target, state, parameters) => state.Add("Out1"))
                .WithPostEntryHandler((target, state, parameters) => state.Add("Out2"))
                .WithPreExitHandler((target, state, ret) =>
                {
                    state.Add("In1");
                    return ret;
                })
                .WithPreExitHandler((target, state, ret) =>
                {
                    state.Add("In2");
                    return ret;
                })
                .Create();
            var v = new SimpleInterfaceImplementation();
            var s = new List<string>();
            var proxy = f(v, s);
            Assert.AreEqual(-1, v.ReturnInt());
            CollectionAssert.AreEqual(new[]{ "Out2", "Out1", "In1", "In2" }, s);
        }
    }
}
