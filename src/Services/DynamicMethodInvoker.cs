using PackageManager.Models;
using PackageManager.Repository;
using System.Reflection;

namespace PackageManager.Services;

/// <summary>
/// Service for dynamically invoking methods from loaded packages.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DynamicMethodInvoker"/> class.
/// </remarks>
/// <param name="packageRepository">The package repository containing method metadata.</param>
public class DynamicMethodInvoker(IPackageRepository packageRepository)
{
    // The package repository to retrieve method information from
    private readonly IPackageRepository _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
    // A cache of loaded assemblies
    private readonly Dictionary<string, Assembly> _loadedAssemblies = [];

    /// <summary>
    /// Invokes a method by name with the specified parameters.
    /// </summary>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameters">The parameters to pass to the method.</param>
    /// <param name="instance">The instance to invoke the method on (null for static methods).</param>
    /// <returns>The result of the method invocation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the method cannot be found or invoked.</exception>
    public object? InvokeMethod(string methodName, object?[]? parameters = null, object? instance = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        var methodInfos = _packageRepository.FindMethodsByName(methodName).ToList();

        if (methodInfos.Count == 0)
        {
            // Get all available methods for suggestions
            var allMethods = _packageRepository.GetAll()
                .SelectMany(p => p.Methods)
                .Select(m => m.MethodName)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            var suggestions = allMethods
                .Where(m => m.Contains(methodName, StringComparison.OrdinalIgnoreCase) ||
                           LevenshteinDistance(m, methodName) <= 3)
                .Take(5)
                .ToList();

            var suggestionText = suggestions.Count > 0
                ? $" Did you mean: {string.Join(", ", suggestions)}?"
                : $" Available methods in loaded packages: {string.Join(", ", allMethods.Take(10))}...";

            throw new InvalidOperationException(
                $"Method '{methodName}' not found in any loaded package.{suggestionText}");
        }

        // Find the best matching method based on parameter count
        var paramCount = parameters?.Length ?? 0;
        var matchingMethod = methodInfos.FirstOrDefault(m => m.Parameters.Count == paramCount);

        if (matchingMethod == null)
        {
            var availableSignatures = methodInfos.Select(m =>
                $"{m.MethodName}({string.Join(", ", m.Parameters.Select(p => $"{p.Type} {p.Name}"))})"
            ).ToList();

            throw new InvalidOperationException(
                $"No overload of method '{methodName}' matches the provided parameter count ({paramCount}).\n" +
                $"Available signatures:\n  - {string.Join("\n  - ", availableSignatures)}");
        }

        return InvokeMethod(matchingMethod, parameters, instance);
    }

    /// <summary>
    /// Invokes a specific method using its metadata.
    /// </summary>
    /// <param name="methodInfo">The method metadata.</param>
    /// <param name="parameters">The parameters to pass to the method.</param>
    /// <param name="instance">The instance to invoke the method on (null for static methods).</param>
    /// <returns>The result of the method invocation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the method cannot be invoked.</exception>
    public object? InvokeMethod(PackageMethodInfo methodInfo, object?[]? parameters = null, object? instance = null)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);

        // Load the assembly if not already loaded
        var assembly = GetOrLoadAssembly(methodInfo.AssemblyName);

        // Get the type
        var type = assembly.GetType(methodInfo.TypeFullName);
        if (type == null)
            throw new InvalidOperationException($"Type '{methodInfo.TypeFullName}' not found in assembly '{methodInfo.AssemblyName}'.");

        // Get the method
        var parameterTypes = methodInfo.Parameters.Select(p => 
        {
            var paramType = assembly.GetType(p.Type) ?? Type.GetType(p.Type);
            return paramType ?? throw new InvalidOperationException($"Parameter type '{p.Type}' not found.");
        }).ToArray();

        var method = type.GetMethod(
            methodInfo.MethodName,
            BindingFlags.Public | (methodInfo.IsStatic ? BindingFlags.Static : BindingFlags.Instance),
            null,
            parameterTypes,
            null);

        if (method == null)
            throw new InvalidOperationException($"Method '{methodInfo.MethodName}' not found in type '{methodInfo.TypeFullName}'.");

        // Validate instance requirement
        if (!methodInfo.IsStatic && instance == null)
        {
            throw new InvalidOperationException(
                $"Method '{methodInfo.TypeFullName}.{methodInfo.MethodName}' is an instance method and requires an object instance.\n" +
                $"Tip: Create an instance using CreateInstance(\"{methodInfo.TypeFullName}\") and pass it as the 'instance' parameter.");
        }

        if (methodInfo.IsStatic && instance != null)
        {
            throw new InvalidOperationException(
                $"Method '{methodInfo.TypeFullName}.{methodInfo.MethodName}' is static and should be called without an instance.\n" +
                $"Tip: Pass null as the 'instance' parameter for static methods.");
        }

        // Invoke the method
        try
        {
            return method.Invoke(instance, parameters);
        }
        catch (TargetInvocationException ex)
        {
            // Unwrap the inner exception for clearer error messages
            throw new InvalidOperationException($"Error invoking method '{methodInfo.MethodName}': {ex.InnerException?.Message}", ex.InnerException ?? ex);
        }
    }

    /// <summary>
    /// Invokes an async method by name with the specified parameters.
    /// </summary>
    /// <param name="methodName">The name of the async method to invoke.</param>
    /// <param name="parameters">The parameters to pass to the method.</param>
    /// <param name="instance">The instance to invoke the method on (null for static methods).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<object?> InvokeMethodAsync(string methodName, object?[]? parameters = null, object? instance = null)
    {
        var result = InvokeMethod(methodName, parameters, instance);

        if (result is Task task)
        {
            await task.ConfigureAwait(false);

            // Get the result if it's Task<T>
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        return result;
    }

    /// <summary>
    /// Creates an instance of a type from a loaded package.
    /// </summary>
    /// <param name="typeFullName">The fully qualified name of the type.</param>
    /// <param name="constructorArgs">Arguments to pass to the constructor.</param>
    /// <returns>An instance of the specified type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type cannot be instantiated.</exception>
    public object CreateInstance(string typeFullName, params object?[]? constructorArgs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeFullName);

        var typeInfos = _packageRepository.FindTypesByName(typeFullName).ToList();

        if (typeInfos.Count == 0)
        {
            // Get similar type names for suggestions
            var allTypes = _packageRepository.GetAll()
                .SelectMany(p => p.Types)
                .Select(t => t.FullName)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            var suggestions = allTypes
                .Where(t => t.Contains(typeFullName, StringComparison.OrdinalIgnoreCase) ||
                           t.EndsWith("." + typeFullName, StringComparison.OrdinalIgnoreCase) ||
                           LevenshteinDistance(t, typeFullName) <= 3)
                .Take(5)
                .ToList();

            var suggestionText = suggestions.Count > 0
                ? $" Did you mean: {string.Join(", ", suggestions)}?"
                : $" Available types: {string.Join(", ", allTypes.Take(10))}...";

            throw new InvalidOperationException(
                $"Type '{typeFullName}' not found in any loaded package.{suggestionText}");
        }

        var typeInfo = typeInfos.First();
        var assembly = GetOrLoadAssembly(typeInfo.AssemblyName);

        var type = assembly.GetType(typeInfo.FullName);
        if (type == null)
            throw new InvalidOperationException($"Type '{typeFullName}' not found in assembly '{typeInfo.AssemblyName}'.");

        try
        {
            return Activator.CreateInstance(type, constructorArgs) 
                ?? throw new InvalidOperationException(
                    $"Failed to create instance of type '{typeFullName}'.\n" +
                    $"The constructor returned null, which may indicate an abstract class or interface.\n" +
                    $"Tip: Ensure the type is a concrete class with a matching public constructor.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            var argTypes = constructorArgs?.Select(a => a?.GetType().Name ?? "null").ToArray() ?? Array.Empty<string>();
            var argTypeStr = argTypes.Length > 0 ? string.Join(", ", argTypes) : "none";
            
            throw new InvalidOperationException(
                $"Error creating instance of type '{typeFullName}'.\n" +
                $"Constructor arguments: {argTypeStr}\n" +
                $"Error: {ex.Message}\n" +
                $"Tip: Verify the type has a public constructor matching the provided arguments.", ex);
        }
    }

    /// <summary>
    /// Gets or loads an assembly by name.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly.</param>
    /// <returns>The loaded assembly.</returns>
    private Assembly GetOrLoadAssembly(string assemblyName)
    {
        if (_loadedAssemblies.TryGetValue(assemblyName, out var assembly))
            return assembly;

        // Try to find the assembly in the current app domain
        assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name?.Equals(assemblyName, StringComparison.OrdinalIgnoreCase) == true);

        if (assembly == null)
        {
            var loadedPackages = _packageRepository.GetAll()
                .Select(p => $"{p.PackageId} ({string.Join(", ", p.Assemblies)})")
                .ToList();

            var packagesText = loadedPackages.Count > 0
                ? $"\nLoaded packages:\n  - {string.Join("\n  - ", loadedPackages)}"
                : " No packages are currently loaded.";

            throw new InvalidOperationException(
                $"Assembly '{assemblyName}' not found. Ensure the package containing this assembly is loaded.{packagesText}");
        }

        _loadedAssemblies[assemblyName] = assembly;
        return assembly;
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings for similarity matching.
    /// </summary>
    private static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        var distance = new int[source.Length + 1, target.Length + 1];

        for (var i = 0; i <= source.Length; i++) distance[i, 0] = i;
        for (var j = 0; j <= target.Length; j++) distance[0, j] = j;

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = char.ToLowerInvariant(target[j - 1]) == char.ToLowerInvariant(source[i - 1]) ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[source.Length, target.Length];
    }
}
