namespace VanceStubbs.Tests.Types
{
    public interface IGenericTypeWithGenericMethod<U>
    {
        T F<T>(U x);
    }
}
