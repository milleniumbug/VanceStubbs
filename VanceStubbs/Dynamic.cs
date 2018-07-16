namespace VanceStubbs
{
    using System;
    using System.Collections.Generic;

    public class Dynamic
    {
        private readonly Factory factory;

        internal Dynamic(Factory factory)
        {
            this.factory = factory;
        }

        public TypeDictionary<TValue> CreateTypeLookup<TValue>(IEnumerable<KeyValuePair<Type, TValue>> source)
        {
            return new TypeDictionary<TValue>(source, this.factory);
        }
    }
}
