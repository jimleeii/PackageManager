using Microsoft.Extensions.Options;
using PackageManager.Core;
using PackageManager.FileWatching;
using PackageManager.Repository;
using PackageManager.Services;

namespace PackageManager.Configuration;

/// <summary>
/// Provides extension methods for registering package definition services and related functionality with an <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>These extension methods simplify the setup of package management features in an application's
/// dependency injection container. Use these methods to configure package loading and optional file watching
/// capabilities based on application configuration.</remarks>
public static class PackageManagerExtensions
{
    /// <summary>
    /// Adds package definition services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add the services to</param>
    /// <param name="configuration">The configuration containing package definition settings</param>
    /// <remarks>
    /// This method registers a singleton <see cref="PackageLoader"/> configured with settings from the
    /// <see cref="PackageManagerOptions"/> section. File watching is not enabled with this registration.
    /// </remarks>
    public static void AddPackageManager(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PackageManagerOptions>()
            .Bind(configuration.GetSection(PackageManagerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Read options early to determine if file watcher should be registered
        var options = configuration.GetSection(PackageManagerOptions.SectionName).Get<PackageManagerOptions>();

        // Register repository and scanner services
        services.AddSingleton<IPackageRepository, PackageRepository>();
        services.AddSingleton<PackageScanner>();
        services.AddSingleton<DynamicMethodInvoker>();

        // Register PackageLoader as singleton with proper disposal
        services.AddSingleton<PackageLoader>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<PackageManagerOptions>>().Value;
            var repository = sp.GetRequiredService<IPackageRepository>();
            return new PackageLoader(
                packagesFolder: "packages",
                localSource: opts.PackageSource,
                packageRepository: repository,
                allowedFrameworks: opts.AllowedFrameworks,
                fallbackFramework: null,
                useIsolation: opts.UseIsolation  // Default: false for best performance
            );
        });

        // Register disposal hook to ensure PackageLoader is disposed on app shutdown
        services.AddHostedService<PackageLoaderDisposalService>();

        if (options?.EnableFileWatching == true)
        {
            services.AddHostedService<PackageFileWatcherService>();
        }
    }

    /// <summary>
    /// Installs all packages found in the configured package directory during application startup.
    /// </summary>
    /// <param name="app">The web application instance</param>
    /// <returns>A task that represents the asynchronous installation operation</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the configured package directory does not exist and ScanOnStartup is enabled</exception>
    /// <remarks>
    /// This method scans the directory specified in <see cref="PackageManagerOptions.PackageSource"/> for all .nupkg files
    /// and installs them using the registered <see cref="PackageLoader"/>. Call this method in the application startup pipeline
    /// after building the WebApplication to ensure packages are loaded before the application starts handling requests.
    /// Package scanning only occurs if <see cref="PackageManagerOptions.ScanOnStartup"/> is set to true.
    /// </remarks>
    public static async Task UsePackageManager(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<PackageManagerOptions>>().Value;

        // Only scan if ScanOnStartup is enabled
        if (!options.ScanOnStartup)
        {
            return;
        }

        string directoryPath = options.PackageSource;
        if (!Directory.Exists(directoryPath))
        {
            var currentDir = Directory.GetCurrentDirectory();
            var absolutePath = Path.IsPathRooted(directoryPath) 
                ? directoryPath 
                : Path.GetFullPath(Path.Combine(currentDir, directoryPath));
            
            throw new DirectoryNotFoundException(
                $"Package source directory not found: {directoryPath}\n" +
                $"Resolved path: {absolutePath}\n" +
                $"Current directory: {currentDir}\n" +
                $"Tip: Create the directory or update 'PackageManager:PackageSource' in appsettings.json.");
        }

        var loader = app.Services.GetRequiredService<PackageLoader>();
        foreach (var file in Directory.GetFiles(directoryPath, "*.nupkg"))
        {
            await loader.InstallPackageAsync(filePath: file);
        }
    }
}
