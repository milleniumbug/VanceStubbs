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
            var f = VanceStubbs.ProxyFactory
                .For<ISimpleInterface>()
                .WithState<int>()
                .WithPreExitHandler((target, state, o) => o is int x ? x + state : o)
                .WithPostEntryHandler((target, state, parameters) => { })
                .Create();
            var v = new SimpleInterfaceImplementation();
            var proxy = f(v, 42);
            Assert.AreEqual(41, proxy.ReturnInt());
        }

        [Test]
        public void BasicStatefulObjectProxy()
        {
            var f = VanceStubbs.ProxyFactory
                .For<ISimpleInterface>()
                .WithState<object>()
                .WithPreExitHandler((target, state, o) => o is int x ? x + 42 : o)
                .WithPostEntryHandler((target, state, parameters) => { })
                .Create();
            var v = new SimpleInterfaceImplementation();
            var proxy = f(v, null);
            Assert.AreEqual(41, proxy.ReturnInt());
        }

        [Test]
        public void BasicStatelessProxy()
        {
            var f = VanceStubbs.ProxyFactory
                .For<ISimpleInterface>()
                .Stateless()
                .WithPreExitHandler((target, o) => o is int x ? x + 42 : o)
                .WithPostEntryHandler((target, parameters) => { })
                .Create();
            var v = new SimpleInterfaceImplementation();
            var proxy = f(v);
            Assert.AreEqual(41, proxy.ReturnInt());
        }

        [Test]
        public void Noop()
        {
            var f = VanceStubbs.ProxyFactory
                .For<ISimpleInterface>()
                .Stateless()
                .Create();
            var v = new SimpleInterfaceImplementation();
            var proxy = f(v);
            Assert.AreEqual(-1, proxy.ReturnInt());
        }

        [Test]
        public void ParameterModification()
        {
            var f = VanceStubbs.ProxyFactory
                .For<ISimpleInterface>()
                .WithState<int>()
                .WithPostEntryHandler((target, state, parameters) =>
                {
                    if (parameters.Length >= 1 && parameters[0] is int x)
                    {
                        parameters[0] = x + 1;
                    }
                })
                .Create();
            var v = new SimpleInterfaceImplementation();
            var proxy = f(v, 0);
            Assert.AreEqual(51, proxy.PassThrough(50));
        }

        [Test]
        public void Chaining()
        {
            var f = VanceStubbs.ProxyFactory
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
            Assert.AreEqual(-1, proxy.ReturnInt());
            CollectionAssert.AreEqual(new[] { "Out2", "Out1", "In1", "In2" }, s);
        }
    }
}
