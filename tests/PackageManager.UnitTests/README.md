# PackageManager Unit Tests

Comprehensive unit test suite for the PackageManager library using xUnit.

## Running Tests

### Run All Tests
```bash
dotnet test test/Test.csproj
```

### Run with Coverage (requires coverlet)
```bash
dotnet test test/Test.csproj --collect:"XPlat Code Coverage"
```

### Run Specific Test Class
```bash
dotnet test test/Test.csproj --filter "FullyQualifiedName~PackageRepositoryTests"
```

## Test Suites

### PackageRepositoryTests (16 tests)
Tests the core repository functionality with thread-safe operations.

**Coverage:**
- ✅ Adding and updating packages
- ✅ Retrieving packages by ID and version
- ✅ Querying methods by name and type
- ✅ Finding types by name
- ✅ Removing packages
- ✅ Clearing repository
- ✅ Counting packages
- ✅ Thread-safe concurrent operations

**Key Tests:**
- `AddOrUpdate_NewPackage_AddsSuccessfully` - Verifies new package addition
- `AddOrUpdate_ExistingPackage_UpdatesSuccessfully` - Tests package updates
- `GetByPackageId_ExistingPackage_ReturnsPackage` - Direct lookup validation
- `FindMethodsByName_ExistingMethod_ReturnsMethod` - Cross-package method search
- `Repository_ThreadSafety_HandlesConCurrentOperations` - Concurrent access (100 threads)

### PackageScannerTests (7 tests)
Tests assembly scanning and metadata extraction.

**Coverage:**
- ✅ Valid package scanning
- ✅ Non-existent path handling
- ✅ Null parameter validation
- ✅ Empty parameter validation  
- ✅ Event-based logging on failures

**Key Tests:**
- `ScanPackage_ValidPackage_ReturnsMetadata` - Successful scanning
- `ScanPackage_NonExistentPath_ReturnsEmptyMetadata` - Graceful failure
- `ScanPackage_NullPackageId_ThrowsArgumentNullException` - Input validation
- `LogMessage_Event_InvokedOnScanFailure` - Event logging verification

### DynamicMethodInvokerTests (6 tests)
Tests dynamic method invocation with reflection.

**Coverage:**
- ✅ Non-existent method handling
- ✅ Non-existent type handling
- ✅ Async method validation
- ✅ Null parameter checks
- ✅ Constructor validation
- ✅ Assembly loading error handling

**Key Tests:**
- `InvokeMethod_NonExistentMethod_ThrowsInvalidOperationException` - Error handling
- `InvokeMethodAsync_NonAsyncMethod_ThrowsInvalidOperationException` - Async validation
- `Constructor_NullRepository_ThrowsArgumentNullException` - Dependency validation
- `InvokeMethod_MethodExistsButAssemblyNotLoaded_ThrowsInvalidOperationException` - Missing assembly handling

### PackageManagerOptionsTests (7 tests)
Tests configuration options and validation.

**Coverage:**
- ✅ Valid configuration validation
- ✅ Required field enforcement
- ✅ Framework identifier validation
- ✅ Empty/null source handling
- ✅ Mixed valid/invalid frameworks
- ✅ Default values

**Key Tests:**
- `Validate_ValidOptions_ReturnsSuccess` - Happy path validation
- `Validate_NullPackageSource_FailsValidation` - Required field check
- `Validate_InvalidFramework_FailsValidation` - Custom validator test
- `Validate_ValidFrameworks_PassesValidation` - Framework whitelist validation

## Test Statistics

| Suite | Tests | Coverage |
|-------|-------|----------|
| **PackageRepositoryTests** | 16 | Repository CRUD, queries, concurrency |
| **PackageScannerTests** | 7 | Scanning, validation, events |
| **DynamicMethodInvokerTests** | 6 | Reflection, error handling |
| **PackageManagerOptionsTests** | 7 | Configuration validation |
| **Total** | **36** | **Core functionality** |

## Test Categories

### ✅ Positive Tests (Happy Path)
- Package CRUD operations
- Successful scanning
- Valid configuration
- Thread-safe operations

### ❌ Negative Tests (Error Handling)
- Null/empty parameters
- Non-existent entities
- Invalid configurations
- Assembly loading failures

### 🔒 Validation Tests
- Data annotations
- Framework identifiers
- Required fields
- Parameter guards

### ⚡ Concurrency Tests
- 100-thread concurrent repository access
- Thread-safe ConcurrentDictionary operations

## Code Quality

**Test Patterns:**
- Arrange-Act-Assert (AAA) pattern
- Clear test naming (Method_Scenario_ExpectedResult)
- Isolated test fixtures
- No test dependencies

**Best Practices:**
- Each test is independent
- No shared mutable state
- Helper methods for common setup
- Descriptive assertion messages

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Run Tests
  run: dotnet test --logger "trx;LogFileName=test-results.trx"

- name: Publish Test Results
  uses: dorny/test-reporter@v1
  if: always()
  with:
    name: Test Results
    path: '**/test-results.trx'
    reporter: dotnet-trx
```

### Azure DevOps Example
```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    projects: '**/Test.csproj'
    arguments: '--configuration Release --collect:"XPlat Code Coverage"'
```

## Future Test Enhancements

- [ ] Integration tests with real NuGet packages
- [ ] Performance tests for large repositories
- [ ] Load tests for concurrent access
- [ ] Code coverage reporting (> 80% target)
- [ ] Mutation testing
- [ ] Property-based testing with FsCheck
- [ ] Mock frameworks for complex scenarios
- [ ] End-to-end workflow tests

## Dependencies

- **xUnit 2.9.3** - Test framework
- **xUnit.runner.visualstudio 2.8.2** - VS Test adapter
- **Microsoft.NET.Test.Sdk 17.12.0** - SDK support
- **Moq 4.20.72** - Mocking framework (available but not heavily used)

## Notes

⚠️ **Test Isolation**: Tests create temporary files for scanning tests. These are cleaned up in `finally` blocks.

⚠️ **Assembly Loading**: Some tests verify error handling when assemblies aren't loaded rather than testing actual invocation (which would require real NuGet packages).

✅ **Thread Safety**: Repository tests include concurrent access validation with 100 parallel operations.
