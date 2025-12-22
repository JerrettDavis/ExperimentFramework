using BenchmarkDotNet.Running;
using ExperimentFramework.Benchmarks;

Console.WriteLine("ExperimentFramework Performance Benchmarks");
Console.WriteLine("==========================================");
Console.WriteLine();
Console.WriteLine("This benchmark suite measures the overhead of proxying through ExperimentFramework");
Console.WriteLine("compared to direct service invocation.");
Console.WriteLine();
Console.WriteLine("Available benchmarks:");
Console.WriteLine("  1. ProxyOverheadBenchmarks - Raw proxy overhead measurement");
Console.WriteLine("  2. RealWorldScenarioBenchmarks - Realistic I/O and CPU workloads");
Console.WriteLine("  3. All benchmarks");
Console.WriteLine();
Console.Write("Select benchmark to run (1-3, or press Enter for all): ");

var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        BenchmarkRunner.Run<ProxyOverheadBenchmarks>();
        break;
    case "2":
        BenchmarkRunner.Run<RealWorldScenarioBenchmarks>();
        break;
    case "3":
    case "":
    default:
        BenchmarkRunner.Run<ProxyOverheadBenchmarks>();
        BenchmarkRunner.Run<RealWorldScenarioBenchmarks>();
        break;
}

Console.WriteLine();
Console.WriteLine("Benchmarks complete! Results saved to BenchmarkDotNet.Artifacts/results/");
