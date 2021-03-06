namespace Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;

    public class Program
    {
        private static readonly string ProjectDirectory = Path.Combine(Directory.GetCurrentDirectory(), "../../");

        private static string ArtifactsDirectory { get; } = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results");

        public static void Main()
        {
            var file = Path.Combine(ProjectDirectory, "Benchmarks.csproj");
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(file);
            }

            foreach (var summary in RunSingle<INPCProxy>())
            {
                CopyResult(summary.Title);
            }
        }

        private static IEnumerable<Summary> RunAll()
        {
            var switcher = new BenchmarkSwitcher(typeof(Program).Assembly);
            var summaries = switcher.Run(new[] { "*" });
            return summaries;
        }

        private static IEnumerable<Summary> RunSingle<T>()
        {
            var summaries = new[] { BenchmarkRunner.Run<T>() };
            return summaries;
        }

        private static void CopyResult(string name)
        {
            Console.WriteLine($"DestinationDirectory: {ProjectDirectory}");
            if (Directory.Exists(ProjectDirectory))
            {
                var sourceFileName = Path.Combine(ArtifactsDirectory, "Benchmarks." + name + "-report-github.md");
                var destinationFileName = Path.Combine(ProjectDirectory, "BenchmarkDotNet.Artifacts/results", name + ".md");
                Console.WriteLine($"Copy: {sourceFileName} -> {destinationFileName}");
                File.Copy(sourceFileName, destinationFileName, overwrite: true);
            }
        }
    }
}
