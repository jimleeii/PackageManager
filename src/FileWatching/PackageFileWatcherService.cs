using Microsoft.Extensions.Options;
using PackageManager.Configuration;
using PackageManager.Core;
using PackageManager.Helper;

namespace PackageManager.FileWatching;

/// <summary>
/// Background service that watches package definition files for changes
/// </summary>
public class PackageFileWatcherService(
    ILogger<PackageFileWatcherService> logger,
    IOptions<PackageManagerOptions> options,
    PackageLoader packageLoader) : BackgroundService
{
    // The logger instance for logging information and errors
    private readonly ILogger<PackageFileWatcherService> _logger = logger;
    // The configuration options for package definitions
    private readonly PackageManagerOptions _options = options.Value;
    // The package loader instance for loading package definitions
    private readonly PackageLoader _packageLoader = packageLoader;
    // The file watcher instance for monitoring file changes
    private PackageFileWatcher? _watcher;

    /// <summary>
    /// Executes the background service and starts monitoring the local source directory for file changes until
    /// cancellation is requested.
    /// </summary>
    /// <remarks>File watching is disabled if the local source directory is not configured or does not exist.
    /// The service will continue running until the provided cancellation token is triggered.</remarks>
    /// <param name="stoppingToken">A cancellation token that can be used to signal the request to stop the background service.</param>
    /// <returns>A task that represents the lifetime of the background monitoring operation. The task completes when cancellation
    /// is requested or if the local source directory is not configured or does not exist.</returns>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.PackageSource) || !Directory.Exists(_options.PackageSource))
        {
            _logger.LogWarning("LocalSource not configured or does not exist. File watching is disabled.");
            return Task.CompletedTask;
        }

        try
        {
            _watcher = new PackageFileWatcher(_options.PackageSource);
            _watcher.FileChanged += OnFileChanged;
            _watcher.Start();

            _logger.LogFileOperation("Started watching", _options.PackageSource);

            // Keep the service running until cancellation
            return Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithContext(ex, "Failed to start file watcher for: {FilePath}", _options.PackageSource);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handles file change notifications by attempting to load package definitions from the specified file.
    /// </summary>
    /// <remarks>If the file does not exist or the package name cannot be extracted from the file name, the
    /// method logs a warning and returns without performing any package operations. The method logs progress and errors
    /// using the configured logger. This method is asynchronous and returns immediately; any exceptions during package
    /// loading are logged but not propagated.</remarks>
    /// <param name="sender">The source of the file change event. This parameter is typically the file system watcher or monitoring component
    /// that detected the change.</param>
    /// <param name="e">An event argument containing information about the changed file, including its path and relevant metadata.</param>
    private async void OnFileChanged(object? sender, FileChangedEventArgs e)
    {
        _logger.LogFileOperation("File changed detected", e.FilePath);

        using var _ = _logger.LogOperationStart($"Processing file change: {Path.GetFileName(e.FilePath)}");

        try
        {
            // Read package definitions from the file
            if (!File.Exists(e.FilePath))
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("File no longer exists: {FilePath}", e.FilePath);
                }
                return;
            }

            await _packageLoader.InstallPackageAsync(filePath: e.FilePath, logger: _logger);
            _logger.LogInfo("Successfully loaded packages from: {FilePath}", e.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithContext(ex, "Failed to load packages from file: {FilePath}", e.FilePath);
        }
    }

    /// <summary>
    /// Releases all resources used by the current instance of the class.
    /// </summary>
    /// <remarks>This method disposes of managed resources and suppresses finalization to prevent the garbage
    /// collector from calling the finalizer. Call this method when you are finished using the object to free resources
    /// promptly.</remarks>
    public override void Dispose()
    {
        _watcher?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
