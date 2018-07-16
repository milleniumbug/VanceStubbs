namespace VanceStubbs.Tests
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using NUnit.Framework;
    using VanceStubbs.Tests.Types;

    public partial class StubsTests
    {
        [TestFixture]
        public class NotifyPropertyChangedProxy
        {
            [Test]
            public void Basic()
            {
                var proxy = VanceStubbs.Proxies.Factory.NotifyPropertyChangedProxy<IGetSetNotifyProperty>();
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

            [Test]
            public void Advanced()
            {
                var proxy = VanceStubbs.Proxies.Factory.NotifyPropertyChangedProxy<INotifyManyProperties>();
                proxy.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(INotifyManyProperties.Long))
                    {
                        Assert.Pass();
                    }
                };
                proxy.Long = 42;
                Assert.Fail();
            }

            [Test]
            public void ReferenceEquals()
            {
                var proxy = VanceStubbs.Proxies.Factory
                    .NotifyPropertyChangedProxy<IGetSetNotifyPropertyGeneric<ObservableCollection<int>>>();

                proxy.Value = new ObservableCollection<int>(new[] { 1, 2, 3 });
                proxy.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(proxy.Value))
                    {
                        Assert.Pass();
                    }
                };
                proxy.Value = new ObservableCollection<int>();
                Assert.Fail();
            }

            [Test]
            public void Nullable()
            {
                var proxy = VanceStubbs.Proxies.Factory
                    .NotifyPropertyChangedProxy<IGetSetNotifyPropertyGeneric<DateTime?>>();

                proxy.Value = DateTime.MinValue;
                proxy.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(proxy.Value))
                    {
                        Assert.Pass();
                    }
                };
                proxy.Value = null;
                Assert.Fail();
            }

            [Test]
            public void AbstractClassWithConcreteEvent()
            {
                var proxy = VanceStubbs.Proxies.Factory
                    .NotifyPropertyChangedProxy<AbstractPropertyConcreteINPCEvent>();

                proxy.GetSet = 4;
                proxy.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(proxy.GetSet))
                    {
                        Assert.Pass();
                    }
                };
                proxy.GetSet = 16;
                Assert.Fail();
            }

            [Test]
            public void AbstractClassWithAbstractEvent()
            {
                var proxy = VanceStubbs.Proxies.Factory
                    .NotifyPropertyChangedProxy<AbstractPropertyAbstractINPCEvent>();

                proxy.GetSet = 4;
                proxy.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(proxy.GetSet))
                    {
                        Assert.Pass();
                    }
                };
                proxy.GetSet = 16;
                Assert.Fail();
            }

            [Test]
            public void DoesntExtendINPC()
            {
                var proxy = VanceStubbs.Proxies.Factory.NotifyPropertyChangedProxy<IGetSetProperty>();
                ((INotifyPropertyChanged)proxy).PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(IGetSetProperty.Value))
                    {
                        Assert.Pass();
                    }
                };
                proxy.Value = 42;
                Assert.Fail();
            }

            [Test]
            public void NonDefaultConstructible()
            {
                var proxy = VanceStubbs.Proxies.Factory.NotifyPropertyChangedProxy<NonDefaultConstructibleAbstractPropertyConcreteINPCEvent>(1337);
                proxy.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(proxy.GetSet))
                    {
                        Assert.Pass();
                    }
                };
                proxy.GetSet = 42;
                Assert.AreEqual(proxy.NonAbstractButVirtual, 1337);
                Assert.Fail();
            }

            [Test]
            public void ConstructorParameterDeathTest()
            {
                var proxy = VanceStubbs.Proxies.Factory.NotifyPropertyChangedProxy<INPCPropertyAbstractConstructorDeathTest>(1337);
                proxy.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(proxy.GetSet))
                    {
                        Assert.Pass();
                    }
                };
                proxy.GetSet = 42;
                Assert.AreEqual(proxy.NonAbstractButVirtual, 1337);
                Assert.Fail();
            }

            [Explicit]
            [Test]
            public void KillerTestInterfaces()
            {
                var types = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(asm => asm.ExportedTypes)
                    .Where(t => t.IsInterface);
                foreach (var type in types)
                {
                    var proxy = VanceStubbs.Proxies.Factory.NotifyPropertyChangedProxy(type);
                }
            }
        }
    }
}
