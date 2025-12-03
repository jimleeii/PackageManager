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
using PackageManager.Helper;
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
    // List to store assembly paths for loaded NuGet packages
    private readonly List<string> _assemblyPaths = [];
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
    // Flag to indicate whether the object has been disposed
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageLoader"/> class with the specified packages folder.
    /// </summary>
    /// <param name="packagesFolder">The path to the folder where NuGet packages will be stored. Defaults to "packages".</param>
    /// <param name="localSource">The path to a local folder containing NuGet packages. Defaults to null.</param>
    /// <param name="packageRepository">Optional package repository for storing package metadata.</param>
    /// <param name="allowedFrameworks">Optional list of allowed framework versions to load. If null or empty, all compatible frameworks are loaded.</param>
    /// <remarks>
    /// This constructor sets up the packages folder and subscribes to the <see cref="AppDomain.AssemblyResolve"/> event
    /// to handle assembly resolution for NuGet packages.
    /// </remarks>
    public PackageLoader(string packagesFolder = "packages", string? localSource = null, IPackageRepository? packageRepository = null, List<string>? allowedFrameworks = null)
    {
        _packagesFolder = Path.GetFullPath(packagesFolder);
        _packageRepository = packageRepository;
        _packageScanner = packageRepository != null ? new PackageScanner() : null;
        _allowedFrameworks = allowedFrameworks;

        // Add default NuGet source
        _packageSources.Add(new PackageSource("https://api.nuget.org/v3/index.json", "nuget.org"));

        // Add local source if provided
        if (!string.IsNullOrEmpty(localSource))
        {
            if (!Directory.Exists(localSource))
                throw new DirectoryNotFoundException($"Local NuGet source directory not found: {localSource}");

            _packageSources.Add(new PackageSource(localSource, "Local"));
        }

        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
    }

    /// <summary>
    /// Installs a package from the specified file path.
    /// </summary>
    /// <param name="filePath">The file path of the package to install</param>
    /// <param name="logger">An optional logger for logging package installation operations</param>
    /// <returns>A task that represents the asynchronous installation operation</returns>
    public async Task InstallPackageAsync(string filePath, Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        // Extract package name and version from file name (e.g., "Newtonsoft.Json.13.0.1")
        string packageName = Path.GetFileNameWithoutExtension(filePath);

        if (string.IsNullOrWhiteSpace(packageName))
        {
            if (logger?.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning) == true)
            {
                logger.LogWarning("Could not extract package name from: {FilePath}", filePath);
            }
            return;
        }

        // Split package name and version (last segment after final dot is assumed to be version)
        var parts = packageName.Split('.');
        string? packageId = null;
        string? version = null;

        // Find where the version starts (first numeric segment)
        int versionStartIndex = -1;
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (char.IsDigit(parts[i][0]))
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
                if (findPackageResource == null) continue;

                var versions = await findPackageResource.GetAllVersionsAsync(
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

        throw new InvalidOperationException($"Unable to find any versions of package '{packageId}' in the configured sources.");
    }

    /// <summary>
    /// Retrieves the current NuGet framework based on the entry assembly's target framework attribute.
    /// </summary>
    /// <returns>
    /// A <see cref="NuGetFramework"/> object that represents the current framework.
    /// If the target framework cannot be determined, returns the fallback framework "net8.0".
    /// </returns>
    private static NuGetFramework GetCurrentNuGetFramework()
    {
        var targetFramework = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        return targetFramework != null ?
            NuGetFramework.Parse(targetFramework) :
            NuGetFramework.Parse("net8.0"); // Updated fallback framework
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
                .Where(f => _allowedFrameworks == null || _allowedFrameworks.Count == 0 || 
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
                    return dirName.StartsWith(packageId, StringComparison.OrdinalIgnoreCase) && dirName.EndsWith(version, StringComparison.OrdinalIgnoreCase);
                });

            if (packageDir == null)
            {
                Console.WriteLine($"Package directory not found for '{packageId}'");
                return Task.CompletedTask;
            }

            // Scan the package
            var metadata = _packageScanner.ScanPackage(packageDir, packageId, version, _allowedFrameworks);

            // Add to repository
            _packageRepository.AddOrUpdate(metadata);

            Console.WriteLine($"Cataloged package '{packageId}' v{version} with {metadata.Methods.Count} methods");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cataloging package '{packageId}': {ex.Message}");
            return Task.FromException(ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the package repository if available.
    /// </summary>
    public IPackageRepository? PackageRepository => _packageRepository;

    /// <summary>
    /// Releases all resources used by the <see cref="PackageLoader"/> instance.
    /// </summary>
    /// <remarks>
    /// This method unregisters the AssemblyResolve event handler and suppresses
    /// finalization to release unmanaged resources and improve performance.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomainAssemblyResolve;
        GC.SuppressFinalize(this);
    }
}