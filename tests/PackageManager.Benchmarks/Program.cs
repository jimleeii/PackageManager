using PackageManager.Benchmarks;

BenchmarkRunner.RunAll();
Console.WriteLine("Benchmarks completed. Press any key to exit.");

if (Environment.UserInteractive && !Console.IsInputRedirected)
{
    Console.ReadKey();
}
