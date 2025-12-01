namespace PackageManager.Models;

/// <summary>
/// Represents metadata about a loaded NuGet package.
/// </summary>
public class PackageMetadata
{
    /// <summary>
    /// Gets or sets the package identifier.
    /// </summary>
    public required string PackageId { get; set; }

    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets the list of assembly names included in this package.
    /// </summary>
    public required List<string> Assemblies { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of public types exposed by this package.
    /// </summary>
    public required List<PackageTypeInfo> Types { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of public methods available for dynamic calling.
    /// </summary>
    public required List<PackageMethodInfo> Methods { get; set; } = [];

    /// <summary>
    /// Gets or sets the date and time when this package was loaded.
    /// </summary>
    public DateTime LoadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the path to the package folder.
    /// </summary>
    public string? PackagePath { get; set; }
}

/// <summary>
/// Represents type information within a package.
/// </summary>
public class PackageTypeInfo
{
    /// <summary>
    /// Gets or sets the fully qualified name of the type.
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Gets or sets the namespace of the type.
    /// </summary>
    public required string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the simple name of the type.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this type is a class.
    /// </summary>
    public bool IsClass { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this type is an interface.
    /// </summary>
    public bool IsInterface { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this type is abstract.
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this type is static.
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// Gets or sets the assembly name where this type is defined.
    /// </summary>
    public required string AssemblyName { get; set; }
}
