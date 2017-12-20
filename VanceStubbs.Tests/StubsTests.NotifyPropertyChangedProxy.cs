using System;

namespace VanceStubbs.Tests
{
    using System.Collections.ObjectModel;
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

            [Test]
            public void Advanced()
            {
                var proxy = VanceStubbs.Stubs.NotifyPropertyChangedProxy<INotifyManyProperties>();
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
                var proxy = VanceStubbs.Stubs
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
                var proxy = VanceStubbs.Stubs
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
            public void Abstract()
            {
                var proxy = VanceStubbs.Stubs
                    .NotifyPropertyChangedProxy<AbstractProperty>();

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
        }
    }
}
