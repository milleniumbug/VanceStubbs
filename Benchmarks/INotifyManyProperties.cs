using System;
using System.ComponentModel;
using System.IO;

namespace Benchmarks
{
    public interface INotifyManyProperties : INotifyPropertyChanged
    {
        float Float { get; set; }

        double Double { get; set; }

        decimal Decimal { get; set; }

        string String { get; set; }

        byte Byte { get; set; }

        long Long { get; set; }

        IntPtr IntPtr { get; set; }

        FileAccess Enum { get; set; }
    }
}
