using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace VanceStubbs.Tests
{
	[TestFixture]
	public class Stubs
	{
		[Test]
		public void BlackHole()
		{
			IList<int> inst = VanceStubbs.Stubs.BlackHole<IList<int>>();
			Assert.AreEqual(inst.Count, 0);
			Assert.AreEqual(inst[0], 0);
		}

		[Test]
		public void WhiteHole()
		{
			IList<int> inst = VanceStubbs.Stubs.WhiteHole<IList<int>>();
			Assert.Throws<Exception>(() =>
			{
				var c = inst.Count;
			});
			Assert.Throws<Exception>(() =>
			{
				inst[0] = 42;
			});
			Assert.Throws<Exception>(() =>
			{
				var i = inst[0];
			});
		}
	}
}
