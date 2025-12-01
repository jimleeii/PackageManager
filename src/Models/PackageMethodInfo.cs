namespace PackageManager.Models;

/// <summary>
/// Represents metadata about a method within a package for dynamic invocation.
/// </summary>
public class PackageMethodInfo
{
    /// <summary>
    /// Gets or sets the fully qualified name of the type containing this method.
    /// </summary>
    public required string TypeFullName { get; set; }

    /// <summary>
    /// Gets or sets the name of the method.
    /// </summary>
    public required string MethodName { get; set; }

    /// <summary>
    /// Gets or sets the return type of the method.
    /// </summary>
    public required string ReturnType { get; set; }

    /// <summary>
    /// Gets or sets the parameter information for the method.
    /// </summary>
    public required List<MethodParameterInfo> Parameters { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this method is static.
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this method is public.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this method is async.
    /// </summary>
    public bool IsAsync { get; set; }

    /// <summary>
    /// Gets or sets the assembly name where this method is defined.
    /// </summary>
    public required string AssemblyName { get; set; }
}

/// <summary>
/// Represents parameter information for a method.
/// </summary>
public class MethodParameterInfo
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this parameter is optional.
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Gets or sets the default value if the parameter is optional.
    /// </summary>
    public object? DefaultValue { get; set; }
}
