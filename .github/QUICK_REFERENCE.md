# ExperimentFramework Quick Reference

This document provides quick reference information for common operations in the ExperimentFramework repository.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Building](#building)
- [Testing](#testing)
- [Running Benchmarks](#running-benchmarks)
- [Documentation](#documentation)
- [CI/CD](#cicd)
- [Contributing](#contributing)

## Prerequisites

- [.NET SDK 8.0, 9.0, or 10.0](https://dotnet.microsoft.com/download)
- Git
- A code editor (Visual Studio, VS Code, Rider, etc.)

## Getting Started

### Clone the Repository
```bash
git clone https://github.com/JerrettDavis/ExperimentFramework.git
cd ExperimentFramework
```

### Restore Dependencies
```bash
dotnet restore --use-lock-file
dotnet tool restore
```

## Building

### Build the Solution
```bash
# Debug build
dotnet build ExperimentFramework.slnx

# Release build
dotnet build ExperimentFramework.slnx --configuration Release
```

## Testing

### Run All Tests
```bash
dotnet test ExperimentFramework.slnx
```

### Run Tests with Coverage
```bash
dotnet test ExperimentFramework.slnx \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --settings tests/ExperimentFramework.Tests/coverage.runsettings
```

### Generate Coverage Report
```bash
dotnet tool update -g dotnet-reportgenerator-globaltool
REPORTS=$(find . -type f -path "*/TestResults/*/coverage.cobertura.xml" | tr '\n' ';')
reportgenerator \
  -reports:"$REPORTS" \
  -targetdir:"coverage-report" \
  -reporttypes:"HtmlInline;Cobertura"
```

## Running Benchmarks

### Windows
```powershell
.\run-benchmarks.ps1
```

### Linux/macOS
```bash
./run-benchmarks.sh
```

## Documentation

### Build Documentation Locally
```bash
dotnet tool update -g docfx
docfx docs/docfx.json --serve
```

Then navigate to `http://localhost:8080` in your browser.

### Generate Configuration Schemas
```bash
dotnet run --project tools/ExperimentFramework.SchemaGenerator/ExperimentFramework.SchemaGenerator.csproj -- ./artifacts/schemas
```

## CI/CD

### Workflows

The repository includes the following GitHub Actions workflows:

- **CI (`ci.yml`)**: Runs on PRs and main branch pushes
  - PR checks: Build, test, and code coverage
  - Release: Version, pack, and publish NuGet packages
  
- **CodeQL (`codeql-analysis.yml`)**: Security analysis
  
- **Documentation (`docs.yml`)**: Publishes documentation to GitHub Pages
  
- **Dependency Review (`dependency-review.yml`)**: Scans PRs for dependency vulnerabilities
  
- **Labeler (`labeler.yml`)**: Automatically labels PRs based on changed files
  
- **Stale (`stale.yml`)**: Marks and closes stale issues/PRs
  
- **Update Packages (`update-packages.yml`)**: Automated dependency updates

### Dependabot

Dependabot is configured to:
- Update NuGet packages weekly
- Update GitHub Actions weekly
- Group minor and patch updates together

## Contributing

### Branch Naming
- `feature/` - New features
- `bugfix/` - Bug fixes
- `hotfix/` - Critical fixes
- `docs/` - Documentation changes

### Commit Messages
Follow conventional commits format:
```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

### Pull Requests
1. Create a feature branch from `main`
2. Make your changes
3. Ensure all tests pass
4. Fill out the PR template
5. Request review from code owners

### Code Owners
- All code: @JerrettDavis
- See `.github/CODEOWNERS` for detailed ownership

## Useful Commands

### Clean Build Artifacts
```bash
dotnet clean ExperimentFramework.slnx
find . -name "bin" -o -name "obj" | xargs rm -rf
```

### Update Global Tools
```bash
dotnet tool restore
dotnet tool update -g docfx
dotnet tool update -g dotnet-reportgenerator-globaltool
```

### Create NuGet Packages
```bash
dotnet pack ExperimentFramework.slnx \
  --configuration Release \
  --output ./artifacts
```

## Troubleshooting

### Build Errors
- Ensure you have the correct .NET SDK versions installed
- Run `dotnet restore --use-lock-file` to restore dependencies
- Clean the solution: `dotnet clean ExperimentFramework.slnx`

### Test Failures
- Check test output for specific error messages
- Ensure all dependencies are restored
- Run tests in isolation to identify flaky tests

### Coverage Report Issues
- Ensure `reportgenerator` is installed: `dotnet tool update -g dotnet-reportgenerator-globaltool`
- Check that coverage files exist in `TestResults` directories

## Resources

- [Project Documentation](https://jerrettdavis.github.io/ExperimentFramework/)
- [Issue Tracker](https://github.com/JerrettDavis/ExperimentFramework/issues)
- [Pull Requests](https://github.com/JerrettDavis/ExperimentFramework/pulls)
- [Discussions](https://github.com/JerrettDavis/ExperimentFramework/discussions)

## License

This project is licensed under the terms specified in the [LICENSE](../LICENSE) file.
