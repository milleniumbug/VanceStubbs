namespace VanceStubbs.Tests
{
    using NUnit.Framework;
    using VanceStubbs.Tests.Types;

    public partial class StubsTests
    {
        [Ignore("Not implemented yet")]
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
        }
    }
}
