using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace VanceStubbs
{
	public static class Stubs
	{
		private static readonly ConcurrentDictionary<Type, TypeInfo> whiteholes = new ConcurrentDictionary<Type, TypeInfo>();

		public static TAbstract WhiteHole<TAbstract>()
		{
			return (TAbstract)WhiteHole(typeof(TAbstract));
		}

		public static object WhiteHole(Type type)
		{
			var concreteType = whiteholes.GetOrAdd(type, t => DynamicAssembly.ImplementAbstractMethods("WhiteHole.", t, ImplementAsThrowing));
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

		private static readonly ConcurrentDictionary<Type, TypeInfo> blackholes = new ConcurrentDictionary<Type, TypeInfo>();

		public static TAbstract BlackHole<TAbstract>()
		{
			return (TAbstract)BlackHole(typeof(TAbstract));
		}

		public static object BlackHole(Type type)
		{
			var concreteType = blackholes.GetOrAdd(type, t => DynamicAssembly.ImplementAbstractMethods("BlackHole.", t, ImplementAsReturnDefault));
			return Activator.CreateInstance(concreteType);

			void ImplementAsReturnDefault(MethodInfo originalMethod, ILGenerator il)
			{
				if(originalMethod.ReturnType == typeof(void))
				{
					il.Emit(OpCodes.Ret);
					return;
				}
				if(!originalMethod.ReturnType.IsValueType)
				{
					il.Emit(OpCodes.Ldnull);
					il.Emit(OpCodes.Ret);
					return;
				}
				if(!originalMethod.ReturnType.IsPrimitive)
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
				if(primitive == typeof(float))
				{
					il.Emit(OpCodes.Ldc_R4);
					il.Emit(OpCodes.Ret);
				}
				else if(primitive == typeof(double))
				{
					il.Emit(OpCodes.Ldc_R8);
					il.Emit(OpCodes.Ret);
				}
				else if(primitive == typeof(decimal))
				{
					il.Emit(OpCodes.Ldsfld, typeof(decimal).GetField(nameof(decimal.Zero)));
					il.Emit(OpCodes.Ret);
				}
				else if(primitive == typeof(long))
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
	}
}
