namespace VanceStubbs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    internal class DynamicAssembly
    {
        public static readonly DynamicAssembly Default = new DynamicAssembly();

        public DynamicAssembly()
        {
            this.Assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("VanceStubbs" + Guid.NewGuid()), AssemblyBuilderAccess.RunAndCollect);
            this.Module = this.Assembly.DefineDynamicModule("proxiesmodule");
        }

        public AssemblyBuilder Assembly { get; }

        public ModuleBuilder Module { get; }

        public TypeInfo ImplementAbstractMethods(string prefix, Type abstractType, Action<MethodInfo, ILGenerator> implementer)
        {
            var tb = this.Module.DefineType(prefix + abstractType.FullName, TypeAttributes.Class);
            if (abstractType.IsInterface)
            {
                tb.AddInterfaceImplementation(abstractType);
            }
            else
            {
                tb.SetParent(abstractType);
            }

            foreach (var eventInfo in this.AbstractEventsFor(abstractType))
            {
                this.ImplementEventByDelegatingToANewField(tb, eventInfo);
            }

            foreach (var method in this.AbstractMethodsFor(abstractType, skipEventMethods: true))
            {
                var methodOverride = tb.DefineMethod(
                    method.Name,
                    method.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.NewSlot),
                    method.CallingConvention,
                    method.ReturnType,
                    method.GetParameters().Select(p => p.ParameterType).ToArray());
                implementer(method, methodOverride.GetILGenerator());
            }

            return tb.CreateTypeInfo();
        }

        public void ImplementEventByDelegatingToANewField(TypeBuilder tb, EventInfo e)
        {
            var fb = tb.DefineField(e.Name, e.EventHandlerType, FieldAttributes.Family);
            this.ImplementEventWithField(tb, e, fb);
        }

        public void ImplementEventWithField(TypeBuilder tb, EventInfo e, FieldInfo field)
        {
            {
                var addMethod = tb.DefineMethod(
                    "add_" + e.Name,
                    MethodAttributes.Virtual | MethodAttributes.SpecialName,
                    CallingConventions.HasThis,
                    typeof(void),
                    new[] { e.EventHandlerType });
                ImplementMethod(
                    addMethod.GetILGenerator(),
                    typeof(Delegate).GetMethod(nameof(Delegate.Combine), new[] { typeof(Delegate), typeof(Delegate) }));
            }

            {
                var removeMethod = tb.DefineMethod(
                    "remove_" + e.Name,
                    MethodAttributes.Virtual | MethodAttributes.SpecialName,
                    CallingConventions.HasThis,
                    typeof(void),
                    new[] { e.EventHandlerType });
                ImplementMethod(
                    removeMethod.GetILGenerator(),
                    typeof(Delegate).GetMethod(nameof(Delegate.Remove)));
            }

            void ImplementMethod(ILGenerator il, MethodInfo method)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Stloc_0);
                var label = il.DefineLabel();
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Stloc_1);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(
                    OpCodes.Call,
                    method,
                    null);
                il.Emit(OpCodes.Castclass, e.EventHandlerType);
                il.Emit(OpCodes.Stloc_2);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldflda, field);
                il.Emit(OpCodes.Ldloc_2);
                il.Emit(OpCodes.Ldloc_1);
                il.EmitCall(
                    OpCodes.Call,
                    typeof(System.Threading.Interlocked).GetMethod(nameof(System.Threading.Interlocked.CompareExchange), new[] { e.EventHandlerType }),
                    null);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Bne_Un_S, label);
                il.Emit(OpCodes.Ret);
            }
        }

        private IEnumerable<EventInfo> AbstractEventsFor(Type type)
        {
            if (type.IsInterface)
            {
                foreach (var e in type.GetEvents())
                {
                    yield return e;
                }
            }

            var abstractThis = type.GetEvents()
                .Where(e => e.AddMethod?.IsAbstract == true || e.RemoveMethod?.IsAbstract == true);
            foreach (var e in abstractThis)
            {
                yield return e;
            }
        }

        private IEnumerable<MethodInfo> AbstractMethodsFor(Type type, bool skipEventMethods = false)
        {
            var methods = new HashSet<MethodInfo>(Impl());
            if (skipEventMethods)
            {
                foreach (var m in EventMethods())
                {
                    methods.Remove(m);
                }
            }

            return methods;

            IEnumerable<MethodInfo> EventMethods()
            {
                if (type.IsInterface)
                {
                    foreach (var m in type.GetEvents().SelectMany(ForEvent))
                    {
                        yield return m;
                    }
                }

                var abstractThis = type.GetEvents()
                    .SelectMany(ForEvent)
                    .Where(m => m?.IsAbstract == true);
                foreach (var m in abstractThis)
                {
                    yield return m;
                }

                IEnumerable<MethodInfo> ForEvent(EventInfo e)
                {
                    yield return e.AddMethod;
                    yield return e.RemoveMethod;
                }
            }

            IEnumerable<MethodInfo> Impl()
            {
                if (type.IsInterface)
                {
                    foreach (var i in type.GetInterfaces())
                    {
                        foreach (var m in this.AbstractMethodsFor(i))
                        {
                            yield return m;
                        }
                    }
                }

                var abstractThis = type.GetMethods()
                    .Concat(type.GetProperties().Select(p => p.SetMethod))
                    .Concat(type.GetProperties().Select(p => p.GetMethod))
                    .Where(m => m?.IsAbstract == true);
                foreach (var m in abstractThis)
                {
                    yield return m;
                }
            }
        }
    }
}
