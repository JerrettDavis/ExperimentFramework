using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.HotReload;

namespace ExperimentFramework.Plugins.Tests.HotReload;

public class PluginWatcherTests
{
    private readonly IPluginManager _pluginManager;
    private readonly PluginWatcher _watcher;

    public PluginWatcherTests()
    {
        _pluginManager = Substitute.For<IPluginManager>();
        _pluginManager.GetLoadedPlugins().Returns([]);
        _watcher = new PluginWatcher(_pluginManager, 100);
    }

    [Fact]
    public void Constructor_ThrowsOnNullPluginManager()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PluginWatcher(null!));
    }

    [Fact]
    public void StartWatching_DoesNotThrow()
    {
        _watcher.StartWatching();

        // No exception means success
    }

    [Fact]
    public void StopWatching_DoesNotThrow()
    {
        _watcher.StartWatching();
        _watcher.StopWatching();

        // No exception means success
    }

    [Fact]
    public void WatchDirectory_NonExistentPath_DoesNotThrow()
    {
        _watcher.WatchDirectory("/nonexistent/path");

        // Should just log a warning, not throw
    }

    [Fact]
    public void WatchDirectory_ValidPath_Succeeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            _watcher.WatchDirectory(tempDir);
            // No exception means success
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void Dispose_StopsWatching()
    {
        _watcher.StartWatching();
        _watcher.Dispose();

        // Should not throw on double dispose
        _watcher.Dispose();
    }

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

    [Fact]
    public void StartWatching_AfterDispose_ThrowsObjectDisposedException()
    {
        _watcher.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _watcher.StartWatching());
    }

    [Fact]
    public void WatchDirectory_AfterDispose_ThrowsObjectDisposedException()
    {
        _watcher.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _watcher.WatchDirectory("./plugins"));
    }
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
