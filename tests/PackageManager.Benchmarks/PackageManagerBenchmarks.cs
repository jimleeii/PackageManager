using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PackageManager.Core;
using PackageManager.Models;
using PackageManager.Repository;
using PackageManager.Services;
using System.Runtime.Loader;

namespace PackageManager.Benchmarks;

/// <summary>
/// Performance benchmarks for PackageManager core operations.
/// Run with: dotnet run --project test/Test.csproj -c Release -- --filter *PackageManagerBenchmarks*
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PackageManagerBenchmarks
{
    private IPackageRepository _repository = null!;
    private PackageScanner _scanner = null!;
    private DynamicMethodInvoker _invoker = null!;
    private PackageMetadata _sampleMetadata = null!;
    private List<PackageMetadata> _largeDataset = null!;

    [GlobalSetup]
    public void Setup()
    {
        _repository = new Repository.PackageRepository();
        _scanner = new PackageScanner();
        _invoker = new DynamicMethodInvoker(_repository);

        // Create sample metadata
        _sampleMetadata = new PackageMetadata
        {
            PackageId = "TestPackage",
            Version = "1.0.0",
            PackagePath = @"C:\test\packages\TestPackage.1.0.0",
            Assemblies = ["TestAssembly"],
            Types = 
            [
                new PackageTypeInfo 
                { 
                    FullName = "TestNamespace.TestClass",
                    Name = "TestClass",
                    Namespace = "TestNamespace",
                    AssemblyName = "TestAssembly",
                    IsClass = true
                }
            ],
            Methods = 
            [
                new PackageMethodInfo
                {
                    MethodName = "TestMethod",
                    TypeFullName = "TestNamespace.TestClass",
                    AssemblyName = "TestAssembly",
                    IsStatic = true,
                    IsPublic = true,
                    ReturnType = "System.String",
                    Parameters = new List<MethodParameterInfo>
                    {
                        new MethodParameterInfo 
                        { 
                            Name = "input", 
                            Type = "System.String" 
                        }
                    }
                }
            ]
        };

        // Populate repository with sample data
        _repository.AddOrUpdate(_sampleMetadata);

        // Create large dataset for query benchmarks
        _largeDataset = new List<PackageMetadata>();
        for (int i = 0; i < 100; i++)
        {
            var metadata = new PackageMetadata
            {
                PackageId = $"Package{i}",
                Version = "1.0.0",
                PackagePath = $@"C:\test\packages\Package{i}.1.0.0",
                Assemblies = [$"Assembly{i}"],
                Types = [],
                Methods = []
            };

            // Add 50 types per package
            for (int j = 0; j < 50; j++)
            {
                metadata.Types.Add(new PackageTypeInfo
                {
                    FullName = $"Namespace{i}.Type{j}",
                    Name = $"Type{j}",
                    Namespace = $"Namespace{i}",
                    AssemblyName = $"Assembly{i}",
                    IsClass = j % 2 == 0,
                    IsInterface = j % 2 == 1
                });
            }

            // Add 200 methods per package
            for (int j = 0; j < 200; j++)
            {
                metadata.Methods.Add(new PackageMethodInfo
                {
                    MethodName = $"Method{j}",
                    TypeFullName = $"Namespace{i}.Type{j % 50}",
                    AssemblyName = $"Assembly{i}",
                    IsStatic = j % 3 == 0,
                    IsPublic = true,
                    IsAsync = j % 5 == 0,
                    ReturnType = j % 2 == 0 ? "System.String" : "System.Int32",
                    Parameters = []
                });
            }

            _largeDataset.Add(metadata);
            _repository.AddOrUpdate(metadata);
        }
    }

    [Benchmark]
    public void AddOrUpdatePackage()
    {
        _repository.AddOrUpdate(_sampleMetadata);
    }

    [Benchmark]
    public PackageMetadata? GetByPackageId()
    {
        return _repository.GetByPackageId("Package50");
    }

    [Benchmark]
    public PackageMetadata? GetByPackageIdAndVersion()
    {
        return _repository.GetByPackageIdAndVersion("Package50", "1.0.0");
    }

    [Benchmark]
    public int GetAllPackages()
    {
        return _repository.GetAll().Count();
    }

    [Benchmark]
    public int FindMethodsByName()
    {
        return _repository.FindMethodsByName("Method100").Count();
    }

    [Benchmark]
    public int FindMethodsByType()
    {
        return _repository.FindMethodsByType("Namespace50.Type25").Count();
    }

    [Benchmark]
    public int FindTypesByName()
    {
        return _repository.FindTypesByName("Type25").Count();
    }

    [Benchmark]
    public int QueryAsyncMethods()
    {
        return _repository.GetAll()
            .SelectMany(p => p.Methods)
            .Where(m => m.IsAsync)
            .Count();
    }

    [Benchmark]
    public int QueryStaticMethods()
    {
        return _repository.GetAll()
            .SelectMany(p => p.Methods)
            .Where(m => m.IsStatic)
            .Count();
    }

    [Benchmark]
    public int QueryPublicInterfaces()
    {
        return _repository.GetAll()
            .SelectMany(p => p.Types)
            .Where(t => t.IsInterface)
            .Count();
    }

    [Benchmark]
    public int ComplexQuery()
    {
        return _repository.GetAll()
            .Where(p => p.PackageId.Contains("5"))
            .SelectMany(p => p.Methods)
            .Where(m => m.IsPublic && m.IsStatic && !m.IsAsync)
            .Where(m => m.Parameters.Count == 0)
            .Count();
    }

    [Benchmark]
    public object? InvokeStringToString()
    {
        // Invoke a simple string method
        try
        {
            return _invoker.InvokeMethod("ToString", null, "test");
        }
        catch
        {
            return null;
        }
    }

    [Benchmark]
    public int RepositoryCount()
    {
        return _repository.Count();
    }

    [Benchmark]
    public void ClearAndRepopulate()
    {
        var tempRepo = new Repository.PackageRepository();
        foreach (var metadata in _largeDataset.Take(10))
        {
            tempRepo.AddOrUpdate(metadata);
        }
    }
}

/// <summary>
/// Benchmarks comparing isolated vs non-isolated assembly loading.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class AssemblyLoadContextBenchmarks
{
    private string _testAssemblyPath = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Use a real system assembly for testing
        _testAssemblyPath = typeof(System.Linq.Enumerable).Assembly.Location;
    }

    [Benchmark(Baseline = true)]
    public void LoadAssembly_DefaultContext()
    {
        var assembly = System.Reflection.Assembly.LoadFrom(_testAssemblyPath);
        _ = assembly.GetTypes().Length;
    }

    [Benchmark]
    public void LoadAssembly_IsolatedContext()
    {
        var context = new AssemblyLoadContext("BenchmarkContext", isCollectible: true);
        var assembly = context.LoadFromAssemblyPath(_testAssemblyPath);
        _ = assembly.GetTypes().Length;
        context.Unload();
    }
}

/// <summary>
/// Benchmarks for PackageScanner operations.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PackageScannerBenchmarks
{
    private PackageScanner _scanner = null!;
    private string _sampleAssemblyPath = null!;

    [GlobalSetup]
    public void Setup()
    {
        _scanner = new PackageScanner();
        // Use a real system assembly
        _sampleAssemblyPath = typeof(System.Linq.Enumerable).Assembly.Location;
    }

    [Benchmark]
    public void ScanAssembly_SystemLinq()
    {
        var assembly = System.Reflection.Assembly.LoadFrom(_sampleAssemblyPath);
        _ = _scanner.GetType()
            .GetMethod("ScanAssembly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_scanner, [assembly]);
    }
}
