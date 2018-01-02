namespace VanceStubbs.Tests.Types
{
    public class SimpleInterfaceImplementation : ISimpleInterface
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
