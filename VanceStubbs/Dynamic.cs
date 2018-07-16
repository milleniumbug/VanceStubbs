namespace VanceStubbs
{
    using System;
    using System.Collections.Generic;

    public class Dynamic
    {
        private readonly Factory factory;

        public static Dynamic Factory => VanceStubbs.Factory.Default.Dynamic;

        internal Dynamic(Factory factory)
        {
            this.factory = factory;
        }
    }
}
