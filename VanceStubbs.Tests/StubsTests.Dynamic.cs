namespace VanceStubbs.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using NUnit.Framework;

    public partial class StubsTests
    {
        [TestFixture]
        public class Dynamic
        {
            private IEnumerable<Type> types = Array.AsReadOnly(new Type[]
            {
                // void
                typeof(void),

                // CLS-compliant primitives
                typeof(bool),
                typeof(byte),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(char),
                typeof(float),
                typeof(double),

                // non-CLS compliant primitives
                typeof(sbyte),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),

                // non-primitives, but built-in to C#
                typeof(decimal),
                typeof(string),

                // runtime special types
                typeof(object),
                typeof(System.Array),
                typeof(System.ValueType),
                typeof(System.Enum),
                typeof(System.Delegate),
                typeof(System.MulticastDelegate),
                typeof(System.Type),
                typeof(System.TypedReference),
                typeof(System.IntPtr),
                typeof(System.UIntPtr),
                typeof(System.Exception),
                typeof(System.MarshalByRefObject),
                typeof(System.ContextBoundObject),
                typeof(System.ArgIterator),
                typeof(System.Attribute),

                // custom struct type
                typeof(System.DateTime),

                // an array
                typeof(int[]),
                typeof(DateTime[]),
                typeof(string[]),

                // an enum
                typeof(System.IO.FileAccess),

                // an interface
                typeof(System.IDisposable),

                // an attribute
                typeof(System.ObsoleteAttribute),

                // an abstract class
                typeof(System.IO.Stream),

                // an abstract sealed (static) class
                typeof(System.Linq.Enumerable),

                // open generic class
                typeof(System.Collections.Generic.List<>),

                // closed generic class
                typeof(System.Collections.Generic.List<int>),

                // open generic interface
                typeof(System.Collections.Generic.IList<>),

                // closed generic interface
                typeof(System.Collections.Generic.IList<int>),

                // open generic delegate
                typeof(System.Func<,,>),

                // closed generic delegate
                typeof(System.Func<IList<int>, int, IList<int>>),

                // nullable and friends
                typeof(Nullable<>),
                typeof(int?),
                typeof(double?),

                // generic type parameters
                typeof(IList<>).GetGenericArguments()[0],
                typeof(Nullable<>).GetGenericArguments()[0],

                // pointers
                typeof(char*),
                typeof(int*),
                typeof(void*),
            });

            [Test]
            public void TypeLookupBasic()
            {
                var d = new TypeDictionary<string>(new Dictionary<Type, string>
                {
                    { typeof(IEnumerable<int>), "enumerable" },
                    { typeof(int), "int" },
                    { typeof(IList<int>), "list" }
                });
                Assert.AreEqual("list", d[typeof(List<int>)]);
                Assert.AreEqual("int", d[typeof(int)]);
                Assert.AreEqual("enumerable", d[typeof(IReadOnlyList<int>)]);
            }

            [Test]
            public void TypeLookupString()
            {
                var d = new TypeDictionary<string>(new Dictionary<Type, string>
                {
                    { typeof(IEnumerable<int>), "enumerable" },
                    { typeof(string), "string" },
                });
                Assert.AreEqual("enumerable", d[typeof(List<int>)]);
                Assert.IsFalse(d.ContainsKey(typeof(void)));
                Assert.AreEqual("string", d[typeof(string)]);
            }

            [Test]
            public void TypeLookupObject()
            {
                var d = new TypeDictionary<string>(new Dictionary<Type, string>
                {
                    { typeof(object), "object" }
                });
                Assert.AreEqual("object", d[typeof(List<int>)]);
                Assert.AreEqual("object", d[typeof(string)]);
            }

            [Test]
            public void TypeLookupGeneric()
            {
                var d = new TypeDictionary<string>(new Dictionary<Type, string>
                {
                    { typeof(int), "int" },
                    { typeof(Nullable<>), "nullable" },
                    { typeof(List<int>), "list of int" },
                    { typeof(List<>), "list" }
                });
                Assert.AreEqual("list of int", d[typeof(List<int>)]);
                Assert.AreEqual("list", d[typeof(List<string>)]);
                Assert.AreEqual("nullable", d[typeof(double?)]);
                Assert.AreEqual("int", d[typeof(int)]);
            }

            [Test]
            public void TypeLookupKillerTest()
            {
                var d = new TypeDictionary<Type>(this.types
                    .Where(t => !t.IsGenericParameter)
                    .Select((t, i) => new KeyValuePair<Type, Type>(t, t)));
                Assert.AreEqual(typeof(List<int>), d[typeof(List<int>)]);
                Assert.AreEqual(typeof(Attribute), d[typeof(DescriptionAttribute)]);
                Assert.AreEqual(typeof(void), d[typeof(void)]);
            }
        }
    }
}
