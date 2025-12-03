using PackageManager.Core;

namespace PackageManager.Configuration;

/// <summary>
/// Background service that ensures PackageLoader is properly disposed on application shutdown.
/// </summary>
/// <remarks>
/// Since PackageLoader is registered as a singleton and implements IDisposable,
/// this service ensures the Dispose method is called when the application stops.
/// This unregisters the AssemblyResolve event handler to prevent resource leaks.
/// </remarks>
internal sealed class PackageLoaderDisposalService : IHostedService
{
    private readonly PackageLoader _packageLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageLoaderDisposalService"/> class.
    /// </summary>
    /// <param name="packageLoader">The PackageLoader singleton to dispose on shutdown.</param>
    public PackageLoaderDisposalService(PackageLoader packageLoader)
    {
        _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
    }

    /// <summary>
    /// No action needed on startup.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the PackageLoader when the application is stopping.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _packageLoader.Dispose();
        return Task.CompletedTask;
    }
}
