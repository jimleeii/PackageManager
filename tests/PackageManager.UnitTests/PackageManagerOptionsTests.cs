using PackageManager.Configuration;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace PackageManager.UnitTests;

public class PackageManagerOptionsTests
{
    [Fact]
    public void Validate_ValidOptions_ReturnsSuccess()
    {
        // Arrange
        var options = new PackageManagerOptions
        {
            PackageSource = @"C:\packages",
            AllowedFrameworks = new List<string> { "net9.0", "net8.0" },
            EnableFileWatching = true,
            ScanOnStartup = true
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void Validate_NullPackageSource_FailsValidation()
    {
        // Arrange
        var options = new PackageManagerOptions
        {
            PackageSource = null!,
            AllowedFrameworks = new List<string> { "net9.0" },
            EnableFileWatching = true,
            ScanOnStartup = true
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Single(validationResults);
        Assert.Contains("PackageSource", validationResults[0].MemberNames);
    }

    [Fact]
    public void Validate_EmptyPackageSource_FailsValidation()
    {
        // Arrange
        var options = new PackageManagerOptions
        {
            PackageSource = "",
            AllowedFrameworks = new List<string> { "net9.0" },
            EnableFileWatching = true,
            ScanOnStartup = true
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Validate_InvalidFramework_FailsValidation()
    {
        // Arrange
        var options = new PackageManagerOptions
        {
            PackageSource = @"C:\packages",
            AllowedFrameworks = new List<string> { "invalid_framework" },
            EnableFileWatching = true,
            ScanOnStartup = true
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Single(validationResults);
        Assert.Contains("Invalid framework identifiers", validationResults[0].ErrorMessage);
    }

    [Fact]
    public void Validate_ValidFrameworks_PassesValidation()
    {
        // Arrange
        var validFrameworks = new List<string>
        {
            "net9.0",
            "net8.0",
            "net7.0",
            "netstandard2.1",
            "netstandard2.0",
            "netcoreapp3.1"
        };

        var options = new PackageManagerOptions
        {
            PackageSource = @"C:\packages",
            AllowedFrameworks = validFrameworks,
            EnableFileWatching = false,
            ScanOnStartup = false
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Validate_EmptyAllowedFrameworks_PassesValidation()
    {
        // Arrange - Empty list means all frameworks are allowed
        var options = new PackageManagerOptions
        {
            PackageSource = @"C:\packages",
            AllowedFrameworks = new List<string>(),
            EnableFileWatching = true,
            ScanOnStartup = true
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Validate_MixedValidAndInvalidFrameworks_FailsValidation()
    {
        // Arrange
        var options = new PackageManagerOptions
        {
            PackageSource = @"C:\packages",
            AllowedFrameworks = new List<string>
            {
                "net9.0",           // valid
                "invalid",          // invalid
                "netstandard2.1"    // valid
            },
            EnableFileWatching = true,
            ScanOnStartup = true
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void DefaultValues_NewInstance_HasCorrectDefaults()
    {
        // Arrange & Act
        var options = new PackageManagerOptions
        {
            PackageSource = "test"
        };

        // Assert
        Assert.True(options.EnableFileWatching);
        Assert.True(options.ScanOnStartup);
        Assert.NotNull(options.AllowedFrameworks);
        Assert.Empty(options.AllowedFrameworks);
    }
}
