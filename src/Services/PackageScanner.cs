using PackageManager.Models;
using PackageManager.Core;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace PackageManager.Services;

/// <summary>
/// Service for scanning assemblies and extracting package metadata.
/// </summary>
public class PackageScanner
{
    /// <summary>
    /// Event raised when diagnostic information should be logged.
    /// </summary>
    public event EventHandler<PackageScannerLogEventArgs>? LogMessage;
    /// <summary>
    /// Scans an assembly and extracts metadata about its types and methods.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>Collections of types and methods found in the assembly.</returns>
    public (List<PackageTypeInfo> Types, List<PackageMethodInfo> Methods) ScanAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var types = new List<PackageTypeInfo>();
        var methods = new List<PackageMethodInfo>();

        try
        {
            var exportedTypes = assembly.GetExportedTypes();

            foreach (var type in exportedTypes)
            {
                // Skip compiler-generated types
                if (type.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                    continue;

                // Add type information
                types.Add(new PackageTypeInfo
                {
                    FullName = type.FullName ?? type.Name,
                    Namespace = type.Namespace ?? string.Empty,
                    Name = type.Name,
                    IsClass = type.IsClass,
                    IsInterface = type.IsInterface,
                    IsAbstract = type.IsAbstract,
                    IsStatic = type.IsAbstract && type.IsSealed,
                    AssemblyName = assembly.GetName().Name ?? string.Empty
                });

                // Scan public methods
                var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

                foreach (var method in publicMethods)
                {
                    // Skip property getters/setters and event methods
                    if (method.IsSpecialName)
                        continue;

                    // Skip compiler-generated methods
                    if (method.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                        continue;

                    var parameters = method.GetParameters()
                        .Select(p => new MethodParameterInfo
                        {
                            Name = p.Name ?? string.Empty,
                            Type = p.ParameterType.FullName ?? p.ParameterType.Name,
                            IsOptional = p.IsOptional,
                            DefaultValue = p.HasDefaultValue ? p.DefaultValue : null
                        })
                        .ToList();

                    var returnType = method.ReturnType;
                    var isAsync = returnType.FullName?.StartsWith("System.Threading.Tasks.Task") == true;

                    methods.Add(new PackageMethodInfo
                    {
                        TypeFullName = type.FullName ?? type.Name,
                        MethodName = method.Name,
                        ReturnType = returnType.FullName ?? returnType.Name,
                        Parameters = parameters,
                        IsStatic = method.IsStatic,
                        IsPublic = method.IsPublic,
                        IsAsync = isAsync,
                        AssemblyName = assembly.GetName().Name ?? string.Empty
                    });
                }
            }
        }
        catch (Exception ex)
        {
            // Log or handle exceptions during assembly scanning
            LogMessage?.Invoke(this, new PackageScannerLogEventArgs($"Error scanning assembly {assembly.FullName}: {ex.Message}", ex));
        }

        return (types, methods);
    }

    /// <summary>
    /// Scans a package directory and creates metadata for all assemblies found.
    /// </summary>
    /// <param name="packagePath">The path to the package directory.</param>
    /// <param name="packageId">The package identifier.</param>
    /// <param name="version">The package version.</param>
    /// <param name="allowedFrameworks">Optional list of allowed framework versions. If null or empty, all frameworks are scanned.</param>
    /// <param name="loadContext">Optional AssemblyLoadContext for isolated loading. If null, uses default context.</param>
    /// <returns>Package metadata containing all scanned information.</returns>
    public PackageMetadata ScanPackage(string packagePath, string packageId, string version, List<string>? allowedFrameworks = null, AssemblyLoadContext? loadContext = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var metadata = new PackageMetadata
        {
            PackageId = packageId,
            Version = version,
            PackagePath = packagePath,
            Assemblies = [],
            Types = [],
            Methods = []
        };

        // Find lib directory
        var libPath = Path.Combine(packagePath, "lib");
        if (!Directory.Exists(libPath))
            return metadata;

        // Scan all framework folders
        foreach (var frameworkDir in Directory.EnumerateDirectories(libPath))
        {
            var frameworkName = Path.GetFileName(frameworkDir);
            
            // Skip if framework is not in allowed list (when list is provided and not empty)
            if (allowedFrameworks != null && allowedFrameworks.Count > 0 && 
                !allowedFrameworks.Contains(frameworkName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var dllFiles = Directory.GetFiles(frameworkDir, "*.dll");

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    // Load assembly in specified context or default
                    var assembly = loadContext != null
                        ? loadContext.LoadFromAssemblyPath(dllFile)
                        : Assembly.LoadFrom(dllFile);
                        
                    metadata.Assemblies.Add(assembly.GetName().Name ?? Path.GetFileNameWithoutExtension(dllFile));

                    var (types, methods) = ScanAssembly(assembly);
                    metadata.Types.AddRange(types);
                    metadata.Methods.AddRange(methods);
                }
                catch (Exception ex)
                {
                    // Log assembly load failures but continue scanning
                    LogMessage?.Invoke(this, new PackageScannerLogEventArgs($"Failed to load assembly {dllFile}: {ex.Message}", ex));
                }
            }
        }

        return metadata;
    }
}

/// <summary>
/// Event arguments for PackageScanner logging events.
/// </summary>
public class PackageScannerLogEventArgs : EventArgs
{
    /// <summary>
    /// Gets the log message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the exception, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageScannerLogEventArgs"/> class.
    /// </summary>
    public PackageScannerLogEventArgs(string message, Exception? exception = null)
    {
        Message = message;
        Exception = exception;
    }
}
