# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- EditorConfig file for consistent code style enforcement
- Directory.Build.props for centralized MSBuild properties
- CHANGELOG.md for version tracking
- Comprehensive file structure documentation in README.md

### Changed
- Renamed `Helper/` folder to `Helpers/` following .NET naming conventions
- Updated README.md with detailed solution structure tree
- Improved namespace organization (PackageManager.Helper → PackageManager.Helpers)

### Fixed
- Namespace consistency across all source files

## [1.0.0] - 2025-12-02

### Added
- Dynamic NuGet package installation at runtime
- Package repository for cataloging loaded packages
- Dynamic method invocation capabilities
- Type and method discovery across loaded packages
- Package metadata models (PackageMetadata, PackageMethodInfo)
- File watching service for package directory monitoring
- PackageLoader with repository integration
- PackageScanner for assembly metadata extraction
- DynamicMethodInvoker for runtime method calls
- Comprehensive configuration system with PackageManagerOptions
- Dependency injection support via PackageManagerExtensions
- Thread-safe in-memory repository implementation
- Package assembly load context isolation
- Project context management
- Framework compatibility sorting
- Logger extensions for structured logging
- Unit tests for core components
- Benchmark suite for performance testing
- Usage examples and demonstrations

### Features
- **Configuration/**
  - Service registration and dependency injection
  - Configurable package source and framework support
  - File watching toggle and scan-on-startup options
  
- **Core/**
  - Isolated assembly loading with PackageAssemblyLoadContext
  - Dynamic package installation and loading
  - Project context with dependency resolution
  
- **Repository/**
  - IPackageRepository interface and implementation
  - Thread-safe package metadata storage
  - Query methods for packages, types, and methods
  
- **Services/**
  - Assembly scanning with reflection
  - Dynamic method invocation with parameter type checking
  - Support for static and instance methods
  
- **FileWatching/**
  - Background service for monitoring package files
  - Automatic package reload on file changes
  
- **Models/**
  - Rich metadata models for packages and methods
  - Support for method parameters, return types, and attributes

[Unreleased]: https://github.com/yourusername/PackageManager/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/yourusername/PackageManager/releases/tag/v1.0.0
