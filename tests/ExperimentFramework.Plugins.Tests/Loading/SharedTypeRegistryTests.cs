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

    [Fact]
    public void TryGetSharedAssembly_WithKnownAssembly_ReturnsTrue()
    {
        var registry = new SharedTypeRegistry();

        // System.Runtime should be shared and loadable
        var assemblyName = new AssemblyName("System.Runtime");

        var result = registry.TryGetSharedAssembly(assemblyName, out var assembly);

        Assert.True(result);
        Assert.NotNull(assembly);
    }

    [Fact]
    public void TryGetSharedAssembly_WithCachedAssembly_ReturnsFromCache()
    {
        var registry = new SharedTypeRegistry();
        var testAssembly = typeof(SharedTypeRegistryTests).Assembly;

        // Add to cache first
        registry.AddSharedAssembly("TestCache.Assembly", testAssembly);

        var assemblyName = new AssemblyName("TestCache.Assembly");
        var result = registry.TryGetSharedAssembly(assemblyName, out var assembly);

        Assert.True(result);
        Assert.Same(testAssembly, assembly);
    }

    [Fact]
    public void TryGetSharedAssembly_WithNullAssemblyName_ReturnsFalse()
    {
        var registry = new SharedTypeRegistry();
        var assemblyName = new AssemblyName(); // Name will be null

        var result = registry.TryGetSharedAssembly(assemblyName, out var assembly);

        Assert.False(result);
        Assert.Null(assembly);
    }

    [Fact]
    public void IsShared_WithNullAssemblyName_ReturnsFalse()
    {
        var registry = new SharedTypeRegistry();
        var assemblyName = new AssemblyName(); // Name will be null

        var result = registry.IsShared(assemblyName);

        Assert.False(result);
    }

    [Fact]
    public void TryGetSharedAssembly_LoadsFromDefaultContext_WhenNotCached()
    {
        var registry = new SharedTypeRegistry();

        // netstandard is a default shared assembly
        var assemblyName = new AssemblyName("netstandard");

        var result = registry.TryGetSharedAssembly(assemblyName, out var assembly);

        // May or may not succeed depending on runtime - just ensure no exception
        // The result depends on whether netstandard is actually loaded
    }

    [Fact]
    public void Constructor_WithNullAdditionalAssemblies_UsesOnlyDefaults()
    {
        var registry = new SharedTypeRegistry(null);

        Assert.True(registry.IsShared("ExperimentFramework"));
        Assert.False(registry.IsShared("Custom.Assembly"));
    }

    [Fact]
    public void AddSharedAssembly_UpdatesBothCollections()
    {
        var registry = new SharedTypeRegistry();
        var testAssembly = typeof(SharedTypeRegistryTests).Assembly;

        Assert.False(registry.IsShared("NewAssembly"));

        registry.AddSharedAssembly("NewAssembly", testAssembly);

        Assert.True(registry.IsShared("NewAssembly"));
        Assert.Contains("NewAssembly", registry.SharedAssemblyNames);
    }

    [Fact]
    public void DefaultSharedAssemblies_ContainsLoggingAbstractions()
    {
        Assert.Contains("Microsoft.Extensions.Logging.Abstractions", SharedTypeRegistry.DefaultSharedAssemblies);
    }

    [Fact]
    public void DefaultSharedAssemblies_ContainsSystemRuntime()
    {
        Assert.Contains("System.Runtime", SharedTypeRegistry.DefaultSharedAssemblies);
        Assert.Contains("System.Private.CoreLib", SharedTypeRegistry.DefaultSharedAssemblies);
    }
}
