using System.Reflection;
using ExperimentFramework.Plugins.Loading;

namespace ExperimentFramework.Plugins.Tests.Loading;

public class SharedTypeRegistryTests
{
    [Fact]
    public void DefaultSharedAssemblies_ContainsExperimentFramework()
    {
        Assert.Contains("ExperimentFramework", SharedTypeRegistry.DefaultSharedAssemblies);
        Assert.Contains("ExperimentFramework.Configuration", SharedTypeRegistry.DefaultSharedAssemblies);
        Assert.Contains("ExperimentFramework.Plugins", SharedTypeRegistry.DefaultSharedAssemblies);
    }

    [Fact]
    public void DefaultSharedAssemblies_ContainsDependencyInjection()
    {
        Assert.Contains("Microsoft.Extensions.DependencyInjection.Abstractions", SharedTypeRegistry.DefaultSharedAssemblies);
        Assert.Contains("Microsoft.Extensions.DependencyInjection", SharedTypeRegistry.DefaultSharedAssemblies);
    }

    [Fact]
    public void Constructor_IncludesDefaultAssemblies()
    {
        var registry = new SharedTypeRegistry();

        Assert.True(registry.IsShared("ExperimentFramework"));
        Assert.True(registry.IsShared("Microsoft.Extensions.DependencyInjection.Abstractions"));
    }

    [Fact]
    public void Constructor_IncludesAdditionalAssemblies()
    {
        var registry = new SharedTypeRegistry(["Custom.Assembly"]);

        Assert.True(registry.IsShared("ExperimentFramework"));
        Assert.True(registry.IsShared("Custom.Assembly"));
    }

    [Fact]
    public void IsShared_WithAssemblyName_ReturnsCorrectly()
    {
        var registry = new SharedTypeRegistry();
        var assemblyName = new AssemblyName("ExperimentFramework");

        Assert.True(registry.IsShared(assemblyName));
    }

    [Fact]
    public void IsShared_UnknownAssembly_ReturnsFalse()
    {
        var registry = new SharedTypeRegistry();

        Assert.False(registry.IsShared("Unknown.Assembly"));
    }

    [Fact]
    public void AddSharedAssembly_AddsToRegistry()
    {
        var registry = new SharedTypeRegistry();
        var assembly = typeof(SharedTypeRegistryTests).Assembly;

        registry.AddSharedAssembly("TestAssembly", assembly);

        Assert.True(registry.IsShared("TestAssembly"));
        Assert.Contains("TestAssembly", registry.SharedAssemblyNames);
    }

    [Fact]
    public void TryGetSharedAssembly_NonSharedAssembly_ReturnsFalse()
    {
        var registry = new SharedTypeRegistry();
        var assemblyName = new AssemblyName("Unknown.Assembly");

        var result = registry.TryGetSharedAssembly(assemblyName, out var assembly);

        Assert.False(result);
        Assert.Null(assembly);
    }

    [Fact]
    public void SharedAssemblyNames_ReturnsAllNames()
    {
        var registry = new SharedTypeRegistry(["Custom.One", "Custom.Two"]);

        var names = registry.SharedAssemblyNames;

        Assert.Contains("ExperimentFramework", names);
        Assert.Contains("Custom.One", names);
        Assert.Contains("Custom.Two", names);
    }
}
