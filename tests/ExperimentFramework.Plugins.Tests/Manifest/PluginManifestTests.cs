using ExperimentFramework.Plugins.Manifest;

namespace ExperimentFramework.Plugins.Tests.Manifest;

public class PluginManifestTests
{
    [Fact]
    public void CreateDefault_SetsRequiredFields()
    {
        var manifest = PluginManifest.CreateDefault("Test.Plugin", "2.0.0");

        Assert.Equal("1.0", manifest.ManifestVersion);
        Assert.Equal("Test.Plugin", manifest.Id);
        Assert.Equal("Test.Plugin", manifest.Name);
        Assert.Equal("2.0.0", manifest.Version);
    }

    [Fact]
    public void CreateDefault_WithDefaultVersion()
    {
        var manifest = PluginManifest.CreateDefault("Test.Plugin");

        Assert.Equal("1.0.0", manifest.Version);
    }

    [Fact]
    public void Manifest_HasDefaultIsolation()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test",
            Name = "Test",
            Version = "1.0.0"
        };

        Assert.Equal(Abstractions.PluginIsolationMode.Shared, manifest.Isolation.Mode);
        Assert.Empty(manifest.Isolation.SharedAssemblies);
    }

    [Fact]
    public void Manifest_HasDefaultLifecycle()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test",
            Name = "Test",
            Version = "1.0.0"
        };

        Assert.True(manifest.Lifecycle.SupportsHotReload);
        Assert.False(manifest.Lifecycle.RequiresRestartOnUnload);
    }

    [Fact]
    public void Manifest_WithCustomIsolation()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test",
            Name = "Test",
            Version = "1.0.0",
            Isolation = new Abstractions.PluginIsolationConfig
            {
                Mode = Abstractions.PluginIsolationMode.Full,
                SharedAssemblies = ["Assembly1", "Assembly2"]
            }
        };

        Assert.Equal(Abstractions.PluginIsolationMode.Full, manifest.Isolation.Mode);
        Assert.Equal(2, manifest.Isolation.SharedAssemblies.Count);
    }

    [Fact]
    public void Manifest_WithServices()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test",
            Name = "Test",
            Version = "1.0.0",
            Services =
            [
                new Abstractions.PluginServiceRegistration
                {
                    Interface = "ITestService",
                    Implementations =
                    [
                        new Abstractions.PluginImplementation { Type = "TestServiceA", Alias = "a" },
                        new Abstractions.PluginImplementation { Type = "TestServiceB", Alias = "b" }
                    ]
                }
            ]
        };

        Assert.Single(manifest.Services);
        Assert.Equal("ITestService", manifest.Services[0].Interface);
        Assert.Equal(2, manifest.Services[0].Implementations.Count);
        Assert.Equal("a", manifest.Services[0].Implementations[0].Alias);
        Assert.Equal("b", manifest.Services[0].Implementations[1].Alias);
    }

    [Fact]
    public void Manifest_WithCustomLifecycle()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test",
            Name = "Test",
            Version = "1.0.0",
            Lifecycle = new Abstractions.PluginLifecycleConfig
            {
                SupportsHotReload = false,
                RequiresRestartOnUnload = true
            }
        };

        Assert.False(manifest.Lifecycle.SupportsHotReload);
        Assert.True(manifest.Lifecycle.RequiresRestartOnUnload);
    }

    [Fact]
    public void Manifest_CanSetDescription()
    {
        var manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = "Test",
            Name = "Test",
            Version = "1.0.0",
            Description = "A test plugin"
        };

        Assert.Equal("A test plugin", manifest.Description);
    }
}
