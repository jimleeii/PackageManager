using PackageManager.Models;
using PackageManager.Repository;
using Xunit;

namespace PackageManager.UnitTests;

public class PackageRepositoryTests
{
    private IPackageRepository CreateRepository()
    {
        return new Repository.PackageRepository();
    }

    private PackageMetadata CreateSampleMetadata(string packageId = "TestPackage", string version = "1.0.0")
    {
        return new PackageMetadata
        {
            PackageId = packageId,
            Version = version,
            PackagePath = $@"C:\packages\{packageId}.{version}",
            Assemblies = new List<string> { $"{packageId}.dll" },
            Types = new List<PackageTypeInfo>
            {
                new PackageTypeInfo
                {
                    FullName = $"{packageId}.TestClass",
                    Name = "TestClass",
                    Namespace = packageId,
                    AssemblyName = $"{packageId}.dll",
                    IsClass = true
                }
            },
            Methods = new List<PackageMethodInfo>
            {
                new PackageMethodInfo
                {
                    MethodName = "TestMethod",
                    TypeFullName = $"{packageId}.TestClass",
                    AssemblyName = $"{packageId}.dll",
                    IsStatic = true,
                    IsPublic = true,
                    ReturnType = "System.String",
                    Parameters = new List<MethodParameterInfo>()
                }
            }
        };
    }

    [Fact]
    public void AddOrUpdate_NewPackage_AddsSuccessfully()
    {
        // Arrange
        var repository = CreateRepository();
        var metadata = CreateSampleMetadata();

        // Act
        repository.AddOrUpdate(metadata);

        // Assert
        Assert.Equal(1, repository.Count());
        var retrieved = repository.GetByPackageId("TestPackage");
        Assert.NotNull(retrieved);
        Assert.Equal("TestPackage", retrieved.PackageId);
    }

    [Fact]
    public void AddOrUpdate_ExistingPackage_UpdatesSuccessfully()
    {
        // Arrange
        var repository = CreateRepository();
        var metadata1 = CreateSampleMetadata();
        var metadata2 = CreateSampleMetadata();
        metadata2.Methods.Add(new PackageMethodInfo
        {
            MethodName = "AnotherMethod",
            TypeFullName = "TestPackage.TestClass",
            AssemblyName = "TestPackage.dll",
            IsStatic = false,
            IsPublic = true,
            ReturnType = "System.Void",
            Parameters = new List<MethodParameterInfo>()
        });

        // Act
        repository.AddOrUpdate(metadata1);
        repository.AddOrUpdate(metadata2);

        // Assert
        Assert.Equal(1, repository.Count());
        var retrieved = repository.GetByPackageId("TestPackage");
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.Methods.Count);
    }

    [Fact]
    public void GetByPackageId_ExistingPackage_ReturnsPackage()
    {
        // Arrange
        var repository = CreateRepository();
        var metadata = CreateSampleMetadata("MyPackage");
        repository.AddOrUpdate(metadata);

        // Act
        var result = repository.GetByPackageId("MyPackage");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MyPackage", result.PackageId);
    }

    [Fact]
    public void GetByPackageId_NonExistingPackage_ReturnsNull()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = repository.GetByPackageId("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetByPackageIdAndVersion_ExistingPackage_ReturnsPackage()
    {
        // Arrange
        var repository = CreateRepository();
        var metadata = CreateSampleMetadata("MyPackage", "2.0.0");
        repository.AddOrUpdate(metadata);

        // Act
        var result = repository.GetByPackageIdAndVersion("MyPackage", "2.0.0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MyPackage", result.PackageId);
        Assert.Equal("2.0.0", result.Version);
    }

    [Fact]
    public void GetAll_MultiplePackages_ReturnsAllPackages()
    {
        // Arrange
        var repository = CreateRepository();
        repository.AddOrUpdate(CreateSampleMetadata("Package1", "1.0.0"));
        repository.AddOrUpdate(CreateSampleMetadata("Package2", "1.0.0"));
        repository.AddOrUpdate(CreateSampleMetadata("Package3", "1.0.0"));

        // Act
        var all = repository.GetAll().ToList();

        // Assert
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void FindMethodsByName_ExistingMethod_ReturnsMethod()
    {
        // Arrange
        var repository = CreateRepository();
        var metadata = CreateSampleMetadata();
        repository.AddOrUpdate(metadata);

        // Act
        var methods = repository.FindMethodsByName("TestMethod").ToList();

        // Assert
        Assert.Single(methods);
        Assert.Equal("TestMethod", methods[0].MethodName);
    }

    [Fact]
    public void FindMethodsByName_NonExistingMethod_ReturnsEmpty()
    {
        // Arrange
        var repository = CreateRepository();
        var metadata = CreateSampleMetadata();
        repository.AddOrUpdate(metadata);

        // Act
        var methods = repository.FindMethodsByName("NonExistent").ToList();

        // Assert
        Assert.Empty(methods);
    }

    [Fact]
    public void FindMethodsByType_ExistingType_ReturnsMethods()
    {
        // Arrange
        var repository = CreateRepository();
        var metadata = CreateSampleMetadata();
        metadata.Methods.Add(new PackageMethodInfo
        {
            MethodName = "AnotherMethod",
            TypeFullName = "TestPackage.TestClass",
            AssemblyName = "TestPackage.dll",
            IsStatic = false,
            IsPublic = true,
            ReturnType = "System.Void",
            Parameters = new List<MethodParameterInfo>()
        });
        repository.AddOrUpdate(metadata);

        // Act
        var methods = repository.FindMethodsByType("TestPackage.TestClass").ToList();

        // Assert
        Assert.Equal(2, methods.Count);
    }

    [Fact]
    public void FindTypesByName_ExistingType_ReturnsTypes()
    {
        // Arrange
        var repository = CreateRepository();
        var metadata = CreateSampleMetadata();
        repository.AddOrUpdate(metadata);

        // Act
        var types = repository.FindTypesByName("TestClass").ToList();

        // Assert
        Assert.Single(types);
        Assert.Equal("TestClass", types[0].Name);
    }

    [Fact]
    public void Remove_ExistingPackage_RemovesSuccessfully()
    {
        // Arrange
        var repository = CreateRepository();
        var metadata = CreateSampleMetadata();
        repository.AddOrUpdate(metadata);

        // Act
        var removed = repository.Remove("TestPackage");

        // Assert
        Assert.True(removed);
        Assert.Equal(0, repository.Count());
    }

    [Fact]
    public void Remove_NonExistingPackage_ReturnsFalse()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var removed = repository.Remove("NonExistent");

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void Clear_MultiplePackages_RemovesAll()
    {
        // Arrange
        var repository = CreateRepository();
        repository.AddOrUpdate(CreateSampleMetadata("Package1"));
        repository.AddOrUpdate(CreateSampleMetadata("Package2"));
        repository.AddOrUpdate(CreateSampleMetadata("Package3"));

        // Act
        repository.Clear();

        // Assert
        Assert.Equal(0, repository.Count());
    }

    [Fact]
    public void Count_EmptyRepository_ReturnsZero()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var count = repository.Count();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void Count_WithPackages_ReturnsCorrectCount()
    {
        // Arrange
        var repository = CreateRepository();
        repository.AddOrUpdate(CreateSampleMetadata("Package1"));
        repository.AddOrUpdate(CreateSampleMetadata("Package2"));

        // Act
        var count = repository.Count();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Repository_ThreadSafety_HandlesConCurrentOperations()
    {
        // Arrange
        var repository = CreateRepository();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() =>
            {
                repository.AddOrUpdate(CreateSampleMetadata($"Package{index}", "1.0.0"));
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, repository.Count());
    }
}
