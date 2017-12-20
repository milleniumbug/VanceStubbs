namespace VanceStubbs
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class Stubs
    {
        private static readonly ConcurrentDictionary<Type, TypeInfo> Whiteholes = new ConcurrentDictionary<Type, TypeInfo>();
        private static readonly ConcurrentDictionary<Type, TypeInfo> Blackholes = new ConcurrentDictionary<Type, TypeInfo>();
        private static readonly ConcurrentDictionary<Type, TypeInfo> InpcProxies = new ConcurrentDictionary<Type, TypeInfo>();

        public static TAbstract WhiteHole<TAbstract>()
        {
            return (TAbstract)WhiteHole(typeof(TAbstract));
        }

        public static object WhiteHole(Type type)
        {
            var ab = DynamicAssembly.Default;
            var concreteType = Whiteholes.GetOrAdd(type, t => ab.ImplementAbstractMethods("WhiteHole.", t, ImplementAsThrowing));
            return Activator.CreateInstance(concreteType);

            void ImplementAsThrowing(MethodInfo originalMethod, ILGenerator il)
            {
                il.ThrowException(typeof(NotImplementedException));
            }
        }

        public static T Undefined<T>()
        {
            throw new NotImplementedException("Undefined<T>() was evaluated");
        }

        public static TAbstract BlackHole<TAbstract>()
        {
            return (TAbstract)BlackHole(typeof(TAbstract));
        }

        public static object BlackHole(Type type)
        {
            var ab = DynamicAssembly.Default;
            var concreteType = Blackholes.GetOrAdd(type, t => ab.ImplementAbstractMethods("BlackHole.", t, ImplementAsReturnDefault));
            return Activator.CreateInstance(concreteType);

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

        public static TAbstract NotifyPropertyChangedProxy<TAbstract>()
            where TAbstract : INotifyPropertyChanged
        {
            return (TAbstract)NotifyPropertyChangedProxy(typeof(TAbstract));
        }

        public static INotifyPropertyChanged NotifyPropertyChangedProxy(Type type)
        {
            var ab = DynamicAssembly.Default;
            var concreteType = InpcProxies.GetOrAdd(type, t =>
            {
                var tb = ab.Module.DefineType("INPC." + t.FullName, TypeAttributes.Class);
                if (t.IsInterface)
                {
                    tb.AddInterfaceImplementation(t);
                }
                else
                {
                    tb.SetParent(t);
                }

                var staticConstructor = tb.DefineTypeInitializer();
                var staticConstructorIl = staticConstructor.GetILGenerator();

                var ev = type
                    .GetInterface(nameof(INotifyPropertyChanged))
                    .GetEvent(nameof(INotifyPropertyChanged.PropertyChanged));
                var inpcField = ab.ImplementEventByDelegatingToANewField(tb, ev);

                foreach (var property in ab.AbstractPropertiesFor(t))
                {
                    var staticComparer = ImplementNotifyProperty(tb, property, inpcField);
                    if (staticComparer != null)
                    {
                        staticConstructorIl.EmitCall(
                            OpCodes.Call,
                            typeof(EqualityComparer<>).MakeGenericType(property.PropertyType).GetMethod("get_Default"),
                            null);
                        staticConstructorIl.Emit(OpCodes.Stsfld, staticComparer);
                    }
                }
                staticConstructorIl.Emit(OpCodes.Ret);
                return tb.CreateTypeInfo();
            });
            return (INotifyPropertyChanged)Activator.CreateInstance(concreteType);
        }

        private static FieldInfo ImplementNotifyProperty(TypeBuilder tb, PropertyInfo property, FieldInfo inpcEventField)
        {
            var ab = DynamicAssembly.Default;
            var staticComparerType = typeof(EqualityComparer<>).MakeGenericType(property.PropertyType);
            var staticComparer = NeedsStaticEqualityComparer(property.PropertyType)
                ? tb.DefineField(
                    "comp" + property.PropertyType.GUID,
                    staticComparerType,
                    FieldAttributes.Static | FieldAttributes.Private | FieldAttributes.InitOnly)
                : null;
            var field = tb.DefineField(
                property.Name + "__backing_field" + Guid.NewGuid(),
                property.PropertyType,
                FieldAttributes.Private | FieldAttributes.SpecialName);
            ImplementGetter(tb, property, field);
            ImplementSetter(tb, property, inpcEventField, staticComparer, field);

            return staticComparer;

            bool NeedsStaticEqualityComparer(Type type)
            {
                return !UnorderedComparisonIsEnough(type) && type.GetMethod("op_Equality") == null;
            }
        }

        private static bool UnorderedComparisonIsEnough(Type type)
        {
            return type.IsPrimitive || !type.IsValueType || type.IsEnum;
        }

        private static void ImplementSetter(
            TypeBuilder tb,
            PropertyInfo property,
            FieldInfo inpcEventField,
            FieldBuilder staticComparer,
            FieldBuilder field)
        {
            {
                if (property.SetMethod == null)
                {
                    return;
                }

                var setter = tb.DefineMethod(
                    property.SetMethod.Name,
                    property.SetMethod.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.NewSlot),
                    property.SetMethod.CallingConvention,
                    property.SetMethod.ReturnType,
                    property.SetMethod.GetParameters().Select(p => p.ParameterType).ToArray());
                var il = setter.GetILGenerator();
                il.DeclareLocal(typeof(bool));
                if (staticComparer != null)
                {
                    il.Emit(OpCodes.Ldsfld, staticComparer);
                }

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ldarg_1);
                Equality(il, property.PropertyType, staticComparer);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, field);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, inpcEventField);
                il.Emit(OpCodes.Dup);
                var label = il.DefineLabel();
                il.Emit(OpCodes.Brtrue_S, label);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ret);
                il.MarkLabel(label);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, property.Name);
                il.Emit(OpCodes.Newobj, typeof(PropertyChangedEventArgs).GetConstructor(new[] { typeof(string) }));
                il.EmitCall(
                    OpCodes.Callvirt,
                    typeof(PropertyChangedEventHandler).GetMethod(nameof(PropertyChangedEventHandler.Invoke)),
                    null);
                il.Emit(OpCodes.Ret);
            }

            void Equality(ILGenerator il, Type type, FieldInfo comparer)
            {
                var label = il.DefineLabel();
                if (UnorderedComparisonIsEnough(type))
                {
                    il.Emit(OpCodes.Bne_Un_S, label);
                }
                else
                {
                    var operatorEquals = type.GetMethod("op_Equality");
                    if (operatorEquals != null)
                    {
                        il.EmitCall(
                            OpCodes.Call,
                            operatorEquals,
                            null);
                        il.Emit(OpCodes.Brfalse_S, label);
                    }
                    else
                    {
                        var equals = staticComparer.FieldType.GetMethod(
                            "Equals",
                            new[] { type, type });
                        il.EmitCall(
                            OpCodes.Callvirt,
                            equals,
                            null);
                        il.Emit(OpCodes.Brfalse_S, label);
                    }
                }

                il.Emit(OpCodes.Ret);
                il.MarkLabel(label);
            }
        }

        private static void ImplementGetter(
            TypeBuilder tb,
            PropertyInfo property,
            FieldBuilder field)
        {
            var getter = tb.DefineMethod(
                property.GetMethod.Name,
                property.GetMethod.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.NewSlot),
                property.GetMethod.CallingConvention,
                property.GetMethod.ReturnType,
                property.GetMethod.GetParameters().Select(p => p.ParameterType).ToArray());
            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
        }
    }
}
