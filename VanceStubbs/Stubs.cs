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
		private static AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.RunAndCollect);

		private static ModuleBuilder mb = ab.DefineDynamicModule(Guid.NewGuid().ToString());

		private static ConcurrentDictionary<Guid, TypeInfo> types = new ConcurrentDictionary<Guid, TypeInfo>();

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
			TypeInfo CreateType()
			{
				TypeBuilder tb;
				if(type.IsInterface)
				{
					tb = mb.DefineType(type.GUID.ToString(), TypeAttributes.Class);
					tb.AddInterfaceImplementation(type);
				}
				else
				{
					tb = mb.DefineType(type.GUID.ToString(), TypeAttributes.Class);
				}

				foreach(var method in AbstractMethodsFor(type))
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

			var concreteType = types.GetOrAdd(type.GUID, g => CreateType());
			return Activator.CreateInstance(concreteType);
		}

		public static T Undefined<T>()
		{
			throw new NotImplementedException("Undefined<T>() was evaluated");
		}
	}
}
