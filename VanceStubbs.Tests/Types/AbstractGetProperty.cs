namespace VanceStubbs.Tests.Types
{
    public abstract class AbstractGetProperty
    {
        public abstract int Value { get; }

        public virtual int NonAbstractButVirtual { get; set; }
    }
}
