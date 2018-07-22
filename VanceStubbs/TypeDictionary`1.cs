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
        private List<TValue> values;

        private Dictionary<Type, int> directTypeMap;

        private dynamic dispatcher;

        private ICache<Type, Type> abstractTypeCache;

        private readonly DynamicAssembly assembly;

        public TypeDictionary(IEnumerable<KeyValuePair<Type, TValue>> source)
            : this(source, new Factory())
        {

        }

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
                if (key == typeof(object))
                {
                    hasObjectFallback = true;
                }

                // we need to special-case nullables
                // because you can't have a boxed instance of nullable type
                // that makes it impossible to call a F<T>(Nullable<T> x)
                // through dynamic
                if (key == typeof(Nullable<>))
                {
                    DefineDispatcherMethod("DispatchNullableHack", dispatcherTypeBuilder, retVal, key.GetTypeInfo().GenericTypeParameters[0]);
                }
                else if (Nullable.GetUnderlyingType(key) != null)
                {
                    DefineDispatcherMethod("DispatchNullableHack", dispatcherTypeBuilder, retVal, Nullable.GetUnderlyingType(key));
                }

                DefineDispatcherMethod("Dispatch", dispatcherTypeBuilder, retVal, key);

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
            var l = source.ToList();
            this.values = l.Select(kvp => kvp.Value).ToList();
            this.directTypeMap = l.Select((kvp, index) => new KeyValuePair<Type, int>(kvp.Key, index)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            this.abstractTypeCache = new TypeFactoryCache<Type>((fac, t) =>
            {
                return fac.OfStubs.BlackHoleType(t);
            });
        }

        private static void DefineDispatcherMethod(string name, TypeBuilder dispatcherTypeBuilder, int retVal, Type type)
        {
            var method = dispatcherTypeBuilder.DefineMethod(name, MethodAttributes.Public, CallingConventions.HasThis);
            method.SetReturnType(typeof(int));
            if (type.ContainsGenericParameters)
            {
                var sourceTypeParams = type
                    .GetTypeInfo()
                    .GenericTypeParameters;
                if (type.IsGenericParameter)
                {
                    sourceTypeParams = new Type[] { type };
                }

                var parameterBuilders = method.DefineGenericParameters(sourceTypeParams.Select(p => p.Name).ToArray());
                foreach (var pb in parameterBuilders.Zip(sourceTypeParams, (b, s) => new { b, s }))
                {
                    var constraints = pb.s.GetGenericParameterConstraints();
                    var baseTypeConstraint = constraints.Where(c => !c.IsInterface).SingleOrDefault();
                    if (baseTypeConstraint == typeof(ValueType))
                    {
                        pb.b.SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);
                    }

                    if (baseTypeConstraint != null)
                    {
                        pb.b.SetBaseTypeConstraint(baseTypeConstraint);
                    }

                    pb.b.SetInterfaceConstraints(constraints.Where(c => c.IsInterface).ToArray());
                }

                if (type.IsGenericTypeDefinition)
                {
                    type = type.MakeGenericType(parameterBuilders);
                }
                else if (type.IsGenericParameter)
                {
                    type = parameterBuilders[0];
                }
            }

            method.SetParameters(new Type[] { type });
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4, retVal);
            il.Emit(OpCodes.Ret);
        }

        private int Dispatch(Type key)
        {
            if (this.directTypeMap.TryGetValue(key, out int valueIndex))
            {
                return valueIndex;
            }

            if (key.IsAbstract || key.IsInterface)
            {
                key = this.abstractTypeCache.Get(key);
            }

            dynamic dummy;
            if (key == typeof(void))
            {
                return -1;
            }
            else if (key == typeof(string))
            {
                dummy = "";
            }
            else
            {
                var nullableUnderlying = Nullable.GetUnderlyingType(key);
                if (nullableUnderlying != null)
                {
                    var t = (Type)this.dispatcher.GetType();
                    dummy = FormatterServices.GetUninitializedObject(key);
                    return this.dispatcher.DispatchNullableHack(dummy);
                }
                else
                {
                    dummy = FormatterServices.GetUninitializedObject(key);
                }
            }

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
                    return this.values[r];
                }
            }
        }

        public IEnumerable<Type> Keys => this.directTypeMap.Keys;

        public IEnumerable<TValue> Values => this.values;

        public int Count => this.values.Count;

        public bool ContainsKey(Type key)
        {
            return this.Dispatch(key) != -1;
        }

        public IEnumerator<KeyValuePair<Type, TValue>> GetEnumerator()
        {
            return this.directTypeMap
                .Zip(this.values, (k, v) => new KeyValuePair<Type, TValue>(k.Key, v))
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
                value = this.values[r];
                return true;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
