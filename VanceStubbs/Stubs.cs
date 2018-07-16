namespace VanceStubbs
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Reflection.Emit;

    public class Stubs
    {
        private readonly Factory factory;

        private readonly ConcurrentDictionary<Type, TypeInfo> whiteholes = new ConcurrentDictionary<Type, TypeInfo>();

        private readonly ConcurrentDictionary<Type, TypeInfo> blackholes = new ConcurrentDictionary<Type, TypeInfo>();

        internal Stubs(Factory factory)
        {
            this.factory = factory;
        }

        public static Stubs Factory => VanceStubbs.Factory.Default.OfStubs;

        public static T Undefined<T>()
        {
            throw new NotImplementedException("Undefined<T>() was evaluated");
        }

        public TAbstract WhiteHole<TAbstract>()
        {
            return (TAbstract)this.WhiteHole(typeof(TAbstract));
        }

        public object WhiteHole(Type type)
        {
            var concreteType = this.WhiteHoleType(type);
            return this.factory.Assembly.ActivateInstance(concreteType);
        }

        public Type WhiteHoleType(Type type)
        {
            return this.whiteholes.GetOrAdd(type, t => this.factory.Assembly.ImplementAbstractMethods("WhiteHole." + t.FullName, t, ImplementAsThrowing));
            void ImplementAsThrowing(MethodInfo originalMethod, ILGenerator il)
            {
                il.ThrowException(typeof(NotImplementedException));
            }
        }

        public TAbstract BlackHole<TAbstract>()
        {
            return (TAbstract)this.BlackHole(typeof(TAbstract));
        }

        public object BlackHole(Type type)
        {
            Type concreteType = this.BlackHoleType(type);
            return this.factory.Assembly.ActivateInstance(concreteType);
        }

        public Type BlackHoleType(Type type)
        {
            return this.blackholes.GetOrAdd(type, t => this.factory.Assembly.ImplementAbstractMethods("BlackHole." + t.FullName, t, ImplementAsReturnDefault));

            void ImplementAsReturnDefault(MethodInfo originalMethod, ILGenerator il)
            {
                if (originalMethod.ReturnType == typeof(void))
                {
                    il.Emit(OpCodes.Ret);
                    return;
                }

                if (!originalMethod.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ret);
                    return;
                }

                if (!originalMethod.ReturnType.IsPrimitive)
                {
                    il.Emit(OpCodes.Ldloca_S, (byte)0);
                    il.Emit(OpCodes.Initobj, originalMethod.ReturnType);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ret);
                    return;
                }

                var primitive = originalMethod.ReturnType.IsEnum
                    ? originalMethod.ReturnType.GetEnumUnderlyingType()
                    : originalMethod.ReturnType;
                if (primitive == typeof(float))
                {
                    il.Emit(OpCodes.Ldc_R4);
                    il.Emit(OpCodes.Ret);
                }
                else if (primitive == typeof(double))
                {
                    il.Emit(OpCodes.Ldc_R8);
                    il.Emit(OpCodes.Ret);
                }
                else if (primitive == typeof(decimal))
                {
                    il.Emit(OpCodes.Ldsfld, typeof(decimal).GetField(nameof(decimal.Zero)));
                    il.Emit(OpCodes.Ret);
                }
                else if (primitive == typeof(long))
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Conv_I8);
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ret);
                }
            }
        }
    }
}
