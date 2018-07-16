namespace VanceStubbs
{
    using System;
    using System.Collections.Concurrent;

    internal class TypeFactoryCache<Key> : Cache<Key, Type, Type>
    {
        private Factory factory;

        private readonly Func<Factory, Key, Type> typeFactory;

        public TypeFactoryCache(Func<Factory, Key, Type> typeFactory)
        {
            this.typeFactory = typeFactory;
            this.Drop();
        }

        protected override void AfterDrop()
        {
            this.factory = new Factory();
        }

        protected override Type Create(Key key)
        {
            return this.typeFactory(this.factory, key);
        }

        protected override Type FromCache(Type key)
        {
            return key;
        }
    }
}
