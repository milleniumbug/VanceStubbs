using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace VanceStubbs
{
	public static class Stubs
	{
		private static readonly ConcurrentDictionary<Type, TypeInfo> types = new ConcurrentDictionary<Type, TypeInfo>();

		public static TAbstract WhiteHole<TAbstract>()
		{
			return (TAbstract)WhiteHole(typeof(TAbstract));
		}

		public static object WhiteHole(Type type)
		{
			var concreteType = types.GetOrAdd(type, t => DynamicAssembly.ImplementAbstractMethods(t, ImplementAsThrowing));
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
			throw new NotImplementedException();
		}
	}
}
