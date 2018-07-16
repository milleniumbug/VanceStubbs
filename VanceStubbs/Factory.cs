using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Sandbox")]

namespace VanceStubbs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    public class Factory
    {
        private static Lazy<Factory> defaultFactory = new Lazy<Factory>(() => new Factory(DynamicAssembly.Default));

        internal static Factory Default => defaultFactory.Value;

        private DynamicAssembly assembly;

        internal DynamicAssembly Assembly => assembly;

        internal Factory(DynamicAssembly ab)
        {
            this.assembly = ab;
            this.OfProxies = new Proxies(this);
            this.OfStubs = new Stubs(this);
            this.Dynamic = new Dynamic(this);
        }

        public Factory()
            : this(new DynamicAssembly())
        {
        }

        public Proxies OfProxies { get; }

        public Stubs OfStubs { get; }

        public Dynamic Dynamic { get; }
    }
}
