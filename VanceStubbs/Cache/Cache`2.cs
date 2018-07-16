namespace VanceStubbs
{
    using System;
    using System.Collections.Concurrent;

    public abstract class Cache<K, V> : IDisposable
    {
        protected readonly ConcurrentDictionary<K, V> cache = new ConcurrentDictionary<K, V>();

        public void Drop()
        {
            foreach (var kvp in this.cache)
            {
                if (this.cache.TryRemove(kvp.Key, out V value))
                {
                    var d = value as IDisposable;
                    d?.Dispose();
                }
            }

            this.AfterDrop();
        }

        public V Get(K key)
        {
            this.BeforeGet(key);
            var value = this.cache.GetOrAdd(key, this.Create);
            this.AfterGet(key);
            return value;
        }

        public void Dispose()
        {
            this.Drop();
        }

        protected virtual void BeforeGet(K key)
        {
        }

        protected virtual void AfterGet(K key)
        {
        }

        protected virtual void AfterDrop()
        {
        }

        protected abstract V Create(K key);
    }
}
