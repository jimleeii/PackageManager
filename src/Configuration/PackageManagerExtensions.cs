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
        services.Configure<PackageManagerOptions>(configuration.GetSection(PackageManagerOptions.SectionName));

        // Read options early to determine if file watcher should be registered
        var options = configuration.GetSection(PackageManagerOptions.SectionName).Get<PackageManagerOptions>();

        // Register repository and scanner services
        services.AddSingleton<IPackageRepository, PackageRepository>();
        services.AddSingleton<PackageScanner>();
        services.AddSingleton<DynamicMethodInvoker>();

        // Register PackageLoader
        services.AddSingleton<PackageLoader>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<PackageManagerOptions>>().Value;
            var repository = sp.GetRequiredService<IPackageRepository>();
            return new PackageLoader("packages", opts.LocalSource, repository);
        });

        if (options?.WithFileWatcher == true)
        {
            services.AddHostedService<PackageFileWatcherService>();
        }
    }

    /// <summary>
    /// Installs all packages found in the configured package directory during application startup.
    /// </summary>
    /// <param name="app">The web application instance</param>
    /// <returns>A task that represents the asynchronous installation operation</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the configured package directory does not exist</exception>
    /// <remarks>
    /// This method scans the directory specified in <see cref="PackageManagerOptions.LocalSource"/> for all .nupkg files
    /// and installs them using the registered <see cref="PackageLoader"/>. Call this method in the application startup pipeline
    /// after building the WebApplication to ensure packages are loaded before the application starts handling requests.
    /// </remarks>
    public static async Task UsePackageManager(this WebApplication app)
    {
        var loader = app.Services.GetRequiredService<PackageLoader>();
        var options = app.Services.GetRequiredService<IOptions<PackageManagerOptions>>().Value;

        string directoryPath = options.LocalSource;
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        foreach (var file in Directory.GetFiles(directoryPath, "*.nupkg"))
        {
            // Extract package ID and version from the .nupkg filename
            string packageName = Path.GetFileNameWithoutExtension(file);

            // Split package name and version (last segment after final dot is assumed to be version)
            var parts = packageName.Split('.');

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

            string packageId;
            string? version = null;
            if (versionStartIndex > 0)
            {
                packageId = string.Join(".", parts.Take(versionStartIndex));
                version = string.Join(".", parts.Skip(versionStartIndex));
            }
            else
            {
                packageId = packageName;
            }

            await loader.InstallPackageAsync(packageId, version);
        }
    }
}
