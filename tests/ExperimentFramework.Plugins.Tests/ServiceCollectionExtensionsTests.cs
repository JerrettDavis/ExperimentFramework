using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Configuration;
using ExperimentFramework.Plugins.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Plugins.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddExperimentPlugins_RegistersRequiredServices()
    {
        var services = new ServiceCollection();

        services.AddExperimentPlugins();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IPluginLoader>());
        Assert.NotNull(provider.GetService<IPluginManager>());
        Assert.NotNull(provider.GetService<IOptions<PluginConfigurationOptions>>());
    }

    [Fact]
    public void AddExperimentPlugins_WithConfiguration_SetsOptions()
    {
        var services = new ServiceCollection();

        services.AddExperimentPlugins(opts =>
        {
            opts.DiscoveryPaths.Add("./plugins");
            opts.EnableHotReload = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PluginConfigurationOptions>>();

        Assert.Contains("./plugins", options.Value.DiscoveryPaths);
        Assert.True(options.Value.EnableHotReload);
    }

    [Fact]
    public void AddExperimentPluginsWithHotReload_EnablesHotReload()
    {
        var services = new ServiceCollection();

        services.AddExperimentPluginsWithHotReload();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PluginConfigurationOptions>>();

        Assert.True(options.Value.EnableHotReload);
    }

    [Fact]
    public void AddExperimentPluginsWithHotReload_WithConfiguration()
    {
        var services = new ServiceCollection();

        services.AddExperimentPluginsWithHotReload(opts =>
        {
            opts.DiscoveryPaths.Add("./plugins");
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PluginConfigurationOptions>>();

        Assert.True(options.Value.EnableHotReload);
        Assert.Contains("./plugins", options.Value.DiscoveryPaths);
    }

    [Fact]
    public void AddExperimentPluginsFromConfiguration_SetsFromConfig()
    {
        var services = new ServiceCollection();
        var config = new PluginsConfig
        {
            Discovery = new PluginDiscoveryConfig
            {
                Paths = ["./plugins", "./external"]
            },
            Defaults = new PluginDefaultsConfig
            {
                IsolationMode = "full",
                SharedAssemblies = ["Custom.Assembly"]
            },
            HotReload = new PluginHotReloadConfig
            {
                Enabled = true,
                DebounceMs = 250
            }
        };

        services.AddExperimentPluginsFromConfiguration(config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PluginConfigurationOptions>>();

        Assert.Equal(2, options.Value.DiscoveryPaths.Count);
        Assert.Equal(PluginIsolationMode.Full, options.Value.DefaultIsolationMode);
        Assert.Single(options.Value.DefaultSharedAssemblies);
        Assert.True(options.Value.EnableHotReload);
        Assert.Equal(250, options.Value.HotReloadDebounceMs);
    }

    [Fact]
    public void AddExperimentPlugins_ThrowsOnNullServices()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddExperimentPlugins());
    }

    [Fact]
    public void AddExperimentPluginsFromConfiguration_ThrowsOnNullConfig()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddExperimentPluginsFromConfiguration(null!));
    }

    [Fact]
    public void AddExperimentPluginsFromConfiguration_WithoutHotReload_DoesNotEnableHotReload()
    {
        var services = new ServiceCollection();
        var config = new PluginsConfig
        {
            Discovery = new PluginDiscoveryConfig
            {
                Paths = ["./plugins"]
            },
            HotReload = new PluginHotReloadConfig
            {
                Enabled = false
            }
        };

        services.AddExperimentPluginsFromConfiguration(config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PluginConfigurationOptions>>();

        Assert.False(options.Value.EnableHotReload);
    }

    [Fact]
    public void AddPluginTypeResolver_ThrowsOnNullServices()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddPluginTypeResolver());
    }

    [Fact]
    public void AddPluginTypeResolver_WithNoTypeResolver_ThrowsInvalidOperation()
    {
        var services = new ServiceCollection();
        services.AddExperimentPlugins();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddPluginTypeResolver());

        Assert.Contains("ITypeResolver", ex.Message);
    }

    [Fact]
    public void AddPluginTypeResolver_WithNoPluginManager_ThrowsInvalidOperation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITypeResolver, TestTypeResolver>();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddPluginTypeResolver());

        Assert.Contains("IPluginManager", ex.Message);
    }

    [Fact]
    public void AddPluginTypeResolver_DecoratesExistingTypeResolver()
    {
        var services = new ServiceCollection();
        services.AddExperimentPlugins();

        // Register a base type resolver
        services.AddSingleton<ITypeResolver, TestTypeResolver>();

        services.AddPluginTypeResolver();

        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<ITypeResolver>();

        // Should be decorated with PluginTypeResolver
        Assert.IsType<PluginTypeResolver>(resolver);
    }

    #region Idempotency Tests

    [Fact]
    public void AddExperimentPlugins_CalledMultipleTimes_IsIdempotent()
    {
        var services = new ServiceCollection();

        // Call multiple times
        services.AddExperimentPlugins();
        services.AddExperimentPlugins();
        services.AddExperimentPlugins();

        var provider = services.BuildServiceProvider();

        // Should only have one IPluginManager registered
        var managers = provider.GetServices<IPluginManager>().ToList();
        Assert.Single(managers);

        // Should only have one IPluginLoader registered
        var loaders = provider.GetServices<IPluginLoader>().ToList();
        Assert.Single(loaders);
    }

    [Fact]
    public void AddExperimentPlugins_CalledMultipleTimes_AppliesConfigurationEachTime()
    {
        var services = new ServiceCollection();

        services.AddExperimentPlugins(opts => opts.DiscoveryPaths.Add("./first"));
        services.AddExperimentPlugins(opts => opts.DiscoveryPaths.Add("./second"));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PluginConfigurationOptions>>();

        // Both paths should be present
        Assert.Contains("./first", options.Value.DiscoveryPaths);
        Assert.Contains("./second", options.Value.DiscoveryPaths);
    }

    [Fact]
    public void AddExperimentPluginsWithHotReload_CalledMultipleTimes_IsIdempotent()
    {
        var services = new ServiceCollection();

        services.AddExperimentPluginsWithHotReload();
        services.AddExperimentPluginsWithHotReload();

        var provider = services.BuildServiceProvider();

        // Should only have one IPluginManager
        var managers = provider.GetServices<IPluginManager>().ToList();
        Assert.Single(managers);
    }

    [Fact]
    public void AddExperimentPluginsFromConfiguration_CalledMultipleTimes_IsIdempotent()
    {
        var services = new ServiceCollection();
        var config1 = new PluginsConfig { Discovery = new PluginDiscoveryConfig { Paths = ["./one"] } };
        var config2 = new PluginsConfig { Discovery = new PluginDiscoveryConfig { Paths = ["./two"] } };

        services.AddExperimentPluginsFromConfiguration(config1);
        services.AddExperimentPluginsFromConfiguration(config2);

        var provider = services.BuildServiceProvider();

        // Should only have one IPluginManager
        var managers = provider.GetServices<IPluginManager>().ToList();
        Assert.Single(managers);
    }

    #endregion

    #region Validator Registration Tests

    [Fact]
    public void AddExperimentPlugins_RegistersConfigurationValidator()
    {
        var services = new ServiceCollection();

        services.AddExperimentPlugins();

        var provider = services.BuildServiceProvider();

        // The validator is registered as IValidateOptions<PluginConfigurationOptions>
        var validators = provider.GetServices<IValidateOptions<PluginConfigurationOptions>>().ToList();
        Assert.True(validators.Count > 0, "Expected at least one validator to be registered");
    }

    #endregion

    private class TestTypeResolver : ITypeResolver
    {
        public Type Resolve(string typeName) => throw new NotImplementedException();
        public bool TryResolve(string typeName, out Type? type) { type = null; return false; }
        public void RegisterAlias(string alias, Type type) { }
    }
}

// Note: ServiceCollectionDecoratorExtensions is internal, so we test it indirectly
// through AddPluginTypeResolver which uses it internally
