# PackageManager

A dynamic NuGet package management system with runtime discovery and invocation capabilities for .NET applications.

## Overview

PackageManager is a comprehensive library that enables:
- **Dynamic Package Installation**: Install NuGet packages at runtime without recompilation
- **Package Repository**: Automatically catalog all loaded packages with their types and methods
- **Dynamic Method Invocation**: Call methods from loaded packages dynamically at runtime
- **Type Discovery**: Search for types and methods across all loaded packages
- **Reflection-based Access**: Access package functionality without compile-time references

## Solution Structure

```
PackageManager/
├── .editorconfig                 # Code style and formatting rules
├── .gitignore                    # Git ignore rules
├── CHANGELOG.md                  # Version history and changes
├── Directory.Build.props         # Shared MSBuild properties
├── LICENSE                       # MIT License
├── NuGet.Config                  # NuGet package sources
├── PackageManager.sln            # Solution file
├── README.md                     # This file
│
├── src/                          # Main library (PackageManager.csproj)
│   ├── Configuration/            # Service registration and options
│   │   ├── PackageLoaderDisposalService.cs
│   │   ├── PackageManagerExtensions.cs
│   │   └── PackageManagerOptions.cs
│   ├── Core/                     # Package loading and project context
│   │   ├── PackageAssemblyLoadContext.cs
│   │   ├── PackageLoader.cs
│   │   └── ProjectContext.cs
│   ├── FileWatching/            # File system monitoring
│   │   ├── PackageFileWatcher.cs
│   │   └── PackageFileWatcherService.cs
│   ├── Helpers/                  # Utilities and extensions
│   │   ├── LoggerExtensions.cs
│   │   └── PackageFrameworkSorter.cs
│   ├── Models/                   # Data models
│   │   ├── PackageMetadata.cs
│   │   └── PackageMethodInfo.cs
│   ├── Repository/              # Package metadata repository
│   │   ├── IPackageRepository.cs
│   │   └── PackageRepository.cs
│   └── Services/                # Core services (scanner, invoker)
│       ├── DynamicMethodInvoker.cs
│       └── PackageScanner.cs
│
└── tests/                        # Test projects
    ├── PackageManager.UnitTests/         # Unit tests
    │   ├── DynamicMethodInvokerTests.cs
    │   ├── PackageManagerOptionsTests.cs
    │   ├── PackageRepositoryTests.cs
    │   ├── PackageScannerTests.cs
    │   └── README.md
    ├── PackageManager.IntegrationTests/  # Integration tests
    │   ├── PackageRepositoryUsageExample.cs
    │   ├── Program.cs
    │   ├── RepositoryDemo.cs
    │   ├── appsettings.json
    │   └── README.md
    └── PackageManager.Benchmarks/        # Performance benchmarks
        ├── BenchmarkRunner.cs
        ├── PackageManagerBenchmarks.cs
        ├── Program.cs
        └── README.md
```

### Key Components

#### Configuration
- **PackageManagerExtensions.cs**: Service registration and dependency injection
- **PackageManagerOptions.cs**: Configuration options for package management

#### Core Services
- **PackageLoader.cs**: Handles NuGet package installation with repository integration
- **ProjectContext.cs**: Manages project context and dependencies
- **PackageScanner.cs**: Scans assemblies to extract metadata
- **DynamicMethodInvoker.cs**: Enables dynamic method invocation

#### Repository System
- **IPackageRepository.cs**: Interface for package metadata storage
- **PackageRepository.cs**: Thread-safe in-memory repository implementation

#### Models
- **PackageMetadata.cs**: Contains complete package information
- **PackageMethodInfo.cs**: Detailed method metadata for dynamic invocation

#### File Watching
- **PackageFileWatcher.cs**: Monitors package directory changes
- **PackageFileWatcherService.cs**: Background service for file monitoring

## Quick Start (5 Minutes)

### 1. Add PackageManager to Your Application

```csharp
// In Program.cs or Startup.cs
builder.Services.AddPackageManager(builder.Configuration);

// In your application
await app.UsePackageManager();
```

### 2. Configure Package Directory

Add to your `appsettings.json`:

```json
{
  "PackageManager": {
    "PackageSource": "C:\\path\\to\\packages",
    "AllowedFrameworks": ["net9.0", "net8.0", "netstandard2.1"],
    "EnableFileWatching": true,
    "ScanOnStartup": true
  }
}
```

### 3. Inject and Use Services

```csharp
public class MyController : ControllerBase
{
    private readonly IPackageRepository _repository;
    private readonly DynamicMethodInvoker _invoker;

    public MyController(IPackageRepository repository, DynamicMethodInvoker invoker)
    {
        _repository = repository;
        _invoker = invoker;
    }

    public void UsePackage()
    {
        // List all packages
        var packages = _repository.GetAll();

        // Find a method
        var methods = _repository.FindMethodsByName("SerializeObject");

        // Call it dynamically
        var result = _invoker.InvokeMethod("SerializeObject", 
            new object[] { new { Name = "Test" } });
    }
}
```

## Core Features

### 1. Automatic Package Cataloging

Packages are automatically scanned when installed, with metadata stored in the repository immediately—no manual registration required.

```csharp
// Packages are automatically scanned after installation
var package = _repository.GetByPackageId("Newtonsoft.Json");
Console.WriteLine($"Package: {package.PackageId}");
Console.WriteLine($"Version: {package.Version}");
Console.WriteLine($"Methods: {package.Methods.Count}");
```

### 2. Comprehensive Querying

```csharp
// Get specific package
var package = _repository.GetByPackageId("Newtonsoft.Json");

// Find methods by name
var methods = _repository.FindMethodsByName("SerializeObject");

// Find methods in a specific type
var typeMethods = _repository.FindMethodsByType("Newtonsoft.Json.JsonConvert");

// Find types by name
var types = _repository.FindTypesByName("JsonConvert");

// Get all packages
var allPackages = _repository.GetAll();
```

### 3. Dynamic Method Invocation

#### Static Methods
```csharp
// Invoke a static method
var json = _invoker.InvokeMethod(
    "SerializeObject",
    parameters: new object[] { new { Name = "Test", Value = 123 } }
);
```

#### Instance Methods
```csharp
// Create an instance
var list = _invoker.CreateInstance("System.Collections.Generic.List`1[System.String]");

// Call instance methods
_invoker.InvokeMethod("Add", new object[] { "Hello" }, list);
_invoker.InvokeMethod("Add", new object[] { "World" }, list);

// Call property getter
var count = _invoker.InvokeMethod("get_Count", null, list);
```

#### Async Methods
```csharp
// Invoke async methods
var result = await _invoker.InvokeMethodAsync(
    "SomeAsyncMethod",
    parameters: new object[] { "param1" }
);
```

### 4. Type Discovery

```csharp
// Find all async methods
var asyncMethods = _repository.GetAll()
    .SelectMany(p => p.Methods)
    .Where(m => m.IsAsync);

// Find all static methods with no parameters
var parameterlessMethods = _repository.GetAll()
    .SelectMany(p => p.Methods)
    .Where(m => m.IsStatic && m.Parameters.Count == 0);

// Find methods returning Task
var taskMethods = _repository.GetAll()
    .SelectMany(p => p.Methods)
    .Where(m => m.ReturnType.Contains("Task"));

// Find all interfaces
var interfaces = _repository.GetAll()
    .SelectMany(p => p.Types)
    .Where(t => t.IsInterface);
```

### 5. Assembly Isolation (Advanced)

For scenarios requiring assembly unloading (plugins, hot-reload, memory management), use `AssemblyLoadContext` isolation:

```csharp
// Enable assembly isolation when creating PackageLoader
var loader = new PackageLoader(
    repository, 
    scanner, 
    options,
    useIsolation: true  // Enable isolated loading
);

// Check isolation status
if (loader.IsIsolationEnabled)
{
    Console.WriteLine("Assemblies loaded in isolated context");
    Console.WriteLine($"Load Context: {loader.LoadContext?.Name}");
}

// When disposed, the isolated context unloads all assemblies
loader.Dispose();  // Assemblies are unloaded from memory
```

**Benefits of Assembly Isolation:**
- **Memory Management**: Unload assemblies when no longer needed
- **Version Isolation**: Load different versions of the same assembly
- **Plugin Scenarios**: Isolate plugin assemblies from main application
- **Hot Reload**: Unload and reload updated assemblies without restart
- **Reduced Memory Footprint**: Free memory from unused packages

**Trade-offs:**
- Slightly slower assembly loading
- Cannot share types across contexts
- Requires careful lifetime management

**When to use isolation:**
- Long-running applications with dynamic plugins
- Applications that load/unload packages frequently
- Multi-tenant scenarios with isolated workspaces
- Development tools requiring assembly reload

**Default behavior (useIsolation=false):**
- Assemblies loaded in default context
- Better performance for static scenarios
- Simpler programming model
- Cannot unload assemblies

## Common Scenarios

### Scenario 1: Find and Call a Static Method

```csharp
// Find all methods named "Parse"
var parseMethods = _repository.FindMethodsByName("Parse");

// Call DateTime.Parse
var date = _invoker.InvokeMethod("Parse", new object[] { "2024-01-01" });
```

### Scenario 2: Dynamic JSON Serialization

```csharp
// If Newtonsoft.Json is loaded
var data = new 
{ 
    Id = 123, 
    Name = "Product", 
    Price = 99.99 
};

var json = _invoker.InvokeMethod("SerializeObject", new object[] { data });
Console.WriteLine(json); // {"Id":123,"Name":"Product","Price":99.99}
```

### Scenario 3: Query Package Information

```csharp
// Get specific package
var jsonPackage = _repository.GetByPackageId("Newtonsoft.Json");

Console.WriteLine($"Package: {jsonPackage.PackageId}");
Console.WriteLine($"Version: {jsonPackage.Version}");
Console.WriteLine($"Methods: {jsonPackage.Methods.Count}");

// List all types
foreach (var type in jsonPackage.Types.Take(5))
{
    Console.WriteLine($"  {type.FullName}");
}
```

### Scenario 4: Advanced Method Search

```csharp
// Find all async methods
var asyncMethods = _repository.GetAll()
    .SelectMany(p => p.Methods)
    .Where(m => m.IsAsync);

// Find all static methods with no parameters
var parameterlessMethods = _repository.GetAll()
    .SelectMany(p => p.Methods)
    .Where(m => m.IsStatic && m.Parameters.Count == 0);

// Find methods with specific parameter count
var twoParamMethods = _repository.GetAll()
    .SelectMany(p => p.Methods)
    .Where(m => m.Parameters.Count == 2);
```

## API Reference

### IPackageRepository

```csharp
public interface IPackageRepository
{
    void AddOrUpdate(PackageMetadata metadata);
    PackageMetadata? GetByPackageId(string packageId);
    PackageMetadata? GetByPackageIdAndVersion(string packageId, string version);
    IEnumerable<PackageMetadata> GetAll();
    IEnumerable<PackageMethodInfo> FindMethodsByName(string methodName);
    IEnumerable<PackageMethodInfo> FindMethodsByType(string typeFullName);
    IEnumerable<PackageTypeInfo> FindTypesByName(string typeName);
    bool Remove(string packageId);
    void Clear();
    int Count();
}
```

### DynamicMethodInvoker

```csharp
// Invoke by method name (finds best matching overload)
object? InvokeMethod(string methodName, object?[]? parameters = null, object? instance = null);

// Invoke using method metadata
object? InvokeMethod(PackageMethodInfo methodInfo, object?[]? parameters = null, object? instance = null);

// Invoke async method
Task<object?> InvokeMethodAsync(string methodName, object?[]? parameters = null, object? instance = null);

// Create instance of a type
object CreateInstance(string typeFullName, params object?[]? constructorArgs);
```

## Architecture

```
┌─────────────────────────────────────────┐
│         PackageLoader                   │
│  - Installs packages                    │
│  - Triggers scanning                    │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│       PackageScanner                    │
│  - Scans assemblies                     │
│  - Extracts metadata                    │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│     IPackageRepository                  │
│  - Stores metadata                      │
│  - Provides queries                     │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│    DynamicMethodInvoker                 │
│  - Invokes methods                      │
│  - Creates instances                    │
│  - Handles async                        │
└─────────────────────────────────────────┘
```

## Complete Example

```csharp
public class PackageExplorerService
{
    private readonly IPackageRepository _repository;
    private readonly DynamicMethodInvoker _invoker;
    private readonly ILogger<PackageExplorerService> _logger;

    public PackageExplorerService(
        IPackageRepository repository, 
        DynamicMethodInvoker invoker,
        ILogger<PackageExplorerService> logger)
    {
        _repository = repository;
        _invoker = invoker;
        _logger = logger;
    }

    public async Task DemonstrateUsageAsync()
    {
        // 1. List all packages
        var packages = _repository.GetAll();
        _logger.LogInformation("Loaded {Count} packages", packages.Count());

        // 2. Find Newtonsoft.Json package
        var jsonPackage = _repository.GetByPackageId("Newtonsoft.Json");
        if (jsonPackage != null)
        {
            _logger.LogInformation("Found {Count} methods in Newtonsoft.Json", 
                jsonPackage.Methods.Count);
        }

        // 3. Use JsonConvert.SerializeObject dynamically
        var testObject = new { Name = "John", Age = 30 };
        var json = _invoker.InvokeMethod("SerializeObject", new object[] { testObject });
        _logger.LogInformation("Serialized: {Json}", json);

        // 4. Search for all async methods
        var asyncMethods = _repository.GetAll()
            .SelectMany(p => p.Methods)
            .Where(m => m.IsAsync)
            .Take(10);

        _logger.LogInformation("Found {Count} async methods", asyncMethods.Count());

        // 5. Find all methods that return string
        var stringMethods = _repository.GetAll()
            .SelectMany(p => p.Methods)
            .Where(m => m.ReturnType.Contains("String"));

        _logger.LogInformation("Found {Count} methods returning strings", 
            stringMethods.Count());
    }
}
```

## Error Handling

The library provides detailed, helpful error messages with suggestions:

```csharp
try
{
    var result = _invoker.InvokeMethod("MethodName", parameters);
}
catch (InvalidOperationException ex)
{
    // Error messages include suggestions for similar methods
    // Example: "Method 'Serilize' not found. Did you mean: Serialize, SerializeObject?"
    Console.WriteLine($"Error: {ex.Message}");
}
catch (TargetInvocationException ex)
{
    Console.WriteLine($"Method threw an exception: {ex.InnerException?.Message}");
}
```

### Intelligent Error Messages

- **Method not found**: Suggests similar method names based on fuzzy matching
- **Type not found**: Shows available types and partial matches
- **Parameter mismatch**: Displays full method signatures with parameter types
- **Assembly not found**: Lists all loaded packages and their assemblies
- **Directory not found**: Shows resolved paths and actionable tips

## Performance Tips

1. **Cache method info**: Query repository once, store metadata
2. **Reuse instances**: Create instances once, call methods multiple times
3. **Batch queries**: Use LINQ to query once instead of multiple calls
4. **Async for I/O**: Use InvokeMethodAsync for I/O-bound methods
5. **Thread Safety**: Repository is thread-safe for concurrent access
6. **Lazy evaluation**: Repository queries use `IEnumerable` for deferred execution
7. **Assembly isolation**: Only enable when needed (plugins, hot-reload) - default is faster

## Benchmarks

Performance benchmarks are available using BenchmarkDotNet. To run:

```bash
# Run all benchmarks
dotnet run --project test/Test.csproj -c Release -- --benchmarks

# Or use the BenchmarkRunner directly
```

### Benchmark Categories

**Repository Operations:**
- `AddOrUpdatePackage` - Adding/updating package metadata
- `GetByPackageId` - Retrieving by package ID
- `GetByPackageIdAndVersion` - Retrieving by ID and version
- `FindMethodsByName` - Finding methods by name across all packages
- `FindMethodsByType` - Finding methods in specific types
- `QueryAsyncMethods` - Complex LINQ queries over methods
- `ComplexQuery` - Multi-filter LINQ queries

**Assembly Loading:**
- `LoadAssembly_DefaultContext` - Standard assembly loading (baseline)
- `LoadAssembly_IsolatedContext` - Isolated loading with unload capability

**Scanner Operations:**
- `ScanAssembly_SystemLinq` - Metadata extraction performance

**Expected Performance:**
- Repository operations: Sub-microsecond for lookups
- Method queries: ~1-10ms for 20,000 methods across 100 packages
- Assembly isolation overhead: ~2-5x slower than default context
- Memory: Isolated contexts use more memory but can be unloaded

## Thread Safety

The `PackageRepository` uses `ConcurrentDictionary` internally, making it thread-safe for concurrent reads and writes from multiple threads.

## Use Cases

1. **Plugin System**: Load and invoke plugin methods dynamically
2. **API Explorer**: Build tools to explore package APIs
3. **Dynamic Integration**: Integrate with external packages without hard references
4. **Testing Tools**: Invoke methods for testing without compilation
5. **Code Generation**: Generate code based on discovered methods
6. **Documentation**: Auto-generate API docs from loaded packages
7. **Runtime Configuration**: Change behavior by loading different packages
8. **A/B Testing**: Dynamically switch between package versions

## Testing the Demo

To see the repository in action:

1. Open `test/Program.cs`
2. Uncomment this line:
   ```csharp
   await RepositoryDemo.DemoRepositoryUsage(app);
   ```
3. Run the application:
   ```bash
   dotnet run --project test/Test.csproj
   ```
4. View console output showing:
   - Loaded packages
   - Available methods
   - Dynamic invocation results

## Building the Solution

```bash
# Build the entire solution
dotnet build PackageManager.sln

# Build specific projects
dotnet build src/PackageManager.csproj
dotnet build test/Test.csproj

# Run tests
dotnet run --project test/Test.csproj
```

## Configuration

### appsettings.json Example

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "PackageManager": {
    "PackageSource": "plugins",
    "AllowedFrameworks": [
      "net9.0",
      "net8.0",
      "netstandard2.1",
      "netstandard2.0"
    ],
    "EnableFileWatching": true,
    "ScanOnStartup": true
  }
}
```

### Configuration Options

- **`PackageSource`** (string, **required**): The directory path containing NuGet package files (.nupkg). This is where the package manager will look for packages to load. Can be relative or absolute path. **Validated at startup** - application will fail to start if not configured or directory doesn't exist.

- **`AllowedFrameworks`** (array of strings, optional): List of .NET framework versions to load from packages. When specified, only assemblies from these framework folders will be scanned and loaded. If empty or omitted, all compatible frameworks are loaded. **Validated at startup** - framework identifiers must start with 'net', 'netstandard', or 'netcoreapp'.

- **`EnableFileWatching`** (bool, default: `true`): Enables file system monitoring of the PackageSource directory. When enabled, the package manager automatically detects and processes new or changed .nupkg files with debouncing to prevent duplicate processing.

- **`ScanOnStartup`** (bool, default: `true`): Controls whether packages are automatically scanned and loaded during application startup when `UsePackageManager()` is called. Set to `false` to defer package loading or load packages on-demand.

## Benefits

✅ **No Compile-Time Dependencies** - Use packages without referencing them  
✅ **Runtime Discovery** - Discover capabilities at runtime  
✅ **Plugin Architecture** - Build extensible applications  
✅ **Reflection Simplified** - Easy-to-use API over raw reflection  
✅ **Type-Safe Queries** - Strongly-typed metadata models  
✅ **Async Support** - Full async/await support  
✅ **Thread-Safe** - Concurrent access with ConcurrentDictionary  
✅ **Auto-Cataloging** - Automatic metadata extraction via PackageArchiveReader  
✅ **File Watching** - Detect package changes in real-time with debouncing  
✅ **Configuration Validation** - Fail-fast with data annotations  
✅ **Helpful Error Messages** - Intelligent suggestions and fuzzy matching  
✅ **Event-Based Logging** - Subscribe to diagnostic events instead of console output  
✅ **Performance Optimized** - Lazy evaluation, cached comparers, efficient queries  
✅ **Assembly Isolation** - Optional AssemblyLoadContext for unloading and memory management  

## Requirements

- .NET 8.0 or later (tested on .NET 9.0 and .NET 10.0)
- NuGet.Configuration 7.0.1+
- NuGet.Frameworks 7.0.1+
- NuGet.PackageManagement 7.0.1+
- NuGet.Protocol 7.0.1+
- NuGet.Resolver 7.0.1+

## Future Enhancements

- [ ] Add persistence (save repository to disk)
- [ ] Add method signature matching (better overload resolution)
- [ ] Add performance metrics and telemetry
- [ ] Add method caching for frequently-called methods
- [ ] Add security/sandboxing for invoked methods
- [ ] Add LINQ query provider over repository
- [ ] Support for generic method invocation
- [ ] Package version management and rollback
- [ ] Package dependency graph visualization

## License

[Your License Here]

## Contributing

[Your Contributing Guidelines Here]

## Support

For issues, questions, or contributions, please [create an issue](your-repo-url/issues) on GitHub.
