using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Configuration;

namespace ExperimentFramework.Plugins.Tests.Configuration;

public class PluginConfigurationOptionsTests
{
    [Fact]
    public void DefaultValues()
    {
        var options = new PluginConfigurationOptions();

        Assert.Empty(options.DiscoveryPaths);
        Assert.Equal(PluginIsolationMode.Shared, options.DefaultIsolationMode);
        Assert.Empty(options.DefaultSharedAssemblies);
        Assert.False(options.EnableHotReload);
        Assert.Equal(500, options.HotReloadDebounceMs);
        Assert.True(options.AutoLoadOnStartup);
        Assert.False(options.ForceIsolation);
        Assert.True(options.EnableUnloading);
        Assert.False(options.StrictManifestValidation);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        var options = new PluginConfigurationOptions
        {
            DiscoveryPaths = ["./plugins"],
            DefaultIsolationMode = PluginIsolationMode.Full,
            DefaultSharedAssemblies = ["Custom.Assembly"],
            EnableHotReload = true,
            HotReloadDebounceMs = 1000,
            AutoLoadOnStartup = false,
            ForceIsolation = true,
            EnableUnloading = false,
            StrictManifestValidation = true
        };

        Assert.Single(options.DiscoveryPaths);
        Assert.Equal(PluginIsolationMode.Full, options.DefaultIsolationMode);
        Assert.Single(options.DefaultSharedAssemblies);
        Assert.True(options.EnableHotReload);
        Assert.Equal(1000, options.HotReloadDebounceMs);
        Assert.False(options.AutoLoadOnStartup);
        Assert.True(options.ForceIsolation);
        Assert.False(options.EnableUnloading);
        Assert.True(options.StrictManifestValidation);
    }
}

public class PluginsConfigTests
{
    [Fact]
    public void ToOptions_EmptyConfig_ReturnsDefaults()
    {
        var config = new PluginsConfig();

        var options = config.ToOptions();

        Assert.Empty(options.DiscoveryPaths);
        Assert.Equal(PluginIsolationMode.Shared, options.DefaultIsolationMode);
        Assert.Empty(options.DefaultSharedAssemblies);
        Assert.False(options.EnableHotReload);
        Assert.Equal(500, options.HotReloadDebounceMs);
    }

    [Fact]
    public void ToOptions_WithDiscovery_SetsDiscoveryPaths()
    {
        var config = new PluginsConfig
        {
            Discovery = new PluginDiscoveryConfig
            {
                Paths = ["./plugins", "./external/**/*.dll"]
            }
        };

        var options = config.ToOptions();

        Assert.Equal(2, options.DiscoveryPaths.Count);
        Assert.Contains("./plugins", options.DiscoveryPaths);
        Assert.Contains("./external/**/*.dll", options.DiscoveryPaths);
    }

    [Fact]
    public void ToOptions_WithDefaults_SetsIsolationMode()
    {
        var config = new PluginsConfig
        {
            Defaults = new PluginDefaultsConfig
            {
                IsolationMode = "full"
            }
        };

        var options = config.ToOptions();

        Assert.Equal(PluginIsolationMode.Full, options.DefaultIsolationMode);
    }

    [Fact]
    public void ToOptions_NoneIsolationMode()
    {
        var config = new PluginsConfig
        {
            Defaults = new PluginDefaultsConfig
            {
                IsolationMode = "none"
            }
        };

        var options = config.ToOptions();

        Assert.Equal(PluginIsolationMode.None, options.DefaultIsolationMode);
    }

    [Fact]
    public void ToOptions_SharedIsolationMode()
    {
        var config = new PluginsConfig
        {
            Defaults = new PluginDefaultsConfig
            {
                IsolationMode = "shared"
            }
        };

        var options = config.ToOptions();

        Assert.Equal(PluginIsolationMode.Shared, options.DefaultIsolationMode);
    }

    [Fact]
    public void ToOptions_WithSharedAssemblies()
    {
        var config = new PluginsConfig
        {
            Defaults = new PluginDefaultsConfig
            {
                SharedAssemblies = ["Custom.Assembly", "Another.Assembly"]
            }
        };

        var options = config.ToOptions();

        Assert.Equal(2, options.DefaultSharedAssemblies.Count);
    }

    [Fact]
    public void ToOptions_WithHotReload()
    {
        var config = new PluginsConfig
        {
            HotReload = new PluginHotReloadConfig
            {
                Enabled = true,
                DebounceMs = 1000
            }
        };

        var options = config.ToOptions();

        Assert.True(options.EnableHotReload);
        Assert.Equal(1000, options.HotReloadDebounceMs);
    }

    [Fact]
    public void ToOptions_HotReloadDisabled()
    {
        var config = new PluginsConfig
        {
            HotReload = new PluginHotReloadConfig
            {
                Enabled = false
            }
        };

        var options = config.ToOptions();

        Assert.False(options.EnableHotReload);
    }

    [Fact]
    public void ToOptions_FullConfig()
    {
        var config = new PluginsConfig
        {
            Discovery = new PluginDiscoveryConfig
            {
                Paths = ["./plugins"]
            },
            Defaults = new PluginDefaultsConfig
            {
                IsolationMode = "shared",
                SharedAssemblies = ["Custom"]
            },
            HotReload = new PluginHotReloadConfig
            {
                Enabled = true,
                DebounceMs = 250
            }
        };

        var options = config.ToOptions();

        Assert.Single(options.DiscoveryPaths);
        Assert.Equal(PluginIsolationMode.Shared, options.DefaultIsolationMode);
        Assert.Single(options.DefaultSharedAssemblies);
        Assert.True(options.EnableHotReload);
        Assert.Equal(250, options.HotReloadDebounceMs);
    }
}
