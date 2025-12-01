using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PackageManager.Repository;
using PackageManager.Services;

namespace Test;

/// <summary>
/// Example class demonstrating how to use the package repository system.
/// Call DemoRepositoryUsage from your main program to see this in action.
/// </summary>
public static class RepositoryDemo
{
    public static async Task DemoRepositoryUsage(WebApplication app)
    {
        // Get services
        var repository = app.Services.GetRequiredService<IPackageRepository>();
        var invoker = app.Services.GetRequiredService<DynamicMethodInvoker>();

        Console.WriteLine("=== Package Repository Demo ===\n");

        // 1. List all loaded packages
        Console.WriteLine("1. Loaded Packages:");
        var packages = repository.GetAll();
        foreach (var package in packages)
        {
            Console.WriteLine($"   - {package.PackageId} v{package.Version}");
            Console.WriteLine($"     Assemblies: {package.Assemblies.Count}, Types: {package.Types.Count}, Methods: {package.Methods.Count}");
        }
        Console.WriteLine($"   Total: {repository.Count()} packages\n");

        // 2. Find methods by name
        Console.WriteLine("2. Finding methods named 'SerializeObject':");
        var serializeMethods = repository.FindMethodsByName("SerializeObject");
        foreach (var method in serializeMethods.Take(3))
        {
            var paramStr = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
            Console.WriteLine($"   - {method.TypeFullName}.{method.MethodName}({paramStr})");
            Console.WriteLine($"     Returns: {method.ReturnType}, Static: {method.IsStatic}");
        }
        Console.WriteLine();

        // 3. Try to invoke a method if available
        if (serializeMethods.Any())
        {
            try
            {
                Console.WriteLine("3. Dynamic Method Invocation:");
                var testObject = new { Name = "Test Package", Version = "1.0.0", IsActive = true };
                var result = invoker.InvokeMethod("SerializeObject", new object[] { testObject });
                Console.WriteLine($"   SerializeObject result: {result}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error invoking method: {ex.Message}\n");
            }
        }

        // 4. Find all types
        Console.WriteLine("4. Sample Types from Packages:");
        var allTypes = repository.GetAll().SelectMany(p => p.Types).Take(10);
        foreach (var type in allTypes)
        {
            Console.WriteLine($"   - {type.FullName}");
            Console.WriteLine($"     Class: {type.IsClass}, Interface: {type.IsInterface}, Static: {type.IsStatic}");
        }
        Console.WriteLine();

        // 5. Find async methods
        Console.WriteLine("5. Async Methods:");
        var asyncMethods = repository.GetAll()
            .SelectMany(p => p.Methods)
            .Where(m => m.IsAsync)
            .Take(5);
        foreach (var method in asyncMethods)
        {
            Console.WriteLine($"   - {method.TypeFullName}.{method.MethodName}");
        }
        Console.WriteLine();

        Console.WriteLine("=== Demo Complete ===\n");
        Console.WriteLine("Package repository is ready for dynamic method invocation!");

        await Task.CompletedTask;
    }
}

