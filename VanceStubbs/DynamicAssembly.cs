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

            foreach (var method in this.AbstractMethodsFor(abstractType))
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

        private IEnumerable<MethodInfo> AbstractMethodsFor(Type type)
        {
            return Impl().Distinct();

            IEnumerable<MethodInfo> Impl()
            {
                foreach (var i in type.GetInterfaces())
                {
                    foreach (var m in this.AbstractMethodsFor(i))
                    {
                        yield return m;
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
