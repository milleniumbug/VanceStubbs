namespace VanceStubbs
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class ProxyFactory
    {
        public static ProxyBuilder<TWrappedType> For<TWrappedType>()
        {
            var type = typeof(TWrappedType);
            if (!type.IsInterface)
            {
                throw new ArgumentException("the type parameter must be an interface", nameof(TWrappedType));
            }

            return new ProxyBuilder<TWrappedType>();
        }

        public class ProxyBuilder<TWrappedType>
        {
            internal ProxyBuilder()
            {
            }

            public InstantiatedStatefulBuilder<TWrappedType, TState> WithState<TState>()
            {
                return new InstantiatedStatefulBuilder<TWrappedType, TState>();
            }

            public InstantiatedStatelessBuilder<TWrappedType> Stateless()
            {
                return new InstantiatedStatelessBuilder<TWrappedType>();
            }
        }

        public class InstantiatedStatelessBuilder<TWrappedType>
        {
            private readonly InstantiatedStatefulBuilder<TWrappedType, object> builder;

            internal InstantiatedStatelessBuilder()
            {
                this.builder = new InstantiatedStatefulBuilder<TWrappedType, object>();
            }

            public InstantiatedStatelessBuilder<TWrappedType> WithPreExitHandler(Func<TWrappedType, object, object> preExit)
            {
                this.builder.WithPreExitHandler((@this, state, result) => preExit(@this, result));
                return this;
            }

            public InstantiatedStatelessBuilder<TWrappedType> WithPostEntryHandler(Action<TWrappedType, object[]> postEntry)
            {
                this.builder.WithPostEntryHandler((@this, state, parameters) => postEntry(@this, parameters));
                return this;
            }

            public Func<TWrappedType, TWrappedType> Create()
            {
                var factory = this.builder.Create();
                return @this => factory(@this, null);
            }
        }

        public class InstantiatedStatefulBuilder<TWrappedType, TState>
        {
            private Func<TWrappedType, TState, object, object> preExit;

            private Action<TWrappedType, TState, object[]> postEntry;

            internal InstantiatedStatefulBuilder()
            {
            }

            public InstantiatedStatefulBuilder<TWrappedType, TState> WithPreExitHandler(Func<TWrappedType, TState, object, object> preExit)
            {
                if (preExit == null)
                {
                    throw new ArgumentNullException(nameof(preExit));
                }

                if (this.preExit == null)
                {
                    this.preExit = preExit;
                }
                else
                {
                    var previousHandler = this.preExit;
                    this.preExit = (@this, state, result) =>
                    {
                        result = previousHandler(@this, state, result);
                        return preExit(@this, state, result);
                    };
                }

                return this;
            }

            public InstantiatedStatefulBuilder<TWrappedType, TState> WithPostEntryHandler(Action<TWrappedType, TState, object[]> postEntry)
            {
                if (postEntry == null)
                {
                    throw new ArgumentNullException(nameof(postEntry));
                }

                if (this.postEntry == null)
                {
                    this.postEntry = postEntry;
                }
                else
                {
                    var previousHandler = this.postEntry;
                    this.postEntry = (@this, state, parameters) =>
                    {
                        postEntry(@this, state, parameters);
                        previousHandler(@this, state, parameters);
                    };
                }

                return this;
            }

            public Func<TWrappedType, TState, TWrappedType> Create()
            {
                throw new NotImplementedException();
            }
        }
    }
}
