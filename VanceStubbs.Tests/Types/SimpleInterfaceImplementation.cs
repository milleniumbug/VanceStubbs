namespace VanceStubbs.Tests.Types
{
    internal class SimpleInterfaceImplementation : ISimpleInterface
    {
        /// <inheritdoc />
        public void DoA()
        {
        }

        /// <inheritdoc />
        public void DoB(int x)
        {
        }

        /// <inheritdoc />
        public int ReturnInt()
        {
            return -1;
        }
    }
}
