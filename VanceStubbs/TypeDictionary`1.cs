namespace VanceStubbs
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;

    public class TypeDictionary<TValue> : IReadOnlyDictionary<Type, TValue>
    {
        private IReadOnlyList<TValue> underlyingCollection;

        private IReadOnlyList<Type> originalTypes;

        private dynamic dispatcher;

        private Cache<Type, int> lookupCache;

        private Cache<Type, Type> abstractTypeCache;

        private readonly DynamicAssembly assembly;

        internal TypeDictionary(IEnumerable<KeyValuePair<Type, TValue>> source, Factory factory)
        {
            var dispatcherTypeBuilder = factory.Assembly.Module.DefineType("T" + Guid.NewGuid().ToString().Replace('-', '_'), TypeAttributes.Public | TypeAttributes.AutoClass, typeof(object));
            int retVal = 0;
            bool hasObjectFallback = false;
            var underlyingCollection = new List<TValue>();
            var originalTypes = new List<Type>();
            foreach (var kvp in source)
            {
                var key = kvp.Key;
                var value = kvp.Value;
                Type type;
                if (key.ContainsGenericParameters)
                {
                    throw new NotImplementedException();

                    // var gen = key.GenericTypeArguments.Where(t => t.IsGenericParameter);
                }
                else
                {
                    if (key == typeof(object))
                    {
                        hasObjectFallback = true;
                    }

                    type = key;
                }

                var method = dispatcherTypeBuilder.DefineMethod("Dispatch", MethodAttributes.Public, CallingConventions.HasThis);
                method.SetReturnType(typeof(int));
                method.SetParameters(new Type[] { type });
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldc_I4, retVal);
                il.Emit(OpCodes.Ret);

                originalTypes.Add(key);
                underlyingCollection.Add(value);
                retVal++;
            }

            if (hasObjectFallback)
            {
                var method = dispatcherTypeBuilder.DefineMethod("Dispatch", MethodAttributes.Public, CallingConventions.HasThis, typeof(int), new[] { typeof(object) });
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldc_I4, -1);
                il.Emit(OpCodes.Ret);
            }

            TypeInfo dispatcherType = dispatcherTypeBuilder.CreateTypeInfo();
            this.dispatcher = Activator.CreateInstance(dispatcherType);
            this.underlyingCollection = underlyingCollection.AsReadOnly();
            this.originalTypes = originalTypes.AsReadOnly();
        }

        private int Dispatch(Type key)
        {
            dynamic dummy = FormatterServices.GetUninitializedObject(key);
            return this.dispatcher.Dispatch(dummy);
        }

        public TValue this[Type key]
        {
            get
            {
                int r = this.Dispatch(key);
                if (r == -1)
                {
                    throw new KeyNotFoundException();
                }
                else
                {
                    return this.underlyingCollection[r];
                }
            }
        }

        public IEnumerable<Type> Keys => this.originalTypes;

        public IEnumerable<TValue> Values => this.underlyingCollection;

        public int Count => this.underlyingCollection.Count;

        public bool ContainsKey(Type key)
        {
            return this.Dispatch(key) == -1;
        }

        public IEnumerator<KeyValuePair<Type, TValue>> GetEnumerator()
        {
            return this.originalTypes
                .Zip(this.underlyingCollection, (k, v) => new KeyValuePair<Type, TValue>(k, v))
                .GetEnumerator();
        }

        public bool TryGetValue(Type key, out TValue value)
        {
            int r = this.Dispatch(key);
            if (r == -1)
            {
                value = default(TValue);
                return false;
            }
            else
            {
                value = this.underlyingCollection[r];
                return true;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
