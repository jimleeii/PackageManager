using System.ComponentModel.DataAnnotations;

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
    [Required(ErrorMessage = "PackageSource is required")]
    [MinLength(1, ErrorMessage = "PackageSource cannot be empty")]
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
    [FrameworkVersions(ErrorMessage = "AllowedFrameworks contains invalid framework identifiers")]
    public List<string> AllowedFrameworks { get; set; } = [];
}

/// <summary>
/// Validates that framework version strings follow NuGet framework naming conventions.
/// </summary>
internal sealed class FrameworkVersionsAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not List<string> frameworks || frameworks.Count == 0)
        {
            return ValidationResult.Success; // Empty list is valid (means all frameworks)
        }

        var invalidFrameworks = frameworks
            .Where(f => string.IsNullOrWhiteSpace(f) || 
                       (!f.StartsWith("net", StringComparison.OrdinalIgnoreCase) && 
                        !f.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) &&
                        !f.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (invalidFrameworks.Count > 0)
        {
            return new ValidationResult(
                $"Invalid framework identifiers: {string.Join(", ", invalidFrameworks)}. " +
                "Framework identifiers must start with 'net', 'netstandard', or 'netcoreapp'.");
        }

        return ValidationResult.Success;
    }
}