using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace VanceStubbs
{
	public static class Stubs
	{
		private static readonly AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("proxies"), AssemblyBuilderAccess.RunAndCollect);

		private static readonly ModuleBuilder mb = ab.DefineDynamicModule("proxiesmodule");

		private static readonly ConcurrentDictionary<Type, TypeInfo> types = new ConcurrentDictionary<Type, TypeInfo>();

		public static TAbstract BlackHole<TAbstract>()
		{
			return (TAbstract)BlackHole(typeof(TAbstract));
		}

		public static object BlackHole(Type type)
		{
			throw new NotImplementedException();
		}

		public static TAbstract WhiteHole<TAbstract>()
		{
			return (TAbstract)WhiteHole(typeof(TAbstract));
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

		public static object WhiteHole(Type type)
		{
			TypeInfo CreateType(Type abstractType)
			{
				TypeBuilder tb;
				if(abstractType.IsInterface)
				{
					tb = mb.DefineType(abstractType.FullName, TypeAttributes.Class);
					tb.AddInterfaceImplementation(abstractType);
				}
				else
				{
					tb = mb.DefineType(abstractType.FullName, TypeAttributes.Class, abstractType);
				}

				foreach(var method in AbstractMethodsFor(abstractType))
				{
					var methodOverride = tb.DefineMethod(
						method.Name,
						method.Attributes & ~MethodAttributes.Abstract,
						method.CallingConvention,
						method.ReturnType,
						method.GetParameters().Select(p => p.ParameterType).ToArray());
					var il = methodOverride.GetILGenerator();
					il.ThrowException(typeof(NotImplementedException));
				}
				return tb.CreateTypeInfo();
			}

			var concreteType = types.GetOrAdd(type, CreateType);
			return Activator.CreateInstance(concreteType);
		}

		public static T Undefined<T>()
		{
			throw new NotImplementedException("Undefined<T>() was evaluated");
		}
	}
}
