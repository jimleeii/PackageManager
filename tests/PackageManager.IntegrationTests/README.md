# Integration Tests

This project contains integration tests and examples for the PackageManager library.

## What's Included

- **Program.cs**: Integration test runner
- **RepositoryDemo.cs**: Repository system demonstration
- **PackageRepositoryUsageExample.cs**: Comprehensive usage examples

## Running Integration Tests

```bash
dotnet run --project tests/PackageManager.IntegrationTests
```

## Configuration

Integration tests use `appsettings.json` for configuration. Ensure your package source directory is properly configured before running tests.

```json
{
  "PackageManager": {
    "PackageSource": "C:\\path\\to\\packages",
    "AllowedFrameworks": ["net10.0", "net9.0"],
    "EnableFileWatching": true,
    "ScanOnStartup": true
  }
}
```
