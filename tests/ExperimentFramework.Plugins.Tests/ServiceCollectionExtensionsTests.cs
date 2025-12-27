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

        Assert.Throws<InvalidOperationException>(() =>
            services.AddPluginTypeResolver());
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

    private class TestTypeResolver : ITypeResolver
    {
        public Type Resolve(string typeName) => throw new NotImplementedException();
        public bool TryResolve(string typeName, out Type? type) { type = null; return false; }
        public void RegisterAlias(string alias, Type type) { }
    }
}

// Note: ServiceCollectionDecoratorExtensions is internal, so we test it indirectly
// through AddPluginTypeResolver which uses it internally
