using PackageManager.Models;
using System.Collections.Concurrent;

namespace PackageManager.Repository;

/// <summary>
/// Thread-safe in-memory repository for storing package metadata.
/// </summary>
public class PackageRepository : IPackageRepository
{
    // Thread-safe dictionary to store packages by their ID
    private readonly ConcurrentDictionary<string, PackageMetadata> _packages = new();

    /// <inheritdoc/>
    public void AddOrUpdate(PackageMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentException.ThrowIfNullOrWhiteSpace(metadata.PackageId);

        _packages.AddOrUpdate(
            metadata.PackageId,
            metadata,
            (key, existing) => metadata);
    }

    /// <inheritdoc/>
    public PackageMetadata? GetByPackageId(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        return _packages.TryGetValue(packageId, out var metadata) ? metadata : null;
    }

    /// <inheritdoc/>
    public PackageMetadata? GetByPackageIdAndVersion(string packageId, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        return _packages.Values
            .FirstOrDefault(p => p.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase) &&
                               p.Version.Equals(version, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public IEnumerable<PackageMetadata> GetAll()
    {
        // Return a snapshot to avoid issues with concurrent modifications
        return _packages.Values.ToList();
    }

    /// <inheritdoc/>
    public IEnumerable<PackageMethodInfo> FindMethodsByName(string methodName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        // Return query without materializing - let caller decide when to enumerate
        return _packages.Values
            .SelectMany(p => p.Methods)
            .Where(m => m.MethodName.Equals(methodName, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public IEnumerable<PackageMethodInfo> FindMethodsByType(string typeFullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeFullName);

        // Return query without materializing - let caller decide when to enumerate
        return _packages.Values
            .SelectMany(p => p.Methods)
            .Where(m => m.TypeFullName.Equals(typeFullName, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public IEnumerable<PackageTypeInfo> FindTypesByName(string typeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

        // Return query without materializing - let caller decide when to enumerate
        return _packages.Values
            .SelectMany(p => p.Types)
            .Where(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                       t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public bool Remove(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        return _packages.TryRemove(packageId, out _);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _packages.Clear();
    }

    /// <inheritdoc/>
    public int Count()
    {
        return _packages.Count;
    }
}
