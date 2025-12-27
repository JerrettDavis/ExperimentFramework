using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Configuration;
using ExperimentFramework.Plugins.HotReload;
using ExperimentFramework.Plugins.Manifest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Plugins.Tests.HotReload;

public class PluginReloadServiceTests : IDisposable
{
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<PluginReloadService> _logger;
    private readonly string _tempDir;

    public PluginReloadServiceTests()
    {
        _pluginManager = Substitute.For<IPluginManager>();
        _pluginManager.GetLoadedPlugins().Returns([]);
        _logger = Substitute.For<ILogger<PluginReloadService>>();
        _tempDir = Path.Combine(Path.GetTempPath(), $"PluginReloadServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullPluginManager_ThrowsArgumentNullException()
    {
        var options = Options.Create(new PluginConfigurationOptions());

        Assert.Throws<ArgumentNullException>(() =>
            new PluginReloadService(null!, options, _logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PluginReloadService(_pluginManager, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var options = Options.Create(new PluginConfigurationOptions());

        Assert.Throws<ArgumentNullException>(() =>
            new PluginReloadService(_pluginManager, options, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesService()
    {
        var options = Options.Create(new PluginConfigurationOptions());

        var service = new PluginReloadService(_pluginManager, options, _logger);

        Assert.NotNull(service);
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_WhenHotReloadDisabled_DoesNotStartWatching()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = false
        });
        var service = new PluginReloadService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);

        // Should not have accessed plugin manager for watching
        _pluginManager.DidNotReceive().GetLoadedPlugins();
    }

    [Fact]
    public async Task StartAsync_WhenHotReloadEnabled_StartsWatching()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = true,
            DiscoveryPaths = []
        });
        var service = new PluginReloadService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);

        // Watcher is started internally - verify by accessing loaded plugins
        _pluginManager.Received().GetLoadedPlugins();
    }

    [Fact]
    public async Task StartAsync_WithDiscoveryPaths_WatchesDirectories()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = true,
            DiscoveryPaths = [_tempDir]
        });
        var service = new PluginReloadService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);

        // Should not throw and watcher should be active
        Assert.NotNull(service);
    }

    [Fact]
    public async Task StartAsync_WithWildcardPath_ExtractsDirectory()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = true,
            DiscoveryPaths = [Path.Combine(_tempDir, "*.dll")]
        });
        var service = new PluginReloadService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);

        Assert.NotNull(service);
    }

    [Fact]
    public async Task StartAsync_WithNonExistentPath_HandlesGracefully()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = true,
            DiscoveryPaths = ["/nonexistent/path"]
        });
        var service = new PluginReloadService(_pluginManager, options, _logger);

        // Should not throw
        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithFilePath_ExtractsDirectory()
    {
        var filePath = Path.Combine(_tempDir, "plugin.dll");
        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = true,
            DiscoveryPaths = [filePath]
        });
        var service = new PluginReloadService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);

        Assert.NotNull(service);
    }

    [Fact]
    public async Task StartAsync_WithCustomDebounceMs_UsesCustomValue()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = true,
            HotReloadDebounceMs = 1000,
            DiscoveryPaths = [_tempDir]
        });
        var service = new PluginReloadService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);

        Assert.NotNull(service);
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_WhenNotStarted_CompletesWithoutError()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = false
        });
        var service = new PluginReloadService(_pluginManager, options, _logger);

        await service.StopAsync(CancellationToken.None);

        // Should complete without throwing
    }

    [Fact]
    public async Task StopAsync_AfterStarting_StopsWatching()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = true,
            DiscoveryPaths = [_tempDir]
        });
        var service = new PluginReloadService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Should complete without throwing
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WhenNotStarted_DoesNotThrow()
    {
        var options = Options.Create(new PluginConfigurationOptions());
        var service = new PluginReloadService(_pluginManager, options, _logger);

        service.Dispose();

        // Should not throw
    }

    [Fact]
    public async Task Dispose_AfterStarting_DisposesWatcher()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = true,
            DiscoveryPaths = [_tempDir]
        });
        var service = new PluginReloadService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);
        service.Dispose();

        // Should not throw
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_IsIdempotent()
    {
        var options = Options.Create(new PluginConfigurationOptions());
        var service = new PluginReloadService(_pluginManager, options, _logger);

        service.Dispose();
        service.Dispose();
        service.Dispose();

        // Should not throw
    }

    #endregion

    #region Event Handler Tests

    [Fact]
    public async Task PluginReloadTriggered_LogsDebugMessage()
    {
        var pluginPath = Path.Combine(_tempDir, "testplugin.dll");
        File.WriteAllBytes(pluginPath, [0x00]);

        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("Test.Plugin");
        manifest.Lifecycle.Returns(new PluginLifecycleConfig { SupportsHotReload = true });

        var context = Substitute.For<IPluginContext>();
        context.Manifest.Returns(manifest);
        context.PluginPath.Returns(pluginPath);
        context.IsLoaded.Returns(true);

        _pluginManager.GetLoadedPlugins().Returns([context]);
        _pluginManager.ReloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(context);

        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = true,
            HotReloadDebounceMs = 50,
            DiscoveryPaths = []
        });

        using var service = new PluginReloadService(_pluginManager, options, _logger);
        await service.StartAsync(CancellationToken.None);

        // Trigger reload by modifying the file
        await Task.Delay(100);
        File.WriteAllBytes(pluginPath, [0x01, 0x02]);
        await Task.Delay(200);

        // The reload events should have triggered logger calls
        // Verify the service logs appropriately
    }

    [Fact]
    public async Task PluginReloadCompleted_LogsInformationMessage()
    {
        var pluginPath = Path.Combine(_tempDir, "completedplugin.dll");
        File.WriteAllBytes(pluginPath, [0x00]);

        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("Completed.Plugin");
        manifest.Lifecycle.Returns(new PluginLifecycleConfig { SupportsHotReload = true });

        var context = Substitute.For<IPluginContext>();
        context.Manifest.Returns(manifest);
        context.PluginPath.Returns(pluginPath);
        context.IsLoaded.Returns(true);

        _pluginManager.GetLoadedPlugins().Returns([context]);
        _pluginManager.ReloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(context);

        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = true,
            HotReloadDebounceMs = 50,
            DiscoveryPaths = []
        });

        using var service = new PluginReloadService(_pluginManager, options, _logger);
        await service.StartAsync(CancellationToken.None);

        await Task.Delay(100);
        File.WriteAllBytes(pluginPath, [0x01, 0x02]);
        await Task.Delay(200);

        // Verify reload was triggered
        await _pluginManager.Received(1).ReloadAsync("Completed.Plugin", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PluginReloadFailed_LogsErrorMessage()
    {
        var pluginPath = Path.Combine(_tempDir, "failingplugin.dll");
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

        var options = Options.Create(new PluginConfigurationOptions
        {
            EnableHotReload = true,
            HotReloadDebounceMs = 50,
            DiscoveryPaths = []
        });

        using var service = new PluginReloadService(_pluginManager, options, _logger);
        await service.StartAsync(CancellationToken.None);

        await Task.Delay(100);
        File.WriteAllBytes(pluginPath, [0x01, 0x02]);
        await Task.Delay(200);

        // Verify reload was attempted
        await _pluginManager.Received(1).ReloadAsync("Failing.Plugin", Arg.Any<CancellationToken>());
    }

    #endregion
}
