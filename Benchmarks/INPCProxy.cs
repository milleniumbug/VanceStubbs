
namespace Benchmarks
{
    using BenchmarkDotNet.Attributes;
    using System.IO;

    [BenchmarkCategory]
    public class INPCProxy
    {
        [Benchmark]
        public void Proxy()
        {
            var a = VanceStubbs.Proxies.NotifyPropertyChangedProxy<INotifyManyProperties>();
            a.PropertyChanged += (sender, args) => { };
            for (int i = 0; i < 100; i++)
            {
                a.Byte = (byte)i;
                a.Enum = FileAccess.Read;
                a.Double = i * 100.0;
            }
        }

        [Benchmark(Baseline = true)]
        public void Boilerplate()
        {
            var a = new NotifyManyPropertiesBoilerplate();
            a.PropertyChanged += (sender, args) => { };
            for (int i = 0; i < 100; i++)
            {
                a.Byte = (byte)i;
                a.Enum = FileAccess.Read;
                a.Double = i * 100.0;
            }
        }
    }
}
