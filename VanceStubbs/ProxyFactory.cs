namespace VanceStubbs
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class ProxyFactory
    {
        private static readonly ConcurrentDictionary<Type, TypeInfo> InpcProxies = new ConcurrentDictionary<Type, TypeInfo>();

        public static ProxyBuilder<TWrappedType> For<TWrappedType>()
        {
            var type = typeof(TWrappedType);
            if (!type.IsInterface)
            {
                throw new ArgumentException("the type parameter must be an interface", nameof(TWrappedType));
            }

            return new ProxyBuilder<TWrappedType>();
        }

        public static TAbstract NotifyPropertyChangedProxy<TAbstract>(object context = null)
        {
            return (TAbstract)NotifyPropertyChangedProxy(typeof(TAbstract), context);
        }

        public static INotifyPropertyChanged NotifyPropertyChangedProxy(Type type, object context = null)
        {
            var ab = DynamicAssembly.Default;
            var concreteType = InpcProxies.GetOrAdd(type, t =>
            {
                var tb = ab.Module.DefineType("INPC." + t.FullName, TypeAttributes.Class);
                tb.AddInterfaceImplementation(typeof(INotifyPropertyChanged));
                if (t.IsInterface)
                {
                    tb.AddInterfaceImplementation(t);
                }
                else
                {
                    tb.SetParent(t);
                }

                ab.DelegateAllConstructorsToBase(tb);

                var staticConstructor = tb.DefineTypeInitializer();
                var staticConstructorIl = staticConstructor.GetILGenerator();

                var ev = typeof(INotifyPropertyChanged)
                    .GetEvent(nameof(INotifyPropertyChanged.PropertyChanged));
                var inpcField = HasINPCImplemented(t) ? null : ab.ImplementEventByDelegatingToANewField(tb, ev);
                foreach (var property in ab.AbstractPropertiesFor(t))
                {
                    var staticComparer = ImplementNotifyProperty(tb, property, inpcField);
                    if (staticComparer != null)
                    {
                        staticConstructorIl.EmitCall(
                            OpCodes.Call,
                            typeof(EqualityComparer<>).MakeGenericType(property.PropertyType)
                                .GetMethod("get_Default"),
                            null);
                        staticConstructorIl.Emit(OpCodes.Stsfld, staticComparer);
                    }
                }

                staticConstructorIl.Emit(OpCodes.Ret);
                return tb.CreateTypeInfo();
            });
            return (INotifyPropertyChanged)(context == null
                ? ab.ActivateInstance(concreteType)
                : ab.ActivateInstance(concreteType, context));
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

        private static bool HasINPCImplemented(Type type)
        {
            if (type.IsInterface)
            {
                return false;
            }

            var ev = typeof(INotifyPropertyChanged)
                .GetEvent(nameof(INotifyPropertyChanged.PropertyChanged));
            return Enumerable.Where<KeyValuePair<MethodInfo, MethodInfo>>(InterfaceMapping(typeof(INotifyPropertyChanged), type), kvp => kvp.Key == ev.AddMethod || kvp.Key == ev.RemoveMethod)
                .All(kvp => !kvp.Value.IsAbstract);
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
                if (inpcEventField != null)
                {
                    RaiseEventByInvokingItDirectly(property, inpcEventField, il);
                }
                else
                {
                    var raiseEventMethod = tb.BaseType.GetMethod(
                        "OnPropertyChanged",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (raiseEventMethod == null)
                    {
                        raiseEventMethod = tb.BaseType.GetMethod(
                            "RaisePropertyChanged",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                    }

                    if (raiseEventMethod == null)
                    {
                        throw new ArgumentException(
                            "The class implements the event, but doesn't provide any recognisable method to raise it from a derived type.");
                    }

                    RaiseEventByCallingAnEventMethod(property, raiseEventMethod, il);
                }

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
                            @equals,
                            null);
                        il.Emit(OpCodes.Brfalse_S, label);
                    }
                }

                il.Emit(OpCodes.Ret);
                il.MarkLabel(label);
            }
        }

        private static IEnumerable<KeyValuePair<MethodInfo, MethodInfo>> InterfaceMapping(Type interfaceType, Type concreteType)
        {
            var mapping = concreteType.GetInterfaceMap(interfaceType);
            for (int i = 0; i < mapping.InterfaceMethods.Length; i++)
            {
                yield return new KeyValuePair<MethodInfo, MethodInfo>(mapping.InterfaceMethods[i], mapping.TargetMethods[i]);
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

        private static void RaiseEventByCallingAnEventMethod(PropertyInfo property, MethodInfo method, ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_0);
            var parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
            {
                il.Emit(OpCodes.Ldstr, property.Name);
            }
            else if (parameters.Length == 1 && parameters[0].GetType() == typeof(PropertyChangedEventArgs))
            {
                il.Emit(OpCodes.Ldstr, property.Name);
                il.Emit(OpCodes.Newobj, typeof(PropertyChangedEventArgs).GetConstructor(new[] { typeof(string) }));
            }
            else
            {
                throw new NotImplementedException();
            }

            var callOpcode = method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call;
            il.EmitCall(callOpcode, method, null);
        }

        private static void RaiseEventByInvokingItDirectly(PropertyInfo property, FieldInfo inpcEventField, ILGenerator il)
        {
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
        }

        public class ProxyBuilder<TWrappedType>
        {
            internal ProxyBuilder()
            {
            }

            public InstantiatedStatefulBuilder<TWrappedType, TState> WithState<TState>()
            {
                return new InstantiatedStatefulBuilder<TWrappedType, TState>();
            }

            public InstantiatedStatelessBuilder<TWrappedType> Stateless()
            {
                return new InstantiatedStatelessBuilder<TWrappedType>();
            }
        }

        public class InstantiatedStatelessBuilder<TWrappedType>
        {
            private readonly InstantiatedStatefulBuilder<TWrappedType, object> builder;

            internal InstantiatedStatelessBuilder()
            {
                this.builder = new InstantiatedStatefulBuilder<TWrappedType, object>();
            }

            public InstantiatedStatelessBuilder<TWrappedType> WithPreExitHandler(Func<TWrappedType, object, object> preExit)
            {
                this.builder.WithPreExitHandler((@this, state, result) => preExit(@this, result));
                return this;
            }

            public InstantiatedStatelessBuilder<TWrappedType> WithPostEntryHandler(Action<TWrappedType, object[]> postEntry)
            {
                this.builder.WithPostEntryHandler((@this, state, parameters) => postEntry(@this, parameters));
                return this;
            }

            public Func<TWrappedType, TWrappedType> Create()
            {
                var factory = this.builder.Create();
                return @this => factory(@this, null);
            }
        }

        public class InstantiatedStatefulBuilder<TWrappedType, TState>
        {
            private Func<TWrappedType, TState, object, object> preExit;

            private Action<TWrappedType, TState, object[]> postEntry;

            internal InstantiatedStatefulBuilder()
            {
            }

            public InstantiatedStatefulBuilder<TWrappedType, TState> WithPreExitHandler(Func<TWrappedType, TState, object, object> preExit)
            {
                if (preExit == null)
                {
                    throw new ArgumentNullException(nameof(preExit));
                }

                if (this.preExit == null)
                {
                    this.preExit = preExit;
                }
                else
                {
                    var previousHandler = this.preExit;
                    this.preExit = (@this, state, result) =>
                    {
                        result = previousHandler(@this, state, result);
                        return preExit(@this, state, result);
                    };
                }

                return this;
            }

            public InstantiatedStatefulBuilder<TWrappedType, TState> WithPostEntryHandler(Action<TWrappedType, TState, object[]> postEntry)
            {
                if (postEntry == null)
                {
                    throw new ArgumentNullException(nameof(postEntry));
                }

                if (this.postEntry == null)
                {
                    this.postEntry = postEntry;
                }
                else
                {
                    var previousHandler = this.postEntry;
                    this.postEntry = (@this, state, parameters) =>
                    {
                        postEntry(@this, state, parameters);
                        previousHandler(@this, state, parameters);
                    };
                }

                return this;
            }

            public Func<TWrappedType, TState, TWrappedType> Create()
            {
                var ab = DynamicAssembly.Default;
                var tb = ab.Module.DefineType(Guid.NewGuid().ToString(), TypeAttributes.Class);
                var abstractType = typeof(TWrappedType);
                tb.AddInterfaceImplementation(abstractType);

                var postEntryDelegateField = this.postEntry == null ? null : tb.DefineField(
                    nameof(this.postEntry),
                    this.postEntry.GetType(),
                    FieldAttributes.Private | FieldAttributes.Static);

                var preExitDelegateField = this.preExit == null ? null : tb.DefineField(
                    nameof(this.preExit),
                    this.preExit.GetType(),
                    FieldAttributes.Private | FieldAttributes.Static);

                var targetField = tb.DefineField(
                    "target",
                    abstractType,
                    FieldAttributes.InitOnly | FieldAttributes.Private);

                var stateField = tb.DefineField(
                    "state",
                    typeof(TState),
                    FieldAttributes.InitOnly | FieldAttributes.Private);
                {
                    var constructor = tb.DefineConstructor(
                        MethodAttributes.Public,
                        CallingConventions.HasThis,
                        new Type[] { targetField.FieldType, stateField.FieldType });
                    var il = constructor.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Stfld, targetField);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Stfld, stateField);
                    il.Emit(OpCodes.Ret);
                }

                ab.ImplementAbstractMethods(tb, abstractType, ImplementAsDecorator, false);
                var typeInfo = tb.CreateTypeInfo();
                var runtimePostEntryField = typeInfo.GetField(nameof(this.postEntry), BindingFlags.Static | BindingFlags.NonPublic);
                runtimePostEntryField?.SetValue(typeInfo, this.postEntry);
                var runtimePreExitField = typeInfo.GetField(nameof(this.preExit), BindingFlags.Static | BindingFlags.NonPublic);
                runtimePreExitField?.SetValue(typeInfo, this.preExit);
                return (target, state) => (TWrappedType)ab.ActivateInstance(typeInfo, target, state);

                void ImplementAsDecorator(MethodInfo method, ILGenerator il)
                {
                    il.DeclareLocal(typeof(object[]));
                    if (method.ReturnType != typeof(void))
                    {
                        il.DeclareLocal(method.ReturnType);
                    }

                    var parameters = method.GetParameters();
                    var length = parameters.Length;
                    if (postEntryDelegateField != null)
                    {
                        il.Emit(OpCodes.Ldc_I4, length);
                        il.Emit(OpCodes.Newarr, typeof(object));

                        for (int i = 0; i < length; ++i)
                        {
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Ldc_I4, i);
                            il.Emit(OpCodes.Ldarg, (short)(i + 1));
                            DynamicAssembly.TypeErase(il, parameters[i].ParameterType);
                            il.Emit(OpCodes.Stelem_Ref);
                        }

                        il.Emit(OpCodes.Stloc_0);
                        il.Emit(OpCodes.Ldsfld, postEntryDelegateField);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, targetField);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, stateField);
                        il.Emit(OpCodes.Ldloc_0);
                        il.EmitCall(OpCodes.Callvirt, postEntryDelegateField.FieldType.GetMethod("Invoke"), null);

                        for (int i = 0; i < length; ++i)
                        {
                            il.Emit(OpCodes.Ldloc_0);
                            il.Emit(OpCodes.Ldc_I4, i);
                            il.Emit(OpCodes.Ldelem_Ref);
                            DynamicAssembly.TypeRestore(il, parameters[i].ParameterType);
                            il.Emit(OpCodes.Starg, (short)(i + 1));
                        }
                    }

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, targetField);
                    DynamicAssembly.UnrollParameterLoading(il, 1, length);
                    il.EmitCall(OpCodes.Callvirt, method, null);

                    if (preExitDelegateField != null)
                    {
                        il.Emit(OpCodes.Stloc_1);
                        il.Emit(OpCodes.Ldsfld, preExitDelegateField);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, targetField);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, stateField);
                        il.Emit(OpCodes.Ldloc_1);
                        DynamicAssembly.TypeErase(il, method.ReturnType);
                        il.EmitCall(OpCodes.Callvirt, preExitDelegateField.FieldType.GetMethod("Invoke"), null);
                        DynamicAssembly.TypeRestore(il, method.ReturnType);
                    }

                    il.Emit(OpCodes.Ret);
                }
            }
        }
    }
}
