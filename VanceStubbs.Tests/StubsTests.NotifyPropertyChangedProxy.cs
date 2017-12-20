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
                    if (args.PropertyName == nameof(IGetSetNotifyPropertyGeneric<ObservableCollection<int>>.Value))
                    {
                        Assert.Pass();
                    }
                };
                proxy.Value = new ObservableCollection<int>();
                Assert.Fail();
            }
        }
    }
}
