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
    public string PackageSource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to enable file watching for package changes.
    /// </summary>
    public bool EnableFileWatching { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to scan and install packages on application startup.
    /// </summary>
    public bool ScanOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of allowed .NET framework versions to load from packages.
    /// If empty, all compatible frameworks will be loaded.
    /// Example values: "net8.0", "net9.0", "net10.0", "netstandard2.0", "netstandard2.1"
    /// This property filters which framework-specific assemblies are loaded from NuGet packages.
    /// </summary>
    public List<string> AllowedFrameworks { get; set; } = [];
}