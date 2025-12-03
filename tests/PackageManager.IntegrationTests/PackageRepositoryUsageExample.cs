using PackageManager.Repository;
using PackageManager.Services;

namespace PackageManager.IntegrationTests;

/// <summary>
/// Example usage of the package repository and dynamic method invoker.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PackageRepositoryUsageExample"/> class.
/// </remarks>
public class PackageRepositoryUsageExample(IPackageRepository packageRepository, DynamicMethodInvoker methodInvoker)
{
    private readonly IPackageRepository _packageRepository = packageRepository;
    private readonly DynamicMethodInvoker _methodInvoker = methodInvoker;

    /// <summary>
    /// Example: Query all loaded packages.
    /// </summary>
    public void ListAllPackages()
    {
        var packages = _packageRepository.GetAll();
        
        Console.WriteLine("=== Loaded Packages ===");
        foreach (var package in packages)
        {
            Console.WriteLine($"Package: {package.PackageId} v{package.Version}");
            Console.WriteLine($"  Assemblies: {package.Assemblies.Count}");
            Console.WriteLine($"  Types: {package.Types.Count}");
            Console.WriteLine($"  Methods: {package.Methods.Count}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Example: Find methods by name across all packages.
    /// </summary>
    public void FindMethodsByName(string methodName)
    {
        var methods = _packageRepository.FindMethodsByName(methodName);
        
        Console.WriteLine($"=== Methods named '{methodName}' ===");
        foreach (var method in methods)
        {
            Console.WriteLine($"Type: {method.TypeFullName}");
            Console.WriteLine($"Method: {method.MethodName}");
            Console.WriteLine($"Return Type: {method.ReturnType}");
            Console.WriteLine($"Parameters: {method.Parameters.Count}");
            Console.WriteLine($"Static: {method.IsStatic}");
            Console.WriteLine($"Assembly: {method.AssemblyName}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Example: Find all types in loaded packages.
    /// </summary>
    public void FindTypesByName(string typeName)
    {
        var types = _packageRepository.FindTypesByName(typeName);
        
        Console.WriteLine($"=== Types matching '{typeName}' ===");
        foreach (var type in types)
        {
            Console.WriteLine($"Full Name: {type.FullName}");
            Console.WriteLine($"Namespace: {type.Namespace}");
            Console.WriteLine($"Is Class: {type.IsClass}");
            Console.WriteLine($"Is Interface: {type.IsInterface}");
            Console.WriteLine($"Is Static: {type.IsStatic}");
            Console.WriteLine($"Assembly: {type.AssemblyName}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Example: Invoke a static method dynamically.
    /// </summary>
    public void InvokeStaticMethod()
    {
        try
        {
            // Example: Invoke a static method like Newtonsoft.Json.JsonConvert.SerializeObject
            var result = _methodInvoker.InvokeMethod(
                "SerializeObject",
                parameters: [new { Name = "Test", Value = 123 }]);
            
            Console.WriteLine($"Method result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error invoking method: {ex.Message}");
        }
    }

    /// <summary>
    /// Example: Invoke an instance method dynamically.
    /// </summary>
    public void InvokeInstanceMethod()
    {
        try
        {
            // First create an instance
            var instance = _methodInvoker.CreateInstance("SomeType.FullName");
            
            // Then invoke a method on it
            var result = _methodInvoker.InvokeMethod(
                "SomeMethod",
                parameters: ["param1", 42],
                instance: instance);
            
            Console.WriteLine($"Method result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Example: Invoke an async method.
    /// </summary>
    public async Task InvokeAsyncMethod()
    {
        try
        {
            var result = await _methodInvoker.InvokeMethodAsync(
                "SomeAsyncMethod",
                parameters: ["param1"]);
            
            Console.WriteLine($"Async method result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Example: Get detailed information about a specific package.
    /// </summary>
    public void GetPackageDetails(string packageId)
    {
        var package = _packageRepository.GetByPackageId(packageId);
        
        if (package == null)
        {
            Console.WriteLine($"Package '{packageId}' not found.");
            return;
        }

        Console.WriteLine($"=== Package Details: {package.PackageId} ===");
        Console.WriteLine($"Version: {package.Version}");
        Console.WriteLine($"Path: {package.PackagePath}");
        Console.WriteLine($"Loaded At: {package.LoadedAt}");
        Console.WriteLine();

        Console.WriteLine("Assemblies:");
        foreach (var assembly in package.Assemblies)
        {
            Console.WriteLine($"  - {assembly}");
        }
        Console.WriteLine();

        Console.WriteLine($"Public Types ({package.Types.Count}):");
        foreach (var type in package.Types.Take(10)) // Show first 10
        {
            Console.WriteLine($"  - {type.FullName}");
        }
        if (package.Types.Count > 10)
            Console.WriteLine($"  ... and {package.Types.Count - 10} more");
        Console.WriteLine();

        Console.WriteLine($"Public Methods ({package.Methods.Count}):");
        foreach (var method in package.Methods.Take(10)) // Show first 10
        {
            var paramStr = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
            Console.WriteLine($"  - {method.ReturnType} {method.MethodName}({paramStr})");
        }
        if (package.Methods.Count > 10)
            Console.WriteLine($"  ... and {package.Methods.Count - 10} more");
    }

    /// <summary>
    /// Example: Search for methods by return type.
    /// </summary>
    public void FindMethodsByReturnType(string returnType)
    {
        var allMethods = _packageRepository.GetAll()
            .SelectMany(p => p.Methods)
            .Where(m => m.ReturnType.Contains(returnType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"=== Methods returning '{returnType}' ===");
        foreach (var method in allMethods.Take(20))
        {
            Console.WriteLine($"{method.TypeFullName}.{method.MethodName} -> {method.ReturnType}");
        }
        
        if (allMethods.Count > 20)
            Console.WriteLine($"... and {allMethods.Count - 20} more");
    }
}
