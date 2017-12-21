using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Sandbox")]

namespace VanceStubbs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    internal class DynamicAssembly
    {
        public static readonly DynamicAssembly Default = new DynamicAssembly(debugMode: false);

        private readonly bool debugMode;

        public DynamicAssembly(bool debugMode)
        {
            var assemblyName = new AssemblyName("VanceStubbs" + Guid.NewGuid());
            var moduleName = "proxiesmodule";
            var path = "stubsautogenerated.dll";
            if (debugMode && Enum.TryParse("RunAndSave", out AssemblyBuilderAccess runAndSave))
            {
                this.Assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, runAndSave);
                var twoArgDefineDynamicModule = typeof(AssemblyBuilder)
                    .GetMethod(nameof(this.Assembly.DefineDynamicModule), new[] { typeof(string), typeof(string) });
                this.Module = (ModuleBuilder)twoArgDefineDynamicModule
                    .Invoke(this.Assembly, new object[] { moduleName, path });
                this.debugMode = true;
                return;
            }

            this.Assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            this.Module = this.Assembly.DefineDynamicModule(moduleName);
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

        public FieldInfo ImplementEventByDelegatingToANewField(TypeBuilder tb, EventInfo e)
        {
            var fb = tb.DefineField(e.Name, e.EventHandlerType, FieldAttributes.Family);
            this.ImplementEventWithField(tb, e, fb);
            return fb;
        }

        public void ImplementEventWithField(TypeBuilder tb, EventInfo e, FieldInfo field)
        {
            {
                var addMethod = tb.DefineMethod(
                    "add_" + e.Name,
                    MethodAttributes.Virtual | MethodAttributes.SpecialName | GetAccessibility(e.AddMethod),
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
                    MethodAttributes.Virtual | MethodAttributes.SpecialName | GetAccessibility(e.RemoveMethod),
                    CallingConventions.HasThis,
                    typeof(void),
                    new[] { e.EventHandlerType });
                ImplementMethod(
                    removeMethod.GetILGenerator(),
                    typeof(Delegate).GetMethod(nameof(Delegate.Remove)));
            }

            MethodAttributes GetAccessibility(MemberInfo m)
            {
                return MethodAttributes.Public;
            }

            void ImplementMethod(ILGenerator il, MethodInfo method)
            {
                il.DeclareLocal(e.EventHandlerType);
                il.DeclareLocal(e.EventHandlerType);
                il.DeclareLocal(e.EventHandlerType);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Stloc_0);
                var label = il.DefineLabel();
                il.MarkLabel(label);
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
                var interlocked = typeof(System.Threading.Interlocked);
                var casMethodParameterTypes = new[]
                {
                    e.EventHandlerType.MakeByRefType(),
                    e.EventHandlerType,
                    e.EventHandlerType
                };
                var casMethod =
                   interlocked.GetMethod(
                       nameof(System.Threading.Interlocked.CompareExchange),
                       casMethodParameterTypes);
                il.EmitCall(
                    OpCodes.Call,
                    casMethod,
                    null);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Bne_Un_S, label);
                il.Emit(OpCodes.Ret);
            }
        }

        public void DelegateAllConstructorsToBase(TypeBuilder tb)
        {
            var baseClass = tb.BaseType;
            var constructors = baseClass
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(c => c.IsFamily || c.IsFamilyOrAssembly || c.IsPublic);
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                var constructorOverride = tb.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.HasThis,
                    parameters.Select(p => p.ParameterType).ToArray());
                var il = constructorOverride.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                if (parameters.Length >= 1)
                {
                    il.Emit(OpCodes.Ldarg_1);
                }

                if (parameters.Length >= 2)
                {
                    il.Emit(OpCodes.Ldarg_2);
                }

                if (parameters.Length >= 3)
                {
                    il.Emit(OpCodes.Ldarg_3);
                }

                for (int i = 4; i <= Math.Min(parameters.Length, byte.MaxValue); ++i)
                {
                    il.Emit(OpCodes.Ldarg_S, (byte)i);
                }

                for (int i = 256; i <= Math.Min(parameters.Length, ushort.MaxValue); ++i)
                {
                    il.Emit(OpCodes.Ldarg, (short)i);
                }

                il.Emit(OpCodes.Call, constructor);
                il.Emit(OpCodes.Ret);
            }
        }

        public IEnumerable<PropertyInfo> AbstractPropertiesFor(Type type)
        {
            if (type.IsInterface)
            {
                foreach (var p in type.GetProperties())
                {
                    yield return p;
                }
            }

            var abstractThis = type.GetProperties()
                .Where(e => e.SetMethod?.IsAbstract == true || e.GetMethod?.IsAbstract == true);
            foreach (var p in abstractThis)
            {
                yield return p;
            }
        }

        public object ActivateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }

        public object ActivateInstance(Type type, object parameter)
        {
            return Activator.CreateInstance(type, parameter);
        }

        // DEBUG METHOD ONLY
        // Limited support across .NET runtimes
        // If the runtime doesn't support generating, the request will be ignored
        internal void Save()
        {
            if (!this.debugMode)
            {
                return;
            }

            var save = this.Assembly.GetType().GetMethod("Save", new[] { typeof(string) });
            save?.Invoke(this.Assembly, new object[] { "aaaa.dll" });
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
