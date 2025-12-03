namespace PackageManager.FileWatching;

/// <summary>
/// Watches for changes to package-related files
/// </summary>
public class PackageFileWatcher : IDisposable
{
    // File system watcher to monitor file changes
    private readonly FileSystemWatcher _watcher;
    // Timer to debounce rapid file change events
    private readonly System.Timers.Timer _debounceTimer;
    // Last changed file path
    private string? _filePath;
    // Flag to indicate if the object has been disposed
    private bool _disposed;

    // Event to raise when a file change is detected
    public event EventHandler<FileChangedEventArgs>? FileChanged;
    
    /// <summary>
    /// Event raised when diagnostic information should be logged.
    /// </summary>
    public event EventHandler<PackageFileWatcherLogEventArgs>? LogMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageFileWatcher"/> class
    /// </summary>
    /// <param name="directoryPath">The directory path to watch for .nupkg file changes</param>
    /// <param name="debounceMilliseconds">The debounce interval in milliseconds to wait after the last change before raising the FileChanged event (default: 500ms)</param>
    /// <exception cref="ArgumentException">Thrown when directoryPath is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist</exception>
    public PackageFileWatcher(string directoryPath, int debounceMilliseconds = 500)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));

        if (!Directory.Exists(directoryPath))
        {
            var currentDir = Directory.GetCurrentDirectory();
            var absolutePath = Path.IsPathRooted(directoryPath) 
                ? directoryPath 
                : Path.GetFullPath(Path.Combine(currentDir, directoryPath));
            
            throw new DirectoryNotFoundException(
                $"Watch directory not found: {directoryPath}\n" +
                $"Resolved path: {absolutePath}\n" +
                $"Tip: Create the directory before enabling file watching.");
        }

        _watcher = new FileSystemWatcher(directoryPath)
        {
            Filter = "*.nupkg",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = false
        };

        _debounceTimer = new System.Timers.Timer(debounceMilliseconds)
        {
            AutoReset = false
        };

        _watcher.Changed += OnFileSystemChanged;
        _watcher.Created += OnFileSystemChanged;
        _watcher.Deleted += OnFileSystemChanged;
        _watcher.Renamed += OnFileRenamed;
        _watcher.Error += OnError;

        _debounceTimer.Elapsed += OnDebounceElapsed;
    }

    /// <summary>
    /// Starts watching the file for changes
    /// </summary>
    public void Start()
    {
        if (!_disposed)
            _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Stops watching the file for changes
    /// </summary>
    public void Stop()
    {
        if (_disposed)
            return;

        _watcher.EnableRaisingEvents = false;
        _debounceTimer.Stop();
    }

    /// <summary>
    /// Handles file change events
    /// </summary>
    /// <param name="sender">The FileSystemWatcher that raised the event</param>
    /// <param name="e">The event data containing information about the file change</param>
    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        _filePath = e.FullPath;

        // Restart debounce timer on each change
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    /// <summary>
    /// Handles file rename events
    /// </summary>
    /// <param name="sender">The FileSystemWatcher that raised the event</param>
    /// <param name="e">The event data containing information about the renamed file</param>
    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        _filePath = e.FullPath;

        // Restart debounce timer on rename
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    /// <summary>
    /// Handles debounce timer elapsed event
    /// </summary>
    /// <param name="sender">The timer that raised the event</param>
    /// <param name="e">The elapsed event data</param>
    private void OnDebounceElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // File changes have settled, raise the event
        var args = new FileChangedEventArgs(_filePath, DateTime.Now);
        FileChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Handles file watcher error events
    /// </summary>
    /// <param name="sender">The FileSystemWatcher that raised the error</param>
    /// <param name="e">The error event data containing exception details</param>
    private void OnError(object sender, ErrorEventArgs e)
    {
        // Handle buffer overflow or other errors
        LogMessage?.Invoke(this, new PackageFileWatcherLogEventArgs($"File watcher error: {e.GetException()?.Message}", e.GetException()));
    }

    /// <summary>
    /// Disposes of the file watcher
    /// </summary>
    public void Dispose()
    {
        if (_disposed) 
            return;

        _disposed = true;
        Stop();

        _watcher.Changed -= OnFileSystemChanged;
        _watcher.Created -= OnFileSystemChanged;
        _watcher.Deleted -= OnFileSystemChanged;
        _watcher.Renamed -= OnFileRenamed;
        _watcher.Error -= OnError;

        _debounceTimer.Elapsed -= OnDebounceElapsed;
        _debounceTimer.Dispose();
        _watcher.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Event arguments for file change notifications
/// </summary>
public class FileChangedEventArgs(string? filePath, DateTime changeTime) : EventArgs
{
    /// <summary>
    /// The file path of the changed file
    /// </summary>
    public string? FilePath { get; } = filePath;
    /// <summary>
    /// The time of the change
    /// </summary>
    public DateTime ChangeTime { get; } = changeTime;
}

/// <summary>
/// Event arguments for PackageFileWatcher logging events.
/// </summary>
public class PackageFileWatcherLogEventArgs : EventArgs
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
    /// Initializes a new instance of the <see cref="PackageFileWatcherLogEventArgs"/> class.
    /// </summary>
    public PackageFileWatcherLogEventArgs(string message, Exception? exception = null)
    {
        Message = message;
        Exception = exception;
    }
}
