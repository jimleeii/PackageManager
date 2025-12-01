# Quick Start Guide - Package Repository

## 5-Minute Setup

### 1. Your application already has it!

The repository system is automatically included when you use `AddPackageManager`:

```csharp
builder.Services.AddPackageManager(builder.Configuration);
```

### 2. Inject the services you need

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
}
```

### 3. Use it!

```csharp
// List all packages
var packages = _repository.GetAll();

// Find a method
var methods = _repository.FindMethodsByName("SerializeObject");

// Call it dynamically
var result = _invoker.InvokeMethod("SerializeObject", 
    new object[] { new { Name = "Test" } });
```

## Common Scenarios

### Scenario 1: Find and Call a Static Method

```csharp
// Find all methods named "Parse"
var parseMethods = _repository.FindMethodsByName("Parse");

// Call DateTime.Parse
var date = _invoker.InvokeMethod("Parse", new object[] { "2024-01-01" });
```

### Scenario 2: Create Instance and Call Method

```csharp
// Create instance
var list = _invoker.CreateInstance("System.Collections.Generic.List`1[System.String]");

// Call instance method
_invoker.InvokeMethod("Add", new object[] { "Hello" }, list);
_invoker.InvokeMethod("Add", new object[] { "World" }, list);

// Call Count property getter
var count = _invoker.InvokeMethod("get_Count", null, list);
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

### Scenario 4: Search for Methods by Criteria

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
```

### Scenario 5: Call JSON Serialization Dynamically

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

## Testing the Demo

To see the repository in action:

1. Open `test/Program.cs`
2. Uncomment this line:
   ```csharp
   await RepositoryDemo.DemoRepositoryUsage(app);
   ```
3. Run the application
4. See console output showing all loaded packages and their methods

## API Cheat Sheet

### Repository Queries
```csharp
repository.GetAll()                              // All packages
repository.GetByPackageId("packageId")          // Single package
repository.FindMethodsByName("methodName")      // Methods by name
repository.FindMethodsByType("TypeName")        // Methods in type
repository.FindTypesByName("typeName")          // Types by name
repository.Count()                               // Package count
```

### Dynamic Invocation
```csharp
invoker.InvokeMethod(methodName, parameters)                    // Static method
invoker.InvokeMethod(methodName, parameters, instance)          // Instance method
await invoker.InvokeMethodAsync(methodName, parameters)         // Async method
invoker.CreateInstance(typeName, constructorArgs)               // Create object
```

## Error Handling

Always wrap invocations in try-catch:

```csharp
try
{
    var result = _invoker.InvokeMethod("MethodName", parameters);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Method not found or invocation failed: {ex.Message}");
}
```

## Performance Tips

1. **Cache method info**: Query repository once, store metadata
2. **Reuse instances**: Create instances once, call methods multiple times
3. **Batch queries**: Use LINQ to query once instead of multiple calls
4. **Async for I/O**: Use InvokeMethodAsync for I/O-bound methods

## Full Documentation

See `PACKAGE_REPOSITORY_README.md` for complete documentation.
