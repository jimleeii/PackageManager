using PackageManager.Models;

namespace PackageManager.Repository;

/// <summary>
/// Interface for a repository that stores and retrieves package metadata.
/// </summary>
public interface IPackageRepository
{
    /// <summary>
    /// Adds or updates package metadata in the repository.
    /// </summary>
    /// <param name="metadata">The package metadata to add or update.</param>
    void AddOrUpdate(PackageMetadata metadata);

    /// <summary>
    /// Retrieves package metadata by package ID.
    /// </summary>
    /// <param name="packageId">The package identifier.</param>
    /// <returns>The package metadata if found; otherwise, null.</returns>
    PackageMetadata? GetByPackageId(string packageId);

    /// <summary>
    /// Retrieves package metadata by package ID and version.
    /// </summary>
    /// <param name="packageId">The package identifier.</param>
    /// <param name="version">The package version.</param>
    /// <returns>The package metadata if found; otherwise, null.</returns>
    PackageMetadata? GetByPackageIdAndVersion(string packageId, string version);

    /// <summary>
    /// Gets all packages in the repository.
    /// </summary>
    /// <returns>A collection of all package metadata.</returns>
    IEnumerable<PackageMetadata> GetAll();

    /// <summary>
    /// Finds all methods matching the specified method name across all packages.
    /// </summary>
    /// <param name="methodName">The method name to search for.</param>
    /// <returns>A collection of methods matching the specified name.</returns>
    IEnumerable<PackageMethodInfo> FindMethodsByName(string methodName);

    /// <summary>
    /// Finds all methods in a specific type across all packages.
    /// </summary>
    /// <param name="typeFullName">The fully qualified type name.</param>
    /// <returns>A collection of methods in the specified type.</returns>
    IEnumerable<PackageMethodInfo> FindMethodsByType(string typeFullName);

    /// <summary>
    /// Finds all types matching the specified type name across all packages.
    /// </summary>
    /// <param name="typeName">The type name to search for.</param>
    /// <returns>A collection of types matching the specified name.</returns>
    IEnumerable<PackageTypeInfo> FindTypesByName(string typeName);

    /// <summary>
    /// Removes package metadata from the repository.
    /// </summary>
    /// <param name="packageId">The package identifier to remove.</param>
    /// <returns>True if the package was removed; otherwise, false.</returns>
    bool Remove(string packageId);

    /// <summary>
    /// Removes all packages from the repository.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the total number of packages in the repository.
    /// </summary>
    /// <returns>The total count of packages.</returns>
    int Count();
}
