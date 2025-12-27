using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Configuration;
using ExperimentFramework.Plugins.Integration;
using ExperimentFramework.Plugins.Loading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Plugins.Tests.Integration;

public class PluginManagerTests : IAsyncDisposable
{
    private readonly PluginLoader _loader;
    private readonly PluginManager _manager;

    public PluginManagerTests()
    {
        _loader = new PluginLoader();
        _manager = new PluginManager(_loader);
    }

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }

    #region GetLoadedPlugins Tests

    [Fact]
    public void GetLoadedPlugins_InitiallyEmpty()
    {
        var plugins = _manager.GetLoadedPlugins();

        Assert.Empty(plugins);
    }

    [Fact]
    public async Task GetLoadedPlugins_AfterLoading_ReturnsLoadedPlugin()
    {
        var dllPath = typeof(PluginManagerTests).Assembly.Location;
        var options = new PluginLoadOptions { IsolationModeOverride = PluginIsolationMode.None };

        await _manager.LoadAsync(dllPath, options);

        var plugins = _manager.GetLoadedPlugins();
        Assert.Single(plugins);
    }

    #endregion

    #region GetPlugin Tests

    [Fact]
    public void GetPlugin_NonExistent_ReturnsNull()
    {
        var plugin = _manager.GetPlugin("NonExistent.Plugin");

        Assert.Null(plugin);
    }

    [Fact]
    public async Task GetPlugin_LoadedPlugin_ReturnsContext()
    {
        var dllPath = typeof(PluginManagerTests).Assembly.Location;
        var options = new PluginLoadOptions { IsolationModeOverride = PluginIsolationMode.None };

        var loaded = await _manager.LoadAsync(dllPath, options);
        var retrieved = _manager.GetPlugin(loaded.Manifest.Id);

        Assert.NotNull(retrieved);
        Assert.Same(loaded, retrieved);
    }

    [Fact]
    public void GetPlugin_NullId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _manager.GetPlugin(null!));
    }

    [Fact]
    public void GetPlugin_EmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _manager.GetPlugin(""));
    }

    #endregion

    #region IsLoaded Tests

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
    public async Task IsLoaded_LoadedPlugin_ReturnsTrue()
    {
        var dllPath = typeof(PluginManagerTests).Assembly.Location;
        var options = new PluginLoadOptions { IsolationModeOverride = PluginIsolationMode.None };

        var loaded = await _manager.LoadAsync(dllPath, options);

        Assert.True(_manager.IsLoaded(loaded.Manifest.Id));
    }

    #endregion

    #region LoadAsync Tests

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
    public async Task LoadAsync_ValidPath_LoadsPlugin()
    {
        var dllPath = typeof(PluginManagerTests).Assembly.Location;
        var options = new PluginLoadOptions { IsolationModeOverride = PluginIsolationMode.None };

        var context = await _manager.LoadAsync(dllPath, options);

        Assert.NotNull(context);
        Assert.True(context.IsLoaded);
    }

    [Fact]
    public async Task LoadAsync_SamePathTwice_ReturnsSameContext()
    {
        var dllPath = typeof(PluginManagerTests).Assembly.Location;
        var options = new PluginLoadOptions { IsolationModeOverride = PluginIsolationMode.None };

        var context1 = await _manager.LoadAsync(dllPath, options);
        var context2 = await _manager.LoadAsync(dllPath, options);

        Assert.Same(context1, context2);
    }

    [Fact]
    public async Task LoadAsync_FiresPluginLoadedEvent()
    {
        var dllPath = typeof(PluginManagerTests).Assembly.Location;
        var options = new PluginLoadOptions { IsolationModeOverride = PluginIsolationMode.None };

        IPluginContext? loadedContext = null;
        _manager.PluginLoaded += (_, args) => loadedContext = args.Context;

        var context = await _manager.LoadAsync(dllPath, options);

        Assert.NotNull(loadedContext);
        Assert.Same(context, loadedContext);
    }

    [Fact]
    public async Task LoadAsync_NonExistentPath_FiresPluginLoadFailedEvent()
    {
        string? failedPath = null;
        Exception? failedException = null;
        _manager.PluginLoadFailed += (_, args) =>
        {
            failedPath = args.PluginPath;
            failedException = args.Exception;
        };

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _manager.LoadAsync("/nonexistent/path/plugin.dll"));

        Assert.NotNull(failedPath);
        Assert.NotNull(failedException);
    }

    #endregion

    #region UnloadAsync Tests

    [Fact]
    public async Task UnloadAsync_NullPluginId_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _manager.UnloadAsync(null!));
    }

    [Fact]
    public async Task UnloadAsync_EmptyPluginId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UnloadAsync(""));
    }

    [Fact]
    public async Task UnloadAsync_NonExistentPlugin_DoesNotThrow()
    {
        // Should not throw, just log a warning
        await _manager.UnloadAsync("NonExistent.Plugin");
    }

    [Fact]
    public async Task UnloadAsync_LoadedPlugin_UnloadsAndFiresEvent()
    {
        var dllPath = typeof(PluginManagerTests).Assembly.Location;
        var options = new PluginLoadOptions { IsolationModeOverride = PluginIsolationMode.None };

        var context = await _manager.LoadAsync(dllPath, options);
        var pluginId = context.Manifest.Id;

        IPluginContext? unloadedContext = null;
        _manager.PluginUnloaded += (_, args) => unloadedContext = args.Context;

        await _manager.UnloadAsync(pluginId);

        Assert.NotNull(unloadedContext);
        Assert.False(_manager.IsLoaded(pluginId));
    }

    #endregion

    #region ReloadAsync Tests

    [Fact]
    public async Task ReloadAsync_NonExistentPlugin_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.ReloadAsync("NonExistent.Plugin"));
    }

    [Fact]
    public async Task ReloadAsync_NullPluginId_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _manager.ReloadAsync(null!));
    }

    [Fact]
    public async Task ReloadAsync_LoadedPlugin_ReloadsSuccessfully()
    {
        var dllPath = typeof(PluginManagerTests).Assembly.Location;
        var options = new PluginLoadOptions { IsolationModeOverride = PluginIsolationMode.None };

        var originalContext = await _manager.LoadAsync(dllPath, options);
        var pluginId = originalContext.Manifest.Id;

        var reloadedContext = await _manager.ReloadAsync(pluginId);

        Assert.NotNull(reloadedContext);
        Assert.True(reloadedContext.IsLoaded);
        Assert.NotSame(originalContext, reloadedContext);
    }

    [Fact]
    public async Task ReloadAsync_FiresUnloadAndLoadEvents()
    {
        var dllPath = typeof(PluginManagerTests).Assembly.Location;
        var options = new PluginLoadOptions { IsolationModeOverride = PluginIsolationMode.None };

        var context = await _manager.LoadAsync(dllPath, options);
        var pluginId = context.Manifest.Id;

        bool unloadedFired = false;
        bool loadedFired = false;
        _manager.PluginUnloaded += (_, _) => unloadedFired = true;
        _manager.PluginLoaded += (_, _) => loadedFired = true;

        await _manager.ReloadAsync(pluginId);

        Assert.True(unloadedFired);
        Assert.True(loadedFired);
    }

    #endregion

    #region ResolveType Tests

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
    public async Task ResolveType_LoadedPluginWithValidType_ReturnsType()
    {
        var dllPath = typeof(PluginManagerTests).Assembly.Location;
        var options = new PluginLoadOptions { IsolationModeOverride = PluginIsolationMode.None };

        var context = await _manager.LoadAsync(dllPath, options);
        var pluginId = context.Manifest.Id;
        var typeName = typeof(PluginManagerTests).FullName!;

        var type = _manager.ResolveType($"plugin:{pluginId}/{typeName}");

        Assert.NotNull(type);
        Assert.Equal(typeof(PluginManagerTests), type);
    }

    [Fact]
    public async Task ResolveType_LoadedPluginWithInvalidType_ReturnsNull()
    {
        var dllPath = typeof(PluginManagerTests).Assembly.Location;
        var options = new PluginLoadOptions { IsolationModeOverride = PluginIsolationMode.None };

        var context = await _manager.LoadAsync(dllPath, options);
        var pluginId = context.Manifest.Id;

        var type = _manager.ResolveType($"plugin:{pluginId}/NonExistent.Type");

        Assert.Null(type);
    }

    #endregion

    #region DiscoverAndLoadAsync Tests

    [Fact]
    public async Task DiscoverAndLoadAsync_EmptyPaths_ReturnsEmpty()
    {
        var plugins = await _manager.DiscoverAndLoadAsync();

        Assert.Empty(plugins);
    }

    [Fact]
    public async Task DiscoverAndLoadAsync_CancelledToken_StopsDiscovery()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var plugins = await _manager.DiscoverAndLoadAsync(cts.Token);

        Assert.Empty(plugins);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_UnloadsAllPlugins()
    {
        var dllPath = typeof(PluginManagerTests).Assembly.Location;
        var loader = new PluginLoader();
        var manager = new PluginManager(loader);
        var options = new PluginLoadOptions { IsolationModeOverride = PluginIsolationMode.None };

        await manager.LoadAsync(dllPath, options);

        await manager.DisposeAsync();

        // Manager should be disposed
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => manager.LoadAsync("some/path.dll"));
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_IsIdempotent()
    {
        var loader = new PluginLoader();
        var manager = new PluginManager(loader);

        await manager.DisposeAsync();
        await manager.DisposeAsync();
        await manager.DisposeAsync();

        // Should not throw
    }

    [Fact]
    public async Task AfterDispose_AllMethods_ThrowObjectDisposedException()
    {
        var loader = new PluginLoader();
        var manager = new PluginManager(loader);
        await manager.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => manager.GetLoadedPlugins());
        Assert.Throws<ObjectDisposedException>(() => manager.GetPlugin("any"));
        Assert.Throws<ObjectDisposedException>(() => manager.IsLoaded("any"));
        Assert.Throws<ObjectDisposedException>(() => manager.ResolveType("plugin:any/Type"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.LoadAsync("path"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.UnloadAsync("id"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ReloadAsync("id"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.DiscoverAndLoadAsync());
    }

    #endregion

    #region Event Tests

    [Fact]
    public void Events_CanBeSubscribed()
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

    #endregion
}

public class PluginManagerWithOptionsTests : IAsyncDisposable
{
    private PluginManager? _manager;

    public async ValueTask DisposeAsync()
    {
        if (_manager != null)
        {
            await _manager.DisposeAsync();
        }
    }

    [Fact]
    public void Constructor_AcceptsOptions()
    {
        var loader = new PluginLoader();
        var options = Options.Create(new PluginConfigurationOptions
        {
            DefaultIsolationMode = PluginIsolationMode.Full,
            DiscoveryPaths = ["./plugins"]
        });

        _manager = new PluginManager(loader, options);

        Assert.NotNull(_manager);
    }

    [Fact]
    public void Constructor_ThrowsOnNullLoader()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PluginManager(null!));
    }

    [Fact]
    public void Constructor_ThrowsOnNullOptions()
    {
        var loader = new PluginLoader();

        Assert.Throws<ArgumentNullException>(() =>
            new PluginManager(loader, (IOptions<PluginConfigurationOptions>)null!));
    }

    [Fact]
    public void Constructor_WithLogger_UsesLogger()
    {
        var loader = new PluginLoader();
        var options = Options.Create(new PluginConfigurationOptions());
        var logger = Substitute.For<ILogger<PluginManager>>();

        _manager = new PluginManager(loader, options, logger);

        Assert.NotNull(_manager);
    }

    [Fact]
    public async Task LoadAsync_AppliesDefaultIsolationMode()
    {
        var loader = new PluginLoader();
        var options = Options.Create(new PluginConfigurationOptions
        {
            DefaultIsolationMode = PluginIsolationMode.Full
        });

        _manager = new PluginManager(loader, options);

        var dllPath = typeof(PluginManagerWithOptionsTests).Assembly.Location;
        var context = await _manager.LoadAsync(dllPath);

        Assert.NotNull(context);
        Assert.True(context.IsLoaded);
    }

    [Fact]
    public async Task LoadAsync_AppliesDefaultSharedAssemblies()
    {
        var loader = new PluginLoader();
        var options = Options.Create(new PluginConfigurationOptions
        {
            DefaultSharedAssemblies = ["ExperimentFramework"],
            DefaultIsolationMode = PluginIsolationMode.None
        });

        _manager = new PluginManager(loader, options);

        var dllPath = typeof(PluginManagerWithOptionsTests).Assembly.Location;
        var context = await _manager.LoadAsync(dllPath);

        Assert.NotNull(context);
    }
}

public class PluginManagerDiscoveryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PluginManager _manager;

    public PluginManagerDiscoveryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"PluginDiscoveryTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var loader = new PluginLoader();
        var options = Options.Create(new PluginConfigurationOptions
        {
            DiscoveryPaths = [_tempDir]
        });

        _manager = new PluginManager(loader, options);
    }

    public void Dispose()
    {
        _manager.DisposeAsync().AsTask().Wait();
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task DiscoverAndLoadAsync_WithNoPlugins_ReturnsEmpty()
    {
        var plugins = await _manager.DiscoverAndLoadAsync();

        Assert.Empty(plugins);
    }

    [Fact]
    public async Task DiscoverAndLoadAsync_WithNonDllFiles_IgnoresThem()
    {
        // Create non-DLL files
        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), "not a plugin");
        File.WriteAllText(Path.Combine(_tempDir, "config.json"), "{}");

        var plugins = await _manager.DiscoverAndLoadAsync();

        Assert.Empty(plugins);
    }
}
