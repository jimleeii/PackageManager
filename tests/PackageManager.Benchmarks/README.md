# PackageManager Benchmarks

Performance benchmarks for the PackageManager library using BenchmarkDotNet.

## Running Benchmarks

### Run All Benchmarks
```bash
dotnet run --project test/Test.csproj -c Release -- --benchmarks
```

### Run from Code
```csharp
using PackageManager.Test.Benchmarks;

// Run all benchmarks
BenchmarkRunner.RunAll();

// Run specific benchmark suites
BenchmarkRunner.RunRepositoryBenchmarks();
BenchmarkRunner.RunAssemblyLoadBenchmarks();
BenchmarkRunner.RunScannerBenchmarks();
```

## Benchmark Suites

### PackageManagerBenchmarks
Tests core repository and query operations with realistic data:
- 100 packages
- 5,000 types (50 per package)
- 20,000 methods (200 per package)

**Operations Benchmarked:**
- `AddOrUpdatePackage` - Adding/updating metadata
- `GetByPackageId` - Direct package lookup
- `GetByPackageIdAndVersion` - Version-specific lookup
- `FindMethodsByName` - Cross-package method search
- `FindMethodsByType` - Type-specific method search
- `FindTypesByName` - Type name search
- `QueryAsyncMethods` - Filter async methods
- `QueryStaticMethods` - Filter static methods
- `QueryPublicInterfaces` - Filter public interfaces
- `ComplexQuery` - Multi-condition LINQ query
- `InvokeStringToString` - Dynamic method invocation
- `RepositoryCount` - Count all packages
- `ClearAndRepopulate` - Bulk operations

### AssemblyLoadContextBenchmarks
Compares isolated vs default assembly loading:
- `LoadAssembly_DefaultContext` (Baseline) - Standard .NET loading
- `LoadAssembly_IsolatedContext` - Collectible context with unload

**What This Measures:**
- Overhead of AssemblyLoadContext isolation
- Memory allocation differences
- Unload capability trade-offs

### PackageScannerBenchmarks
Measures assembly scanning performance:
- `ScanAssembly_SystemLinq` - Metadata extraction from real assemblies

## Expected Results

### Repository Operations
- **Direct lookups** (`GetByPackageId`): < 1 μs
- **Version lookups** (`GetByPackageIdAndVersion`): < 1 μs
- **Name searches** (`FindMethodsByName`): 1-5 ms (20K methods)
- **Complex queries** (`ComplexQuery`): 5-15 ms (multi-filter)
- **Add/Update**: < 10 μs

### Assembly Loading
- **Default Context**: Baseline performance
- **Isolated Context**: 2-5x slower (collectible overhead)
- **Memory**: Isolated uses more but can be reclaimed

### Scanner Operations
- **System.Linq assembly**: 50-200 ms (first scan)
- **Cached reflection**: Subsequent scans faster

## Performance Optimizations

The benchmarks validate these optimizations:
1. ✅ **Lazy evaluation** - `IEnumerable` queries don't materialize unnecessarily
2. ✅ **Thread-safe** - `ConcurrentDictionary` for zero-lock reads
3. ✅ **Static comparers** - `FrameworkPrecedenceSorter` allocated once
4. ✅ **Minimal allocations** - LINQ deferred execution
5. ✅ **Opt-in isolation** - Default context used unless needed

## Interpreting Results

### Good Performance Indicators
- Repository lookups in sub-microsecond range
- Linear scaling with dataset size
- Consistent results across iterations
- Low memory allocations per operation

### Red Flags
- Lookups > 100 μs (possible lock contention)
- Exponential scaling (possible N+1 queries)
- High Gen2 collections (possible memory leaks)
- Wide variance (possible GC interference)

## Customizing Benchmarks

Edit `PackageManagerBenchmarks.cs` to:
- Change dataset size (modify `Setup()` loop counts)
- Add new operations to benchmark
- Test specific query patterns
- Measure custom scenarios

## CI/CD Integration

Add to your pipeline:
```yaml
- name: Run Benchmarks
  run: |
    dotnet run --project test/Test.csproj -c Release -- --benchmarks
    # Upload results as artifacts
```

## Benchmark Configuration

Default configuration:
- **MemoryDiagnoser**: Tracks allocations
- **Orderer**: Results sorted fastest to slowest
- **RankColumn**: Shows relative performance
- **Release Build**: Required for accurate results

## Notes

⚠️ **Always run in Release mode** - Debug builds have overhead  
⚠️ **Close background apps** - For consistent results  
⚠️ **Multiple iterations** - BenchmarkDotNet warms up automatically  
⚠️ **Real assemblies** - Uses System.Linq for realistic scanning tests
