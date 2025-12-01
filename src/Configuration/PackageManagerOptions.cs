namespace PackageManager.Configuration;

/// <summary>
/// Configuration options for package definition services
/// </summary>
public class PackageManagerOptions
{
    // Package definition configuration section name
    public const string SectionName = "PackageManager";

    /// <summary>
    /// Gets or sets the local source path or identifier associated with this instance.
    /// </summary>
    public string LocalSource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to enable file watching for package changes.
    /// </summary>
    public bool WithFileWatcher { get; set; } = true;
}