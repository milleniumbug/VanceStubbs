namespace VanceStubbs.Tests.Types
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class AbstractPropertyConcreteINPCEvent : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public abstract int Value { get; }

        public virtual int NonAbstractButVirtual { get; set; }

        public abstract int GetSet { get; set; }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
