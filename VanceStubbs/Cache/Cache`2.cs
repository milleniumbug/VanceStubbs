namespace VanceStubbs
{
    using System;
    using System.Collections.Concurrent;

    internal abstract class Cache<Key, CacheValue, Value> : ICache<Key, Value>
    {
        protected readonly ConcurrentDictionary<Key, CacheValue> cache = new ConcurrentDictionary<Key, CacheValue>();

        public void Drop()
        {
            foreach (var kvp in this.cache)
            {
                this.Retire(kvp.Key);
            }

            this.AfterDrop();
        }

        public Value Get(Key key)
        {
            this.BeforeGet(key);
            var value = this.cache.GetOrAdd(key, this.Create);
            this.AfterGet(key, value);
            return this.FromCache(value);
        }

        public void Dispose()
        {
            this.Drop();
        }

        protected void Retire(Key key)
        {
            if (this.cache.TryRemove(key, out CacheValue value))
            {
                var d = value as IDisposable;
                d?.Dispose();
            }
        }

        protected virtual void BeforeGet(Key key)
        {
        }

        protected virtual void AfterGet(Key key, CacheValue value)
        {
        }

        protected virtual void AfterDrop()
        {
        }

        protected abstract CacheValue Create(Key key);

        protected abstract Value FromCache(CacheValue key);
    }
}
