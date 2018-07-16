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
        public Factory()
        {
            var ab = new DynamicAssembly(debugMode: false);
            this.OfProxies = new Proxies(ab);
            this.OfStubs = new Stubs(ab);
        }

        public Proxies OfProxies { get; }

        public Stubs OfStubs { get; }
    }
}
