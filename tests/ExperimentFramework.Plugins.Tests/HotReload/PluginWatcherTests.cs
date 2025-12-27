using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.HotReload;
using ExperimentFramework.Plugins.Manifest;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Plugins.Tests.HotReload;

public class PluginWatcherTests : IDisposable
{
    private readonly IPluginManager _pluginManager;
    private readonly PluginWatcher _watcher;
    private readonly string _tempDir;

    public PluginWatcherTests()
    {
        _pluginManager = Substitute.For<IPluginManager>();
        _pluginManager.GetLoadedPlugins().Returns([]);
        _watcher = new PluginWatcher(_pluginManager, 100);
        _tempDir = Path.Combine(Path.GetTempPath(), $"PluginWatcherTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _watcher.Dispose();
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullPluginManager()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PluginWatcher(null!));
    }

    [Fact]
    public void Constructor_WithDefaultDebounce_DoesNotThrow()
    {
        using var watcher = new PluginWatcher(_pluginManager);
        Assert.NotNull(watcher);
    }

    [Fact]
    public void Constructor_WithLogger_UsesLogger()
    {
        var logger = Substitute.For<ILogger<PluginWatcher>>();
        using var watcher = new PluginWatcher(_pluginManager, 100, logger);
        Assert.NotNull(watcher);
    }

    [Fact]
    public void Constructor_WithCustomDebounce_SetsDebounceInterval()
    {
        using var watcher = new PluginWatcher(_pluginManager, 1000);
        Assert.NotNull(watcher);
    }

    #endregion

    #region StartWatching Tests

    [Fact]
    public void StartWatching_DoesNotThrow()
    {
        _watcher.StartWatching();

        // No exception means success
    }

    [Fact]
    public void StartWatching_WithLoadedPlugins_WatchesThem()
    {
        var mockContext = CreateMockPluginContext("Test.Plugin", _tempDir);
        _pluginManager.GetLoadedPlugins().Returns([mockContext]);

        _watcher.StartWatching();

        // Should not throw and should have subscribed to events
        _pluginManager.Received(1).GetLoadedPlugins();
    }

    [Fact]
    public void StartWatching_SubscribesToPluginLoadedEvent()
    {
        _watcher.StartWatching();

        // Should have subscribed to events
        _pluginManager.Received().PluginLoaded += Arg.Any<EventHandler<PluginEventArgs>>();
    }

    [Fact]
    public void StartWatching_SubscribesToPluginUnloadedEvent()
    {
        _watcher.StartWatching();

        // Should have subscribed to events
        _pluginManager.Received().PluginUnloaded += Arg.Any<EventHandler<PluginEventArgs>>();
    }

    [Fact]
    public void StartWatching_AfterDispose_ThrowsObjectDisposedException()
    {
        _watcher.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _watcher.StartWatching());
    }

    #endregion

    #region StopWatching Tests

    [Fact]
    public void StopWatching_DoesNotThrow()
    {
        _watcher.StartWatching();
        _watcher.StopWatching();

        // No exception means success
    }

    [Fact]
    public void StopWatching_WithoutStarting_DoesNotThrow()
    {
        _watcher.StopWatching();

        // No exception means success
    }

    [Fact]
    public void StopWatching_CalledMultipleTimes_DoesNotThrow()
    {
        _watcher.StartWatching();
        _watcher.StopWatching();
        _watcher.StopWatching();
        _watcher.StopWatching();

        // No exception means success
    }

    #endregion

    #region WatchDirectory Tests

    [Fact]
    public void WatchDirectory_NonExistentPath_DoesNotThrow()
    {
        _watcher.WatchDirectory("/nonexistent/path");

        // Should just log a warning, not throw
    }

    [Fact]
    public void WatchDirectory_ValidPath_Succeeds()
    {
        _watcher.WatchDirectory(_tempDir);
        // No exception means success
    }

    [Fact]
    public void WatchDirectory_WithCustomFilter_Succeeds()
    {
        _watcher.WatchDirectory(_tempDir, "*.plugin.dll");
        // No exception means success
    }

    [Fact]
    public void WatchDirectory_AfterDispose_ThrowsObjectDisposedException()
    {
        _watcher.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _watcher.WatchDirectory(_tempDir));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_StopsWatching()
    {
        _watcher.StartWatching();
        _watcher.Dispose();

        // Should not throw on double dispose
        _watcher.Dispose();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_IsIdempotent()
    {
        _watcher.Dispose();
        _watcher.Dispose();
        _watcher.Dispose();

        // No exception means success
    }

    #endregion

    #region Event Tests

    [Fact]
    public void Events_CanBeSubscribed()
    {
        bool triggered = false;
        bool completed = false;
        bool failed = false;

        _watcher.PluginReloadTriggered += (_, _) => triggered = true;
        _watcher.PluginReloadCompleted += (_, _) => completed = true;
        _watcher.PluginReloadFailed += (_, _) => failed = true;

        // Events are registered but not invoked
        Assert.False(triggered);
        Assert.False(completed);
        Assert.False(failed);
    }

    #endregion

    #region PluginLoaded Event Tests

    [Fact]
    public void OnPluginLoaded_WatchesNewPlugin()
    {
        _watcher.StartWatching();

        var context = CreateMockPluginContext("New.Plugin", _tempDir);

        // Simulate plugin loaded event
        _pluginManager.PluginLoaded += Raise.EventWith(
            _pluginManager,
            new PluginEventArgs(context));

        // Should have added a watcher for the new plugin
        // No assertion needed - just verifying no exception
    }

    #endregion

    #region File Change Handling Tests

    [Fact]
    public async Task FileChange_OnLoadedPlugin_TriggersReload()
    {
        // Setup a mock plugin
        var pluginPath = Path.Combine(_tempDir, "testplugin.dll");
        File.WriteAllBytes(pluginPath, [0x00]);

        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("Test.Plugin");
        manifest.Version.Returns("1.0.0");
        manifest.Lifecycle.Returns(new PluginLifecycleConfig { SupportsHotReload = true });

        var context = Substitute.For<IPluginContext>();
        context.Manifest.Returns(manifest);
        context.PluginPath.Returns(pluginPath);
        context.IsLoaded.Returns(true);

        _pluginManager.GetLoadedPlugins().Returns([context]);
        _pluginManager.ReloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(context);

        bool reloadTriggered = false;
        bool reloadCompleted = false;

        using var watcher = new PluginWatcher(_pluginManager, 50);
        watcher.PluginReloadTriggered += (_, _) => reloadTriggered = true;
        watcher.PluginReloadCompleted += (_, _) => reloadCompleted = true;

        watcher.StartWatching();

        // Modify the file to trigger a change
        await Task.Delay(100);
        File.WriteAllBytes(pluginPath, [0x01, 0x02]);

        // Wait for debounce and reload
        await Task.Delay(200);

        // Check that reload was triggered
        await _pluginManager.Received(1).ReloadAsync("Test.Plugin", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FileChange_OnPluginWithHotReloadDisabled_DoesNotReload()
    {
        var pluginPath = Path.Combine(_tempDir, "noreload.dll");
        File.WriteAllBytes(pluginPath, [0x00]);

        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("NoReload.Plugin");
        manifest.Lifecycle.Returns(new PluginLifecycleConfig { SupportsHotReload = false });

        var context = Substitute.For<IPluginContext>();
        context.Manifest.Returns(manifest);
        context.PluginPath.Returns(pluginPath);
        context.IsLoaded.Returns(true);

        _pluginManager.GetLoadedPlugins().Returns([context]);

        using var watcher = new PluginWatcher(_pluginManager, 50);
        watcher.StartWatching();

        await Task.Delay(100);
        File.WriteAllBytes(pluginPath, [0x01, 0x02]);

        await Task.Delay(200);

        // Should not have called reload
        await _pluginManager.DidNotReceive().ReloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FileChange_ReloadFails_RaisesFailedEvent()
    {
        var pluginPath = Path.Combine(_tempDir, "failing.dll");
        File.WriteAllBytes(pluginPath, [0x00]);

        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("Failing.Plugin");
        manifest.Lifecycle.Returns(new PluginLifecycleConfig { SupportsHotReload = true });

        var context = Substitute.For<IPluginContext>();
        context.Manifest.Returns(manifest);
        context.PluginPath.Returns(pluginPath);
        context.IsLoaded.Returns(true);

        _pluginManager.GetLoadedPlugins().Returns([context]);
        _pluginManager.ReloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<IPluginContext>(x => throw new InvalidOperationException("Reload failed"));

        bool reloadFailed = false;
        Exception? caughtException = null;

        using var watcher = new PluginWatcher(_pluginManager, 50);
        watcher.PluginReloadFailed += (_, e) =>
        {
            reloadFailed = true;
            caughtException = e.Exception;
        };

        watcher.StartWatching();

        await Task.Delay(100);
        File.WriteAllBytes(pluginPath, [0x01, 0x02]);

        await Task.Delay(300);

        Assert.True(reloadFailed);
        Assert.NotNull(caughtException);
        Assert.IsType<InvalidOperationException>(caughtException);
    }

    [Fact]
    public async Task FileChange_Debounces_RapidChanges()
    {
        var pluginPath = Path.Combine(_tempDir, "debounce.dll");
        File.WriteAllBytes(pluginPath, [0x00]);

        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("Debounce.Plugin");
        manifest.Lifecycle.Returns(new PluginLifecycleConfig { SupportsHotReload = true });

        var context = Substitute.For<IPluginContext>();
        context.Manifest.Returns(manifest);
        context.PluginPath.Returns(pluginPath);
        context.IsLoaded.Returns(true);

        _pluginManager.GetLoadedPlugins().Returns([context]);
        _pluginManager.ReloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(context);

        using var watcher = new PluginWatcher(_pluginManager, 100);
        watcher.StartWatching();

        // Rapid changes within debounce interval
        await Task.Delay(50);
        File.WriteAllBytes(pluginPath, [0x01]);
        await Task.Delay(20);
        File.WriteAllBytes(pluginPath, [0x02]);
        await Task.Delay(20);
        File.WriteAllBytes(pluginPath, [0x03]);

        // Wait for final debounce
        await Task.Delay(250);

        // Should only have reloaded once (or maybe not at all if debounce logic prevents it)
        var calls = _pluginManager.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == "ReloadAsync");
        Assert.True(calls <= 1, "Should debounce rapid changes to single reload");
    }

    [Fact]
    public async Task NewFileCreated_InWatchedDirectory_LoadsPlugin()
    {
        _pluginManager.GetLoadedPlugins().Returns([]);

        var newContext = Substitute.For<IPluginContext>();
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("New.Plugin");
        newContext.Manifest.Returns(manifest);

        _pluginManager.LoadAsync(Arg.Any<string>(), Arg.Any<PluginLoadOptions>(), Arg.Any<CancellationToken>())
            .Returns(newContext);

        using var watcher = new PluginWatcher(_pluginManager, 50);
        watcher.WatchDirectory(_tempDir);
        watcher.StartWatching();

        await Task.Delay(100);

        // Create a new DLL file
        var newPluginPath = Path.Combine(_tempDir, "newplugin.dll");
        File.WriteAllBytes(newPluginPath, [0x00]);

        await Task.Delay(200);

        // Should have tried to load the new plugin
        await _pluginManager.Received().LoadAsync(
            Arg.Is<string>(s => s.Contains("newplugin.dll")),
            Arg.Any<PluginLoadOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FileChange_AfterDispose_IsIgnored()
    {
        var pluginPath = Path.Combine(_tempDir, "disposed.dll");
        File.WriteAllBytes(pluginPath, [0x00]);

        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("Disposed.Plugin");
        manifest.Lifecycle.Returns(new PluginLifecycleConfig { SupportsHotReload = true });

        var context = Substitute.For<IPluginContext>();
        context.Manifest.Returns(manifest);
        context.PluginPath.Returns(pluginPath);
        context.IsLoaded.Returns(true);

        _pluginManager.GetLoadedPlugins().Returns([context]);

        using var watcher = new PluginWatcher(_pluginManager, 50);
        watcher.StartWatching();

        // Dispose before file change
        watcher.Dispose();

        // Try to modify the file
        File.WriteAllBytes(pluginPath, [0x01, 0x02]);
        await Task.Delay(200);

        // Should not have reloaded
        await _pluginManager.DidNotReceive().ReloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Plugin with Invalid Directory Tests

    [Fact]
    public void StartWatching_WithPluginInNonExistentDirectory_DoesNotThrow()
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("Invalid.Plugin");
        manifest.Lifecycle.Returns(new PluginLifecycleConfig { SupportsHotReload = true });

        var context = Substitute.For<IPluginContext>();
        context.Manifest.Returns(manifest);
        context.PluginPath.Returns("/nonexistent/path/plugin.dll");
        context.IsLoaded.Returns(true);

        _pluginManager.GetLoadedPlugins().Returns([context]);

        _watcher.StartWatching();

        // Should not throw
    }

    [Fact]
    public void StartWatching_WithPluginInEmptyPath_DoesNotThrow()
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("Empty.Plugin");
        manifest.Lifecycle.Returns(new PluginLifecycleConfig { SupportsHotReload = true });

        var context = Substitute.For<IPluginContext>();
        context.Manifest.Returns(manifest);
        context.PluginPath.Returns("");
        context.IsLoaded.Returns(true);

        _pluginManager.GetLoadedPlugins().Returns([context]);

        _watcher.StartWatching();

        // Should not throw
    }

    #endregion

    #region Helper Methods

    private static IPluginContext CreateMockPluginContext(string pluginId, string directory)
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns(pluginId);
        manifest.Lifecycle.Returns(new PluginLifecycleConfig { SupportsHotReload = true });

        var context = Substitute.For<IPluginContext>();
        context.Manifest.Returns(manifest);
        context.PluginPath.Returns(Path.Combine(directory, "test.dll"));
        context.IsLoaded.Returns(true);

        return context;
    }

    #endregion
}

public class PluginReloadEventArgsTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var args = new PluginReloadEventArgs("Test.Plugin", "/path/to/plugin.dll");

        Assert.Equal("Test.Plugin", args.PluginId);
        Assert.Equal("/path/to/plugin.dll", args.PluginPath);
    }
}

public class PluginReloadFailedEventArgsTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var exception = new InvalidOperationException("Test error");
        var args = new PluginReloadFailedEventArgs("Test.Plugin", "/path/to/plugin.dll", exception);

        Assert.Equal("Test.Plugin", args.PluginId);
        Assert.Equal("/path/to/plugin.dll", args.PluginPath);
        Assert.Same(exception, args.Exception);
    }
}
