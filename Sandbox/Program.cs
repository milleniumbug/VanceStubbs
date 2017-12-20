namespace Sandbox
{
    using System;
    using VanceStubbs.Tests.Types;

    public class Program
    {
        public static void Main(string[] args)
        {
            var proxy = VanceStubbs.Stubs.NotifyPropertyChangedProxy<INotifyManyProperties>();
        }
    }

    internal class E : IEvent
    {
        /// <inheritdoc />
        public event Action Lol;
    }
}
