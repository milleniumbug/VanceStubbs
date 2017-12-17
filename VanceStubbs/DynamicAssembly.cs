using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace VanceStubbs
{
	internal static class DynamicAssembly
	{
		public static readonly AssemblyBuilder Assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("proxies"), AssemblyBuilderAccess.RunAndCollect);

		public static readonly ModuleBuilder Module = Assembly.DefineDynamicModule("proxiesmodule");

		public static TypeInfo ImplementAbstractMethods(Type abstractType, Action<MethodInfo, ILGenerator> implementer)
		{
			TypeBuilder tb;
			if(abstractType.IsInterface)
			{
				tb = Module.DefineType(abstractType.FullName, TypeAttributes.Class);
				tb.AddInterfaceImplementation(abstractType);
			}
			else
			{
				tb = Module.DefineType(abstractType.FullName, TypeAttributes.Class, abstractType);
			}

			foreach(var method in AbstractMethodsFor(abstractType))
			{
				var methodOverride = tb.DefineMethod(method.Name, method.Attributes & ~MethodAttributes.Abstract, method.CallingConvention, method.ReturnType, Enumerable.Select<ParameterInfo, Type>(method.GetParameters(), p => p.ParameterType).ToArray());
				implementer(method, methodOverride.GetILGenerator());
			}
			return tb.CreateTypeInfo();
		}

		private static IEnumerable<MethodInfo> AbstractMethodsFor(Type type)
		{
			foreach(var i in type.GetInterfaces())
			{
				foreach(var m in AbstractMethodsFor(i))
				{
					yield return m;
				}
			}
			var abstractThis = type.GetMethods()
				.Concat(type.GetProperties().Select(p => p.SetMethod))
				.Concat(type.GetProperties().Select(p => p.GetMethod))
				.Where(m => m?.IsAbstract == true);
			foreach(var m in abstractThis)
			{
				yield return m;
			}
		}
	}
}