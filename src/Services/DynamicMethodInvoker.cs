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
            throw new InvalidOperationException($"Method '{methodName}' not found in any loaded package.");

        // Find the best matching method based on parameter count
        var paramCount = parameters?.Length ?? 0;
        var matchingMethod = methodInfos.FirstOrDefault(m => m.Parameters.Count == paramCount);

        if (matchingMethod == null)
        {
            throw new InvalidOperationException(
                $"No overload of method '{methodName}' matches the provided parameter count ({paramCount}). " +
                $"Available overloads: {string.Join(", ", methodInfos.Select(m => m.Parameters.Count))}");
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
            throw new InvalidOperationException($"Method '{methodInfo.MethodName}' is not static and requires an instance.");

        if (methodInfo.IsStatic && instance != null)
            throw new InvalidOperationException($"Method '{methodInfo.MethodName}' is static and should not have an instance.");

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
            throw new InvalidOperationException($"Type '{typeFullName}' not found in any loaded package.");

        var typeInfo = typeInfos.First();
        var assembly = GetOrLoadAssembly(typeInfo.AssemblyName);

        var type = assembly.GetType(typeInfo.FullName);
        if (type == null)
            throw new InvalidOperationException($"Type '{typeFullName}' not found in assembly '{typeInfo.AssemblyName}'.");

        try
        {
            return Activator.CreateInstance(type, constructorArgs) 
                ?? throw new InvalidOperationException($"Failed to create instance of type '{typeFullName}'.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error creating instance of type '{typeFullName}': {ex.Message}", ex);
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
            throw new InvalidOperationException($"Assembly '{assemblyName}' not found. Ensure the package is loaded.");

        _loadedAssemblies[assemblyName] = assembly;
        return assembly;
    }
}
