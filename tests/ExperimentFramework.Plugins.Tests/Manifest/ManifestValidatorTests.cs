using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Manifest;

namespace ExperimentFramework.Plugins.Tests.Manifest;

public class ManifestValidatorTests
{
    private readonly ManifestValidator _validator = new();

    [Fact]
    public void Validate_ValidManifest_ReturnsSuccess()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "1.0.0"
        };

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_MissingId_ReturnsError()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "",
            Name = "Test Plugin",
            Version = "1.0.0"
        };

        var result = _validator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ID is required"));
    }

    [Fact]
    public void Validate_InvalidPluginId_ReturnsError()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Invalid Plugin ID!",
            Name = "Test Plugin",
            Version = "1.0.0"
        };

        var result = _validator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("is invalid"));
    }

    [Fact]
    public void Validate_ValidPluginId_WithDotsAndHyphens_Succeeds()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Acme.Experiments-Plugin_v2",
            Name = "Test Plugin",
            Version = "1.0.0"
        };

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MissingName_ReturnsError()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "",
            Version = "1.0.0"
        };

        var result = _validator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("name is required"));
    }

    [Fact]
    public void Validate_MissingVersion_ReturnsError()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = ""
        };

        var result = _validator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("version is required"));
    }

    [Fact]
    public void Validate_NonSemVerVersion_ReturnsWarning()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "v1"
        };

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid); // Warnings don't fail validation
        Assert.Contains(result.Warnings, w => w.Contains("semantic versioning"));
    }

    [Fact]
    public void Validate_UnknownManifestVersion_ReturnsWarning()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "2.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "1.0.0"
        };

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("Unknown manifest version"));
    }

    [Fact]
    public void Validate_ServiceWithMissingInterface_ReturnsError()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Services =
            [
                new PluginServiceRegistration
                {
                    Interface = "",
                    Implementations =
                    [
                        new PluginImplementation { Type = "SomeType" }
                    ]
                }
            ]
        };

        var result = _validator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("missing interface"));
    }

    [Fact]
    public void Validate_ImplementationWithMissingType_ReturnsError()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Services =
            [
                new PluginServiceRegistration
                {
                    Interface = "ITestService",
                    Implementations =
                    [
                        new PluginImplementation { Type = "" }
                    ]
                }
            ]
        };

        var result = _validator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("missing type"));
    }

    [Fact]
    public void Validate_InvalidAlias_ReturnsError()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Services =
            [
                new PluginServiceRegistration
                {
                    Interface = "ITestService",
                    Implementations =
                    [
                        new PluginImplementation { Type = "SomeType", Alias = "invalid alias!" }
                    ]
                }
            ]
        };

        var result = _validator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Invalid alias"));
    }

    [Fact]
    public void Validate_DuplicateAliases_ReturnsError()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Services =
            [
                new PluginServiceRegistration
                {
                    Interface = "ITestService",
                    Implementations =
                    [
                        new PluginImplementation { Type = "TypeA", Alias = "duplicate" },
                        new PluginImplementation { Type = "TypeB", Alias = "duplicate" }
                    ]
                }
            ]
        };

        var result = _validator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate alias"));
    }

    [Fact]
    public void Validate_FullIsolationWithSharedAssemblies_ReturnsWarning()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Isolation = new PluginIsolationConfig
            {
                Mode = PluginIsolationMode.Full,
                SharedAssemblies = ["SomeAssembly"]
            }
        };

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("will be ignored"));
    }

    [Fact]
    public void Validate_ValidAlias_ReturnsSuccess()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Services =
            [
                new PluginServiceRegistration
                {
                    Interface = "ITestService",
                    Implementations =
                    [
                        new PluginImplementation { Type = "SomeType", Alias = "valid-alias_123" }
                    ]
                }
            ]
        };

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "",
            Name = "",
            Version = ""
        };

        var result = _validator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3); // At least ID, name, and version errors
    }

    [Fact]
    public void Validate_SharedIsolationWithSharedAssemblies_DoesNotWarn()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Isolation = new PluginIsolationConfig
            {
                Mode = PluginIsolationMode.Shared,
                SharedAssemblies = ["SomeAssembly"]
            }
        };

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Warnings, w => w.Contains("will be ignored"));
    }

    [Fact]
    public void Validate_NoneIsolationWithSharedAssemblies_DoesNotWarn()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Isolation = new PluginIsolationConfig
            {
                Mode = PluginIsolationMode.None,
                SharedAssemblies = ["SomeAssembly"]
            }
        };

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Warnings, w => w.Contains("will be ignored"));
    }

    [Fact]
    public void Validate_ServiceWithNullImplementations_HandlesGracefully()
    {
        // Services with null implementations should be handled
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Services =
            [
                new PluginServiceRegistration
                {
                    Interface = "ITestService",
                    Implementations = []
                }
            ]
        };

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyServices_ReturnsSuccess()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test.Plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Services = []
        };

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid);
    }
}
