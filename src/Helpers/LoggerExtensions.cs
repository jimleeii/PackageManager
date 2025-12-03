namespace PackageManager.Helpers;

/// <summary>
/// Provides extension methods for logging operations throughout the application.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs an informational message with structured parameters.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for the message template.</param>
    public static void LogInfo(this ILogger logger, string message, params object?[] args)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(message, args);
        }
    }

    /// <summary>
    /// Logs a debug message with structured parameters.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for the message template.</param>
    public static void LogDebug(this ILogger logger, string message, params object?[] args)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(message, args);
        }
    }

    /// <summary>
    /// Logs an error with exception details and context information.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message describing the error context.</param>
    /// <param name="args">Optional arguments for the message template.</param>
    public static void LogErrorWithContext(this ILogger logger, Exception exception, string message, params object?[] args)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(exception, message, args);
        }
    }

    /// <summary>
    /// Logs the start of an operation with timing information.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operationName">The name of the operation starting.</param>
    /// <returns>A disposable scope that logs completion when disposed.</returns>
    public static IDisposable? LogOperationStart(this ILogger logger, string operationName)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return null;
        }

        logger.LogInformation("Starting operation: {OperationName}", operationName);
        return new OperationScope(logger, operationName);
    }

    /// <summary>
    /// Logs a package-related operation with package details.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="packageId">The package identifier.</param>
    /// <param name="version">The package version.</param>
    /// <param name="action">The action being performed on the package.</param>
    public static void LogPackageOperation(this ILogger logger, string packageId, string version, string action)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Package operation - {Action}: {PackageId} v{Version}", action, packageId, version);
        }
    }

    /// <summary>
    /// Logs file system operations with file path details.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The file operation being performed.</param>
    /// <param name="filePath">The file path involved in the operation.</param>
    public static void LogFileOperation(this ILogger logger, string operation, string? filePath)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("File operation - {Operation}: {FilePath}", operation, filePath ?? "N/A");
        }
    }

    /// <summary>
    /// Logs performance metrics for operations.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    public static void LogPerformance(this ILogger logger, string operationName, long durationMs)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Performance - {OperationName} completed in {DurationMs}ms", operationName, durationMs);
        }
    }

    /// <summary>
    /// Scope that tracks operation timing and logs completion.
    /// </summary>
    private sealed class OperationScope(ILogger logger, string operationName) : IDisposable
    {
        // Keep a reference to the logger and operation name for logging
        private readonly ILogger _logger = logger;
        // Store the operation name
        private readonly string _operationName = operationName;
        // Stopwatch to measure elapsed time
        private readonly System.Diagnostics.Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        // Flag to indicate if the scope has been disposed
        private bool _disposed;

        /// <summary>
        /// Disposes the scope and logs completion.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _stopwatch.Stop();
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Completed operation: {OperationName} in {DurationMs}ms",
                    _operationName, _stopwatch.ElapsedMilliseconds);
            }

            _disposed = true;
        }
    }
}
