# Multi-Targeting Support

The ExperimentFramework now supports **.NET 8.0, .NET 9.0, and .NET 10.0** across all library projects to maximize compatibility and adoption.

## Supported Target Frameworks

### Library Projects
All library projects in the `src/` directory multi-target the following frameworks:
- **net8.0** - .NET 8 (LTS - Long Term Support)
- **net9.0** - .NET 9 (STS - Standard Term Support)
- **net10.0** - .NET 10 (Latest)

### Source Generators
Source generator projects remain on **.NET Standard 2.0** as required by the Roslyn compiler:
- `ExperimentFramework.Generators`
- `ExperimentFramework.Plugins.Generators`

### Test/Sample/Benchmark Projects
Test, sample, and benchmark projects target **.NET 10.0 only** as they don't require multi-targeting for distribution.

## NuGet Package Support

When you install an ExperimentFramework NuGet package, the appropriate target framework will be automatically selected:

- **.NET 8 projects** → Use `net8.0` assemblies
- **.NET 9 projects** → Use `net9.0` assemblies  
- **.NET 10 projects** → Use `net10.0` assemblies

## Package Version Management

The framework uses centralized package version management in `Directory.Build.props`:

- **Microsoft.Extensions.*** packages: Version `8.0.2` for `netstandard2.0`/`netstandard2.1` targets; `net8.0`, `net9.0`, and `net10.0` rely on the versions provided by the corresponding .NET runtime/SDK
- **Microsoft.FeatureManagement**: Version `4.4.0` (supports all target frameworks)

## Why Not .NET Standard 2.0/2.1?

While .NET Standard would provide broader compatibility, we chose to target .NET 8+ because:

1. **Modern C# Features**: The codebase uses C# 11+ features (init properties, required members, etc.) that require extensive polyfills for .NET Standard
2. **Dependency Requirements**: Key dependencies like:
   - `Microsoft.Extensions.Hosting.Abstractions`
   - `Microsoft.Extensions.Configuration`
   - Polly 8.x
   - Entity Framework Core 10.x
   
   All require .NET 8+ in their latest versions
3. **Ecosystem Alignment**: The .NET ecosystem has largely moved to .NET 8 as the minimum supported version
4. **Maintenance Burden**: Supporting .NET Standard would require significant conditional compilation and polyfills

## Migration Guide

### From .NET 10 Only
If you're currently using .NET 10, no changes are required. The framework continues to support .NET 10.

### Adding .NET 8 or .NET 9 Support to Your App
To use ExperimentFramework in a .NET 8 or .NET 9 application:

1. Update your project file to target the desired framework:
   ```xml
   <TargetFramework>net8.0</TargetFramework>
   <!-- or -->
   <TargetFramework>net9.0</TargetFramework>
   ```

2. Install or update the ExperimentFramework packages:
   ```bash
   dotnet add package ExperimentFramework
   ```

3. The appropriate version will be automatically selected by NuGet

## Build Requirements

To build the solution locally, you need:
- .NET SDK 8.0 or later
- .NET SDK 9.0 (for multi-targeting)
- .NET SDK 10.0 (for multi-targeting)

You can install multiple SDKs side-by-side. The build system will automatically use the correct SDK for each target framework.

## CI/CD Considerations

The GitHub Actions CI/CD pipeline has been updated to install all three SDKs:

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: |
      8.0.x
      9.0.x
      10.0.x
```

This ensures all target frameworks are built and tested in the CI pipeline.

## Feature Parity

All features are available across all supported target frameworks (net8.0, net9.0, net10.0). There are no framework-specific limitations or conditional features.

## Support Policy

- **.NET 8 (LTS)**: Supported until November 2026
- **.NET 9 (STS)**: Supported until May 2026  
- **.NET 10**: Current latest version

We recommend using .NET 8 for production applications due to its Long Term Support status.
