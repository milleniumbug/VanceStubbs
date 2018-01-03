namespace VanceStubbs
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class ProxyFactory
    {
        public static ProxyBuilder<TWrappedType> For<TWrappedType>()
        {
            var type = typeof(TWrappedType);
            if (!type.IsInterface)
            {
                throw new ArgumentException("the type parameter must be an interface", nameof(TWrappedType));
            }

            return new ProxyBuilder<TWrappedType>();
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
