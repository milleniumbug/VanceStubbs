namespace VanceStubbs
{
    using System;

    internal interface ICache<K, V> : IDisposable
    {
        void Drop();

        V Get(K key);
    }
}
