namespace VanceStubbs
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    internal class LruCache<Key, Value> : Cache<Key, LinkedListNode<KeyValuePair<Key, Value>>, Value>
    {
        private readonly Func<Key, Value> factory;

        private readonly int size;

        private LinkedList<KeyValuePair<Key, Value>> leastRecentlyUsedOrder = new LinkedList<KeyValuePair<Key, Value>>();

        public LruCache(Func<Key, Value> factory, int size)
        {
            this.factory = factory;
            this.size = size;
        }

        protected override void AfterGet(Key key, LinkedListNode<KeyValuePair<Key, Value>> value)
        {
            lock (this.leastRecentlyUsedOrder)
            {
                this.leastRecentlyUsedOrder.AddFirst(value);
                if (this.leastRecentlyUsedOrder.Count > this.size)
                {
                    var last = this.leastRecentlyUsedOrder.Last;
                    this.Retire(last.Value.Key);
                    this.leastRecentlyUsedOrder.RemoveLast();
                }
            }
        }

        protected override LinkedListNode<KeyValuePair<Key, Value>> Create(Key key)
        {
            var node = new LinkedListNode<KeyValuePair<Key, Value>>(new KeyValuePair<Key, Value>(key, this.factory(key)));
            return node;
        }

        protected override Value FromCache(LinkedListNode<KeyValuePair<Key, Value>> key)
        {
            return key.Value.Value;
        }
    }
}
