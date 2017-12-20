namespace VanceStubbs.Tests.Types
{
    using System.ComponentModel;

    public interface IGetSetNotifyPropertyGeneric<T> : INotifyPropertyChanged
    {
        T Value { get; set; }
    }
}
