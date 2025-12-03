using BenchmarkDotNet.Running;

namespace PackageManager.Benchmarks;

/// <summary>
/// Entry point for running benchmarks.
/// </summary>
public static class BenchmarkRunner
{
    public static void RunAll()
    {
        Console.WriteLine("Running PackageManager Benchmarks...");
        Console.WriteLine("=====================================\n");
        
        BenchmarkDotNet.Running.BenchmarkRunner.Run<PackageManagerBenchmarks>();
        BenchmarkDotNet.Running.BenchmarkRunner.Run<AssemblyLoadContextBenchmarks>();
        BenchmarkDotNet.Running.BenchmarkRunner.Run<PackageScannerBenchmarks>();
    }
    
    public static void RunRepositoryBenchmarks()
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<PackageManagerBenchmarks>();
    }
    
    public static void RunAssemblyLoadBenchmarks()
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<AssemblyLoadContextBenchmarks>();
    }
    
    public static void RunScannerBenchmarks()
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<PackageScannerBenchmarks>();
    }
}
