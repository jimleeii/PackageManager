# Package Repository System

A comprehensive system for cataloging and dynamically invoking methods from loaded NuGet packages.

## Overview

This package repository system provides the following capabilities:

1. **Package Metadata Storage**: Automatically catalogs all loaded packages with their types and methods
2. **Dynamic Method Invocation**: Call methods from loaded packages dynamically at runtime
3. **Type Discovery**: Search for types and methods across all loaded packages
4. **Reflection-based Access**: Access package functionality without compile-time references

## Components

### Models

- **PackageMetadata**: Contains information about a loaded package including assemblies, types, and methods
- **PackageTypeInfo**: Represents a type within a package
- **PackageMethodInfo**: Contains detailed method information including parameters and return types
- **MethodParameterInfo**: Represents method parameter details

### Services

- **IPackageRepository / PackageRepository**: Thread-safe repository for storing and querying package metadata
- **PackageScanner**: Scans assemblies to extract type and method information
- **DynamicMethodInvoker**: Enables dynamic invocation of methods from loaded packages

## Usage

### 1. Service Registration

The repository system is automatically registered when you use `AddPackageManager`:

```csharp
builder.Services.AddPackageManager(builder.Configuration);
```

This registers:
- `IPackageRepository` (singleton)
- `PackageScanner` (singleton)
- `DynamicMethodInvoker` (singleton)
- `PackageLoader` (singleton, now with repository integration)

### 2. Inject Services

```csharp
public class MyService
{
    private readonly IPackageRepository _packageRepository;
    private readonly DynamicMethodInvoker _methodInvoker;

    public MyService(IPackageRepository packageRepository, DynamicMethodInvoker methodInvoker)
    {
        _packageRepository = packageRepository;
        _methodInvoker = methodInvoker;
    }
}
```

### 3. Query Loaded Packages

```csharp
// Get all loaded packages
var packages = _packageRepository.GetAll();

// Get a specific package
var package = _packageRepository.GetByPackageId("Newtonsoft.Json");

// Get package with specific version
var package = _packageRepository.GetByPackageIdAndVersion("Newtonsoft.Json", "13.0.4");

// Get repository statistics
var count = _packageRepository.Count();
```

### 4. Search for Methods and Types

```csharp
// Find all methods with a specific name across all packages
var methods = _packageRepository.FindMethodsByName("SerializeObject");

// Find all methods in a specific type
var methods = _packageRepository.FindMethodsByType("Newtonsoft.Json.JsonConvert");

// Find types by name
var types = _packageRepository.FindTypesByName("JsonConvert");
```

### 5. Dynamic Method Invocation

#### Static Methods

```csharp
// Invoke a static method by name
var json = _methodInvoker.InvokeMethod(
    "SerializeObject",
    parameters: new object[] { new { Name = "Test", Value = 123 } }
);
```

#### Instance Methods

```csharp
// Create an instance first
var instance = _methodInvoker.CreateInstance(
    "SomeNamespace.SomeClass",
    constructorArgs: new object[] { "arg1", 42 }
);

// Invoke an instance method
var result = _methodInvoker.InvokeMethod(
    "SomeMethod",
    parameters: new object[] { "param1" },
    instance: instance
);
```

#### Async Methods

```csharp
// Invoke async methods
var result = await _methodInvoker.InvokeMethodAsync(
    "SomeAsyncMethod",
    parameters: new object[] { "param1" }
);
```

### 6. Advanced Querying

```csharp
// Find all async methods
var asyncMethods = _packageRepository.GetAll()
    .SelectMany(p => p.Methods)
    .Where(m => m.IsAsync);

// Find all static methods
var staticMethods = _packageRepository.GetAll()
    .SelectMany(p => p.Methods)
    .Where(m => m.IsStatic);

// Find methods with specific parameter count
var twoParamMethods = _packageRepository.GetAll()
    .SelectMany(p => p.Methods)
    .Where(m => m.Parameters.Count == 2);

// Find all interfaces
var interfaces = _packageRepository.GetAll()
    .SelectMany(p => p.Types)
    .Where(t => t.IsInterface);
```

## Repository Interface

### IPackageRepository Methods

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

## DynamicMethodInvoker Methods

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

## Complete Example

```csharp
public class PackageExplorerService
{
    private readonly IPackageRepository _repository;
    private readonly DynamicMethodInvoker _invoker;

    public PackageExplorerService(IPackageRepository repository, DynamicMethodInvoker invoker)
    {
        _repository = repository;
        _invoker = invoker;
    }

    public async Task DemonstrateUsageAsync()
    {
        // 1. List all packages
        var packages = _repository.GetAll();
        Console.WriteLine($"Loaded {packages.Count()} packages");

        // 2. Find Newtonsoft.Json package
        var jsonPackage = _repository.GetByPackageId("Newtonsoft.Json");
        if (jsonPackage != null)
        {
            Console.WriteLine($"Found {jsonPackage.Methods.Count} methods in Newtonsoft.Json");
        }

        // 3. Use JsonConvert.SerializeObject dynamically
        var testObject = new { Name = "John", Age = 30 };
        var json = _invoker.InvokeMethod("SerializeObject", new object[] { testObject });
        Console.WriteLine($"Serialized: {json}");

        // 4. Search for all async methods
        var asyncMethods = _repository.GetAll()
            .SelectMany(p => p.Methods)
            .Where(m => m.IsAsync)
            .Take(10);

        Console.WriteLine($"Found {asyncMethods.Count()} async methods");

        // 5. Find all methods that return string
        var stringMethods = _repository.GetAll()
            .SelectMany(p => p.Methods)
            .Where(m => m.ReturnType.Contains("String"));

        Console.WriteLine($"Found {stringMethods.Count()} methods returning strings");
    }
}
```

## Benefits

1. **Runtime Discovery**: Discover and use package functionality at runtime without compile-time dependencies
2. **Dynamic Plugin System**: Build extensible applications that can load and use packages dynamically
3. **API Documentation**: Generate API documentation from loaded packages
4. **Method Explorer**: Build tools to explore and test methods from loaded packages
5. **Reflection Helper**: Simplifies complex reflection scenarios

## Thread Safety

The `PackageRepository` uses `ConcurrentDictionary` internally, making it thread-safe for concurrent reads and writes.

## Performance Considerations

- Package scanning happens asynchronously when packages are installed
- Method metadata is cached in memory for fast lookup
- Assembly loading is optimized with caching in `DynamicMethodInvoker`

## Error Handling

All methods throw descriptive exceptions when:
- Methods/types are not found
- Parameters don't match method signatures
- Assembly loading fails
- Method invocation fails

Always wrap dynamic invocations in try-catch blocks for production use.
