using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using PackageManager.Helpers;
using PackageManager.Repository;
using PackageManager.Services;
using System.Reflection;
using System.Runtime.Versioning;
using System.Xml.Linq;

namespace PackageManager.Core;

/// <summary>
/// A class for loading NuGet packages and resolving assembly dependencies.
/// </summary>
public class PackageLoader : IDisposable
{
    /// <summary>
    /// Event raised when diagnostic information should be logged.
    /// </summary>
    public event EventHandler<PackageLoaderLogEventArgs>? LogMessage;
    // List to store assembly paths for loaded NuGet packages
    private readonly List<string> _assemblyPaths = [];
    // Custom assembly load context for isolated loading (optional)
    private readonly PackageAssemblyLoadContext? _loadContext;
    // Whether to use assembly isolation
    private readonly bool _useIsolation;
    // Path to the folder where NuGet packages are stored
    private readonly string _packagesFolder;
    // List of NuGet package sources
    private readonly List<PackageSource> _packageSources = [];
    // Package repository for storing metadata
    private readonly IPackageRepository? _packageRepository;
    // Package scanner for extracting metadata
    private readonly PackageScanner? _packageScanner;
    // List of allowed frameworks
    private readonly List<string>? _allowedFrameworks;
    // Fallback framework to use when target framework cannot be determined
    private readonly string _fallbackFramework;
    // Flag to indicate whether the object has been disposed
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageLoader"/> class with the specified packages folder.
    /// </summary>
    /// <param name="packagesFolder">The path to the folder where NuGet packages will be stored. Defaults to "packages".</param>
    /// <param name="localSource">The path to a local folder containing NuGet packages. Defaults to null.</param>
    /// <param name="packageRepository">Optional package repository for storing package metadata.</param>
    /// <param name="allowedFrameworks">Optional list of allowed framework versions to load. If null or empty, all compatible frameworks are loaded.</param>
    /// <param name="fallbackFramework">The fallback framework to use when target framework cannot be determined. If null, auto-detects from current runtime.</param>
    /// <param name="useIsolation">Whether to load assemblies in an isolated AssemblyLoadContext. When true, assemblies can be unloaded. Default is false for backward compatibility.</param>
    /// <remarks>
    /// This constructor sets up the packages folder. If useIsolation is false, it subscribes to the <see cref="AppDomain.AssemblyResolve"/> event.
    /// If useIsolation is true, assemblies are loaded in a custom AssemblyLoadContext that can be unloaded.
    /// </remarks>
    public PackageLoader(string packagesFolder = "packages", string? localSource = null, IPackageRepository? packageRepository = null, List<string>? allowedFrameworks = null, string? fallbackFramework = null, bool useIsolation = false)
    {
        _packagesFolder = Path.GetFullPath(packagesFolder);
        _packageRepository = packageRepository;
        _packageScanner = packageRepository != null ? new PackageScanner() : null;
        _allowedFrameworks = allowedFrameworks;
        _fallbackFramework = fallbackFramework ?? GetRuntimeFrameworkMoniker();
        _useIsolation = useIsolation;

        // Create isolated load context if requested
        if (_useIsolation)
        {
            _loadContext = new PackageAssemblyLoadContext("PackageManager", _assemblyPaths, isCollectible: true);
        }

        // Add default NuGet source
        _packageSources.Add(new PackageSource("https://api.nuget.org/v3/index.json", "nuget.org"));

        // Add local source if provided
        if (!string.IsNullOrEmpty(localSource))
        {
            if (!Directory.Exists(localSource))
            {
                var currentDir = Directory.GetCurrentDirectory();
                throw new DirectoryNotFoundException(
                    $"Local NuGet source directory not found: {localSource}\n" +
                    $"Current directory: {currentDir}\n" +
                    $"Tip: Use an absolute path or ensure the directory exists relative to the current directory.");
            }

            _packageSources.Add(new PackageSource(localSource, "Local"));
        }

        // Only use AppDomain.AssemblyResolve if not using isolated context
        if (!_useIsolation)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
        }
    }

    /// <summary>
    /// Installs a package from the specified file path.
    /// </summary>
    /// <param name="filePath">The file path of the package to install</param>
    /// <param name="logger">An optional logger for logging package installation operations</param>
    /// <returns>A task that represents the asynchronous installation operation</returns>
    public async Task InstallPackageAsync(string filePath, Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        if (!File.Exists(filePath))
        {
            logger?.LogWarning("Package file not found: {FilePath}", filePath);
            return;
        }

        string? packageId = null;
        string? version = null;

        try
        {
            // Use PackageArchiveReader to read metadata from .nuspec file inside .nupkg
            using var packageReader = new PackageArchiveReader(filePath);
            var identity = packageReader.GetIdentity();
            packageId = identity.Id;
            version = identity.Version.ToString();
        }
        catch (Exception ex)
        {
            // Fallback to filename parsing if package reading fails
            logger?.LogWarning("Failed to read package metadata from {FilePath}, falling back to filename parsing: {Error}", filePath, ex.Message);
            
            string packageName = Path.GetFileNameWithoutExtension(filePath);
            if (string.IsNullOrWhiteSpace(packageName))
            {
                logger?.LogWarning("Could not extract package name from: {FilePath}", filePath);
                return;
            }

            // Split package name and version (last segment after final dot is assumed to be version)
            var parts = packageName.Split('.');
            
            // Find where the version starts (first numeric segment from the end)
            int versionStartIndex = -1;
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (parts[i].Length > 0 && char.IsDigit(parts[i][0]))
                {
                    versionStartIndex = i;
                }
                else
                {
                    break;
                }
            }

            if (versionStartIndex > 0)
            {
                packageId = string.Join(".", parts.Take(versionStartIndex));
                version = string.Join(".", parts.Skip(versionStartIndex));
            }
            else
            {
                packageId = packageName;
            }
        }

        if (string.IsNullOrWhiteSpace(packageId))
        {
            logger?.LogWarning("Could not determine package ID from: {FilePath}", filePath);
            return;
        }

        logger?.LogPackageOperation(packageId, version ?? "(latest)", "Loading");
        await InstallPackageAsync(packageId, version);
    }

    /// <summary>
    /// Installs a package to the packages folder.
    /// </summary>
    /// <param name="packageId">The ID of the package to install.</param>
    /// <param name="version">The version of the package to install. If null or empty, the latest stable version will be used.</param>
    /// <remarks>
    /// This method is asynchronous.
    /// The packages folder is configured when the PackageLoader is created.
    /// The framework of the currently executing assembly is used to determine which packages to install.
    /// </remarks>
    public async Task InstallPackageAsync(string packageId, string? version = null)
    {
        var settings = Settings.LoadDefaultSettings(Directory.GetCurrentDirectory());
        var sourceRepositoryProvider = new SourceRepositoryProvider(new PackageSourceProvider(settings), NuGet.Protocol.Core.Types.Repository.Provider.GetCoreV3());

        // Add the package sources to the source repository provider
        foreach (var packageSource in _packageSources)
        {
            sourceRepositoryProvider.PackageSourceProvider.AddPackageSource(packageSource);
        }

        var sourceRepositories = sourceRepositoryProvider.GetRepositories().ToList();

        var framework = GetCurrentNuGetFramework();

        using var cacheContext = new SourceCacheContext();

        // If version is not specified, get the latest stable version
        NuGetVersion packageVersion;
        if (string.IsNullOrWhiteSpace(version))
        {
            packageVersion = await GetLatestVersionAsync(packageId, sourceRepositories, cacheContext, false);
        }
        else
        {
            packageVersion = NuGetVersion.Parse(version);
        }

        var packageIdentity = new PackageIdentity(packageId, packageVersion);

        var resolutionContext = new ResolutionContext(
            DependencyBehavior.Lowest,
            includePrelease : false,
            includeUnlisted : false,
            VersionConstraints.None);

        var logger = new NullLogger();

        var projectContext = new ProjectContext
        {
            PackageExtractionContext = new PackageExtractionContext(PackageSaveMode.Defaultv3, XmlDocFileSaveMode.None, null, logger),
            OriginalPackagesConfig = new XDocument()
        };
        var packageManager = new NuGetPackageManager(sourceRepositoryProvider, settings, _packagesFolder)
        {
            PackagesFolderNuGetProject = new FolderNuGetProject(_packagesFolder)
        };

        await packageManager.InstallPackageAsync(
            packageManager.PackagesFolderNuGetProject,
            packageIdentity,
            resolutionContext,
            projectContext,
            sourceRepositories, [],
            CancellationToken.None);

        CollectAssemblyPaths(framework);

        // Scan and catalog the package if repository is available
        if (_packageRepository != null && _packageScanner != null)
        {
            await ScanAndCatalogPackageAsync(packageId, packageVersion.ToString());
        }
    }

    /// <summary>
    /// Gets the latest version of a package from the specified repositories.
    /// </summary>
    /// <param name="packageId">The ID of the package.</param>
    /// <param name="sourceRepositories">The list of source repositories to search.</param>
    /// <param name="cacheContext">The cache context.</param>
    /// <param name="includePrerelease">Whether to include prerelease versions.</param>
    /// <returns>The latest version of the package.</returns>
    private static async Task<NuGetVersion> GetLatestVersionAsync(
        string packageId,
        List<SourceRepository> sourceRepositories,
        SourceCacheContext cacheContext,
        bool includePrerelease)
    {
        var logger = new NullLogger();

        foreach (var sourceRepository in sourceRepositories)
        {
            try
            {
            var findPackageResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();
            if (findPackageResource == null) 
                continue;                var versions = await findPackageResource.GetAllVersionsAsync(
                    packageId,
                    cacheContext,
                    logger,
                    CancellationToken.None);

                var availableVersions = versions
                    .Where(v => includePrerelease || !v.IsPrerelease)
                    .OrderByDescending(v => v)
                    .ToList();

                if (availableVersions.Count != 0)
                {
                    return availableVersions.First();
                }
            }
            catch
            {
                // Continue to next repository if this one fails
                continue;
            }
        }

        var sourceNames = string.Join(", ", sourceRepositories.Select(s => s.PackageSource.Name));
        throw new InvalidOperationException(
            $"Unable to find any versions of package '{packageId}' in the configured sources.\n" +
            $"Searched sources: {sourceNames}\n" +
            $"Tip: Verify the package ID is correct and the package exists in the configured NuGet sources.");
    }

    /// <summary>
    /// Retrieves the current NuGet framework based on the entry assembly's target framework attribute.
    /// </summary>
    /// <returns>
    /// A <see cref="NuGetFramework"/> object that represents the current framework.
    /// If the target framework cannot be determined, returns the configured fallback framework.
    /// </returns>
    private NuGetFramework GetCurrentNuGetFramework()
    {
        var targetFramework = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        return targetFramework != null 
            ? NuGetFramework.Parse(targetFramework) 
            : NuGetFramework.Parse(_fallbackFramework);
    }

    /// <summary>
    /// Gets the runtime framework moniker for the current executing runtime.
    /// </summary>
    /// <returns>The framework moniker (e.g., "net8.0", "net9.0").</returns>
    private static string GetRuntimeFrameworkMoniker()
    {
        var frameworkName = Assembly.GetExecutingAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        if (frameworkName != null)
        {
            var framework = NuGetFramework.Parse(frameworkName);
            return framework.GetShortFolderName();
        }

        // Final fallback based on runtime version
        var runtimeVersion = Environment.Version;
        return runtimeVersion.Major switch
        {
            >= 10 => "net10.0",
            9 => "net9.0",
            8 => "net8.0",
            7 => "net7.0",
            6 => "net6.0",
            _ => "net8.0" // Ultimate fallback to LTS version
        };
    }

    /// <summary>
    /// Collect assembly paths for the given framework from the packages folder.
    /// </summary>
    /// <param name="framework">The framework to collect assembly paths for.</param>
    /// <remarks>
    /// The best matching framework (i.e. the highest version) is selected for each package.
    /// If AllowedFrameworks is configured, only those frameworks will be considered.
    /// </remarks>
    private void CollectAssemblyPaths(NuGetFramework framework)
    {
        _assemblyPaths.Clear();

        foreach (var packageDir in Directory.EnumerateDirectories(_packagesFolder))
        {
            var libDir = Path.Combine(packageDir, "lib");
            if (!Directory.Exists(libDir)) continue;

            var compatibleFrameworks = Directory.EnumerateDirectories(libDir)
                .Select(d => new
                {
                    Path = d,
                    FrameworkName = Path.GetFileName(d),
                    Framework = NuGetFramework.Parse(Path.GetFileName(d))
                })
                .Where(f => DefaultCompatibilityProvider.Instance.IsCompatible(framework, f.Framework))
                .Where(f => _allowedFrameworks == null || 
                           _allowedFrameworks.Count == 0 || 
                           _allowedFrameworks.Contains(f.FrameworkName, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(f => f.Framework, new PackageFrameworkSorter())
                .ToList();

            var bestFramework = compatibleFrameworks.FirstOrDefault();
            if (bestFramework != null)
            {
                _assemblyPaths.AddRange(Directory.GetFiles(bestFramework.Path, "*.dll"));
            }
        }
    }

    /// <summary>
    /// Resolves and loads an assembly from the collected assembly paths.
    /// Only used when not using AssemblyLoadContext isolation.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event data containing the name of the assembly to resolve.</param>
    /// <returns>
    /// The resolved assembly if found; otherwise, null.
    /// </returns>
    /// <remarks>
    /// This method is used as an event handler for the <see cref="AppDomain.AssemblyResolve"/> event
    /// to load assemblies that are not found by default resolution. It searches through the collected
    /// assembly paths for a matching assembly name and loads it if found.
    /// </remarks>
    private Assembly? CurrentDomainAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        // If using isolated context, this shouldn't be called
        if (_useIsolation)
            return null;

        var assemblyName = new AssemblyName(args.Name);
        var dllPath = _assemblyPaths.FirstOrDefault(p =>
            Path.GetFileNameWithoutExtension(p).Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        return dllPath != null ? Assembly.LoadFrom(dllPath) : null;
    }

    /// <summary>
    /// Scans and catalogs a package, adding its metadata to the repository.
    /// </summary>
    /// <param name="packageId">The package identifier.</param>
    /// <param name="version">The package version.</param>
    private Task ScanAndCatalogPackageAsync(string packageId, string version)
    {
        if (_packageScanner == null || _packageRepository == null)
            return Task.CompletedTask;

        try
        {
            // Find the package directory
            var packageDir = Directory.EnumerateDirectories(_packagesFolder)
                .FirstOrDefault(d =>
                {
                    var dirName = Path.GetFileName(d);
                    return dirName.StartsWith(packageId, StringComparison.OrdinalIgnoreCase) && 
                           dirName.EndsWith(version, StringComparison.OrdinalIgnoreCase);
                });

            if (packageDir == null)
            {
                LogMessage?.Invoke(this, new PackageLoaderLogEventArgs(PackageLoaderLogLevel.Warning, $"Package directory not found for '{packageId}'"));
                return Task.CompletedTask;
            }

            // Scan the package with optional load context for isolated loading
            var metadata = _packageScanner.ScanPackage(packageDir, packageId, version, _allowedFrameworks, _loadContext);

            // Add to repository
            _packageRepository.AddOrUpdate(metadata);

            LogMessage?.Invoke(this, new PackageLoaderLogEventArgs(PackageLoaderLogLevel.Information, $"Cataloged package '{packageId}' v{version} with {metadata.Methods.Count} methods"));
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            LogMessage?.Invoke(this, new PackageLoaderLogEventArgs(PackageLoaderLogLevel.Error, $"Error cataloging package '{packageId}': {ex.Message}", ex));
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Gets the package repository if available.
    /// </summary>
    public IPackageRepository? PackageRepository => _packageRepository;

    /// <summary>
    /// Gets the assembly load context if isolation is enabled.
    /// </summary>
    public PackageAssemblyLoadContext? LoadContext => _loadContext;

    /// <summary>
    /// Gets whether assembly isolation is enabled.
    /// </summary>
    public bool IsIsolationEnabled => _useIsolation;

    /// <summary>
    /// Releases all resources used by the <see cref="PackageLoader"/> instance.
    /// </summary>
    /// <remarks>
    /// This method unregisters the AssemblyResolve event handler (if not using isolation)
    /// and unloads the AssemblyLoadContext (if using isolation), then suppresses
    /// finalization to release unmanaged resources and improve performance.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) 
            return;

        _disposed = true;

        if (_useIsolation)
        {
            // Unload the isolated context (this unloads all assemblies loaded in it)
            _loadContext?.Unload();
        }
        else
        {
            // Only unregister event if we registered it
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomainAssemblyResolve;
        }
        
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Event arguments for PackageLoader logging events.
/// </summary>
public class PackageLoaderLogEventArgs : EventArgs
{
    /// <summary>
    /// Gets the log level.
    /// </summary>
    public PackageLoaderLogLevel Level { get; }

    /// <summary>
    /// Gets the log message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the exception, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageLoaderLogEventArgs"/> class.
    /// </summary>
    public PackageLoaderLogEventArgs(PackageLoaderLogLevel level, string message, Exception? exception = null)
    {
        Level = level;
        Message = message;
        Exception = exception;
    }
}

/// <summary>
/// Log levels for PackageLoader diagnostics.
/// </summary>
public enum PackageLoaderLogLevel
{
    /// <summary>Debug level messages.</summary>
    Debug,
    /// <summary>Informational messages.</summary>
    Information,
    /// <summary>Warning messages.</summary>
    Warning,
    /// <summary>Error messages.</summary>
    Error
}