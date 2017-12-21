using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Benchmarks
{
    class NotifyManyPropertiesBoilerplate : INotifyManyProperties, INotifyPropertyChanged
    {
        private float f;
        private double d;
        private decimal @decimal;
        private string s;
        private byte b;
        private long l;
        private IntPtr intPtr;
        private FileAccess e;

        /// <inheritdoc />
        public float Float
        {
            get { return f; }
            set
            {
                if (f == value)
                    return;

                f = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public double Double
        {
            get { return d; }
            set
            {
                if (d == value)
                    return;

                d = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public decimal Decimal
        {
            get { return @decimal; }
            set
            {
                if (@decimal == value)
                    return;

                @decimal = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public string String
        {
            get { return s; }
            set
            {
                if (s == value)
                    return;

                s = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public byte Byte
        {
            get { return b; }
            set
            {
                if (b == value)
                    return;

                b = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public long Long
        {
            get { return l; }
            set
            {
                if (l == value)
                    return;

                l = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public IntPtr IntPtr
        {
            get => intPtr;
            set
            {
                if (intPtr == value)
                    return;

                intPtr = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public FileAccess Enum
        {
            get => e;
            set
            {
                if (e == value)
                    return;

                e = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
