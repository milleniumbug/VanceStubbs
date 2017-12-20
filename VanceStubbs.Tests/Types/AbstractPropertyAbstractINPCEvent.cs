namespace VanceStubbs.Tests.Types
{
    using System.ComponentModel;

    public abstract class AbstractPropertyAbstractINPCEvent : INotifyPropertyChanged
    {
        public abstract event PropertyChangedEventHandler PropertyChanged;

        public abstract int Value { get; }

        public virtual int NonAbstractButVirtual { get; set; }

        public abstract int GetSet { get; set; }
    }
}
