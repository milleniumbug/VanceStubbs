using System;
using System.Reflection.Emit;

namespace VanceStubbs
{
	public static class Stubs
	{
		public static TAbstract BlackHole<TAbstract>()
		{
			return (TAbstract)BlackHole(typeof(TAbstract));
		}

		public static object BlackHole(Type type)
		{

		}

		public static TAbstract WhiteHole<TAbstract>()
		{
			return (TAbstract)WhiteHole(typeof(TAbstract));
		}

		private static object WhiteHole(Type type)
		{

		}
	}
}
