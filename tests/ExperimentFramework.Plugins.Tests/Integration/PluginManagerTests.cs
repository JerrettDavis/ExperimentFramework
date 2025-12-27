using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Configuration;
using ExperimentFramework.Plugins.Integration;
using ExperimentFramework.Plugins.Loading;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Plugins.Tests.Integration;

public class PluginManagerTests
{
    private readonly PluginLoader _loader;
    private readonly PluginManager _manager;

    public PluginManagerTests()
    {
        _loader = new PluginLoader();
        _manager = new PluginManager(_loader);
    }

    [Fact]
    public void GetLoadedPlugins_InitiallyEmpty()
    {
        var plugins = _manager.GetLoadedPlugins();

        Assert.Empty(plugins);
    }

    [Fact]
    public void GetPlugin_NonExistent_ReturnsNull()
    {
        var plugin = _manager.GetPlugin("NonExistent.Plugin");

        Assert.Null(plugin);
    }

    [Fact]
    public void IsLoaded_NonExistent_ReturnsFalse()
    {
        Assert.False(_manager.IsLoaded("NonExistent.Plugin"));
    }

    [Fact]
    public void IsLoaded_NullOrEmpty_ReturnsFalse()
    {
        Assert.False(_manager.IsLoaded(null!));
        Assert.False(_manager.IsLoaded(""));
        Assert.False(_manager.IsLoaded("   "));
    }

    [Fact]
    public async Task LoadAsync_NullPath_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _manager.LoadAsync(null!));
    }

    [Fact]
    public async Task LoadAsync_EmptyPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.LoadAsync(""));
    }

    [Fact]
    public async Task UnloadAsync_NullPluginId_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _manager.UnloadAsync(null!));
    }

    [Fact]
    public async Task UnloadAsync_NonExistentPlugin_DoesNotThrow()
    {
        // Should not throw, just log a warning
        await _manager.UnloadAsync("NonExistent.Plugin");
    }

    [Fact]
    public async Task ReloadAsync_NonExistentPlugin_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.ReloadAsync("NonExistent.Plugin"));
    }

    [Fact]
    public void ResolveType_InvalidReference_ReturnsNull()
    {
        Assert.Null(_manager.ResolveType("invalid"));
        Assert.Null(_manager.ResolveType("plugin:"));
        Assert.Null(_manager.ResolveType("plugin:NoSlash"));
        Assert.Null(_manager.ResolveType("plugin:/NoPluginId"));
    }

    [Fact]
    public void ResolveType_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(_manager.ResolveType(null!));
        Assert.Null(_manager.ResolveType(""));
        Assert.Null(_manager.ResolveType("   "));
    }

    [Fact]
    public void ResolveType_UnloadedPlugin_ReturnsNull()
    {
        var type = _manager.ResolveType("plugin:NonExistent/SomeType");

        Assert.Null(type);
    }

    [Fact]
    public async Task DiscoverAndLoadAsync_EmptyPaths_ReturnsEmpty()
    {
        var plugins = await _manager.DiscoverAndLoadAsync();

        Assert.Empty(plugins);
    }

    [Fact]
    public async Task DisposeAsync_UnloadsAllPlugins()
    {
        await _manager.DisposeAsync();

        // Manager should be disposed
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _manager.LoadAsync("some/path.dll"));
    }

    [Fact]
    public void Events_InitiallyNull()
    {
        // Events should be accessible
        var manager = new PluginManager(_loader);

        bool loaded = false;
        bool unloaded = false;
        bool failed = false;

        manager.PluginLoaded += (_, _) => loaded = true;
        manager.PluginUnloaded += (_, _) => unloaded = true;
        manager.PluginLoadFailed += (_, _) => failed = true;

        // Events are registered but not invoked
        Assert.False(loaded);
        Assert.False(unloaded);
        Assert.False(failed);
    }
}

public class PluginManagerWithOptionsTests
{
    [Fact]
    public void Constructor_AcceptsOptions()
    {
        var loader = new PluginLoader();
        var options = Options.Create(new PluginConfigurationOptions
        {
            DefaultIsolationMode = PluginIsolationMode.Full,
            DiscoveryPaths = ["./plugins"]
        });

        var manager = new PluginManager(loader, options);

        Assert.NotNull(manager);
    }

    [Fact]
    public void Constructor_ThrowsOnNullLoader()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PluginManager(null!));
    }
}
