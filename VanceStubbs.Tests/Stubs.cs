using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
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
			Assert.Throws<NotImplementedException>(() =>
			{
				var c = inst.Count;
			});
			Assert.Throws<NotImplementedException>(() =>
			{
				inst[0] = 42;
			});
			Assert.Throws<NotImplementedException>(() =>
			{
				var i = inst[0];
			});
		}

		[Test]
		public void WhiteHoleAbstractClass()
		{
			Stream inst = VanceStubbs.Stubs.WhiteHole<Stream>();
			Assert.Throws<NotImplementedException>(() =>
			{
				var c = inst.CanRead;
			});
		}

		[Test]
		public void MultipleInstantiatedGenerics()
		{
			IList<string> inst = VanceStubbs.Stubs.WhiteHole<IList<string>>();
			Assert.Throws<NotImplementedException>(() =>
			{
				inst.RemoveAt(0);
			});
		}

		[Test]
		public void Undefined()
		{
			Assert.Throws<NotImplementedException>(() =>
			{
				Console.WriteLine(new DateTime(2, 3, 5, VanceStubbs.Stubs.Undefined<Calendar>()));
			});
		}
	}
}
