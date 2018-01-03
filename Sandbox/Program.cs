namespace Sandbox
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using NUnit.Framework;
    using VanceStubbs;
    using VanceStubbs.Tests.Types;

    public class Program
    {
        public static void Main()
        {
            var peverifyPath = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\PEVerify.exe";
            if (!File.Exists(peverifyPath))
            {
                Console.WriteLine("PEVerify.exe not found at the given path, verification skipped");
                peverifyPath = null;
            }

            try
            {
                Func<ISimpleInterface, List<string>, ISimpleInterface> f = VanceStubbs.ProxyFactory
                    .For<ISimpleInterface>()
                    .WithState<List<string>>()
                    .WithPostEntryHandler((target, state, parameters) => state.Add("Out1"))
                    .WithPostEntryHandler((target, state, parameters) => state.Add("Out2"))
                    .WithPreExitHandler((target, state, ret) =>
                    {
                        state.Add("In1");
                        return ret;
                    })
                    .WithPreExitHandler((target, state, ret) =>
                    {
                        state.Add("In2");
                        return ret;
                    })
                    .Create();
                var v = new SimpleInterfaceImplementation();
                var s = new List<string>();
                var proxy = f(v, s);
                v.ReturnInt();
                CollectionAssert.AreEqual(new[] { "Out2", "Out1", "In1", "In2" }, s);
            }
            finally
            {
                DynamicAssembly.Default.Save();
                if (peverifyPath != null)
                {
                    var process = Process.Start(new ProcessStartInfo(peverifyPath)
                    {
                        Arguments = "stubsautogenerated.dll",
                        CreateNoWindow = false,
                        UseShellExecute = false
                    });
                }
            }
        }
    }
}
