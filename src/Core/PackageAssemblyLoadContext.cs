using System.Reflection;
using System.Runtime.Loader;

namespace PackageManager.Core;

/// <summary>
/// Custom AssemblyLoadContext for loading package assemblies in isolation.
/// Enables assembly unloading and prevents conflicts with host application assemblies.
/// </summary>
public class PackageAssemblyLoadContext : AssemblyLoadContext
{
    // Resolver function for loading assemblies
    private readonly AssemblyDependencyResolver? _resolver;
    // Additional paths to search for assemblies
    private readonly List<string> _assemblyPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageAssemblyLoadContext"/> class.
    /// </summary>
    /// <param name="name">The name of the load context.</param>
    /// <param name="assemblyPaths">Additional paths to search for assemblies.</param>
    /// <param name="isCollectible">Whether the context can be unloaded.</param>
    public PackageAssemblyLoadContext(string name, List<string> assemblyPaths, bool isCollectible = true) 
        : base(name, isCollectible)
    {
        _assemblyPaths = assemblyPaths ?? [];
        
        // Use the first assembly path as the resolver base (if available)
        var basePath = _assemblyPaths.FirstOrDefault();
        _resolver = !string.IsNullOrEmpty(basePath) && File.Exists(basePath)
            ? new AssemblyDependencyResolver(basePath)
            : null;
    }

    /// <summary>
    /// Loads an assembly from the additional assembly paths.
    /// </summary>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to use the resolver first
        if (_resolver != null)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
        }

        // Search in additional assembly paths
        var dllPath = _assemblyPaths.FirstOrDefault(p =>
            Path.GetFileNameWithoutExtension(p).Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        if (dllPath != null && File.Exists(dllPath))
        {
            return LoadFromAssemblyPath(dllPath);
        }

        // Fall back to default context (allows loading shared framework assemblies)
        return null;
    }

    /// <summary>
    /// Resolves unmanaged libraries.
    /// </summary>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        if (_resolver != null)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Gets all loaded assemblies in this context.
    /// </summary>
    public IEnumerable<Assembly> GetLoadedAssemblies()
    {
        return Assemblies;
    }
}
