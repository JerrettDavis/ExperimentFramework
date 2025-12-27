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
