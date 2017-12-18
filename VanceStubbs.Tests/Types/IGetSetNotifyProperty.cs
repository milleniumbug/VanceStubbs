namespace VanceStubbs.Tests.Types
{
    using System.ComponentModel;

    public interface IGetSetNotifyProperty : INotifyPropertyChanged
    {
        int Value { get; set; }
    }
}