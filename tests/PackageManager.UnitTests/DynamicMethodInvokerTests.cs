using PackageManager.Models;
using PackageManager.Repository;
using PackageManager.Services;
using Xunit;

namespace PackageManager.UnitTests;

public class DynamicMethodInvokerTests
{
    private IPackageRepository CreateEmptyRepository()
    {
        return new Repository.PackageRepository();
    }

    [Fact]
    public void InvokeMethod_NonExistentMethod_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateEmptyRepository();
        var invoker = new DynamicMethodInvoker(repository);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            invoker.InvokeMethod("NonExistentMethod", null));
    }

    [Fact]
    public void CreateInstance_NonExistentType_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateEmptyRepository();
        var invoker = new DynamicMethodInvoker(repository);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            invoker.CreateInstance("NonExistent.Type"));
    }

    [Fact]
    public async Task InvokeMethodAsync_NonAsyncMethod_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = new Repository.PackageRepository();

        // Add non-async method metadata
        var metadata = new PackageMetadata
        {
            PackageId = "TestPackage",
            Version = "1.0.0",
            PackagePath = "C:\\test",
            Assemblies = new List<string> { "Test.dll" },
            Types = new List<PackageTypeInfo>(),
            Methods = new List<PackageMethodInfo>
            {
                new PackageMethodInfo
                {
                    MethodName = "NonAsyncMethod",
                    TypeFullName = "Test.Class",
                    AssemblyName = "Test.dll",
                    IsStatic = true,
                    IsPublic = true,
                    IsAsync = false,  // Not async
                    ReturnType = "System.String",
                    Parameters = new List<MethodParameterInfo>()
                }
            }
        };
        repository.AddOrUpdate(metadata);

        var invoker = new DynamicMethodInvoker(repository);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await invoker.InvokeMethodAsync("NonAsyncMethod", null));
    }

    [Fact]
    public void InvokeMethod_NullMethodInfo_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateEmptyRepository();
        var invoker = new DynamicMethodInvoker(repository);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            invoker.InvokeMethod((PackageMethodInfo)null!, null));
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DynamicMethodInvoker(null!));
    }

    [Fact]
    public void InvokeMethod_MethodExistsButAssemblyNotLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = new Repository.PackageRepository();
        var metadata = new PackageMetadata
        {
            PackageId = "TestPackage",
            Version = "1.0.0",
            PackagePath = "C:\\test",
            Assemblies = new List<string> { "UnloadedAssembly.dll" },
            Types = new List<PackageTypeInfo>(),
            Methods = new List<PackageMethodInfo>
            {
                new PackageMethodInfo
                {
                    MethodName = "TestMethod",
                    TypeFullName = "Test.Class",
                    AssemblyName = "UnloadedAssembly.dll",
                    IsStatic = true,
                    IsPublic = true,
                    ReturnType = "System.Void",
                    Parameters = new List<MethodParameterInfo>()
                }
            }
        };
        repository.AddOrUpdate(metadata);

        var invoker = new DynamicMethodInvoker(repository);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            invoker.InvokeMethod("TestMethod", null));

        Assert.Contains("Assembly", ex.Message);
        Assert.Contains("not found", ex.Message);
    }
}
