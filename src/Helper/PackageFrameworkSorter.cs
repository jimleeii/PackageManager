using NuGet.Frameworks;

namespace PackageManager.Helper;

/// <summary>
/// Compares two Package Framework objects using a specified precedence sorter.
/// </summary>
internal sealed class PackageFrameworkSorter : IComparer<NuGetFramework>
{
    /// <summary>
    /// Compares two NuGetFramework objects and returns an integer indicating their relative order.
    /// </summary>
    /// <param name="x">The first NuGetFramework object to compare.</param>
    /// <param name="y">The second NuGetFramework object to compare.</param>
    /// <returns>
    /// A negative integer if x is less than y, zero if x equals y, or a positive integer if x is greater than y.
    /// </returns>
    public int Compare(NuGetFramework? x, NuGetFramework? y) => new FrameworkPrecedenceSorter(new FrameworkNameProvider([], []), false).Compare(x, y);
}