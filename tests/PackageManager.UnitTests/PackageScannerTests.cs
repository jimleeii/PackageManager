using PackageManager.Services;
using System.Reflection;
using Xunit;

namespace PackageManager.UnitTests;

public class PackageScannerTests
{
    [Fact]
    public void ScanPackage_ValidPackage_ReturnsMetadata()
    {
        // Arrange
        var scanner = new PackageScanner();
        var packageId = "TestPackage";
        var version = "1.0.0";

        // We'll use the test assembly's location as a mock package path
        var testAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        // Act
        var metadata = scanner.ScanPackage(testAssemblyPath, packageId, version);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(packageId, metadata.PackageId);
        Assert.Equal(version, metadata.Version);
        Assert.NotNull(metadata.Assemblies);
        Assert.NotNull(metadata.Types);
        Assert.NotNull(metadata.Methods);
    }

    [Fact]
    public void ScanPackage_NonExistentPath_ReturnsEmptyMetadata()
    {
        // Arrange
        var scanner = new PackageScanner();
        var packageId = "TestPackage";
        var version = "1.0.0";
        var invalidPath = @"C:\NonExistent\Path";

        // Act
        var metadata = scanner.ScanPackage(invalidPath, packageId, version);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(packageId, metadata.PackageId);
        Assert.Equal(version, metadata.Version);
        Assert.Empty(metadata.Assemblies);
        Assert.Empty(metadata.Types);
        Assert.Empty(metadata.Methods);
    }

    [Fact]
    public void ScanPackage_NullPackageId_ThrowsArgumentException()
    {
        // Arrange
        var scanner = new PackageScanner();
        var testAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            scanner.ScanPackage(testAssemblyPath, null!, "1.0.0"));
    }

    [Fact]
    public void ScanPackage_EmptyPackageId_ThrowsArgumentException()
    {
        // Arrange
        var scanner = new PackageScanner();
        var testAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            scanner.ScanPackage(testAssemblyPath, "", "1.0.0"));
    }

    [Fact]
    public void ScanPackage_NullVersion_ThrowsArgumentException()
    {
        // Arrange
        var scanner = new PackageScanner();
        var testAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            scanner.ScanPackage(testAssemblyPath, "TestPackage", null!));
    }

    [Fact]
    public void LogMessage_Event_InvokedOnScanFailure()
    {
        // Arrange
        var scanner = new PackageScanner();
        var logMessageInvoked = false;
        string? loggedMessage = null;

        scanner.LogMessage += (sender, args) =>
        {
            logMessageInvoked = true;
            loggedMessage = args.Message;
        };

        // Create a path with invalid dll files
        var tempPath = Path.Combine(Path.GetTempPath(), "InvalidPackageTest");
        Directory.CreateDirectory(tempPath);
        var libPath = Path.Combine(tempPath, "lib", "net8.0");
        Directory.CreateDirectory(libPath);

        // Create an invalid "dll" file
        var invalidDll = Path.Combine(libPath, "Invalid.dll");
        File.WriteAllText(invalidDll, "Not a real DLL");

        try
        {
            // Act
            var metadata = scanner.ScanPackage(tempPath, "TestPackage", "1.0.0");

            // Assert
            Assert.True(logMessageInvoked);
            Assert.NotNull(loggedMessage);
            Assert.Contains("Failed to load assembly", loggedMessage);
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempPath, true);
        }
    }
}
