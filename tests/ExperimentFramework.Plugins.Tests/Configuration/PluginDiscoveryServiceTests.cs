using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Plugins.Tests.Configuration;

public class PluginDiscoveryServiceTests
{
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<PluginDiscoveryService> _logger;

    public PluginDiscoveryServiceTests()
    {
        _pluginManager = Substitute.For<IPluginManager>();
        _logger = Substitute.For<ILogger<PluginDiscoveryService>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullPluginManager_ThrowsArgumentNullException()
    {
        var options = Options.Create(new PluginConfigurationOptions());

        Assert.Throws<ArgumentNullException>(() =>
            new PluginDiscoveryService(null!, options, _logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PluginDiscoveryService(_pluginManager, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var options = Options.Create(new PluginConfigurationOptions());

        Assert.Throws<ArgumentNullException>(() =>
            new PluginDiscoveryService(_pluginManager, options, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesService()
    {
        var options = Options.Create(new PluginConfigurationOptions());

        var service = new PluginDiscoveryService(_pluginManager, options, _logger);

        Assert.NotNull(service);
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_WhenAutoLoadDisabled_SkipsDiscovery()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            AutoLoadOnStartup = false
        });
        var service = new PluginDiscoveryService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);

        await _pluginManager.DidNotReceive().DiscoverAndLoadAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WithNoDiscoveryPaths_SkipsDiscovery()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            AutoLoadOnStartup = true,
            DiscoveryPaths = []
        });
        var service = new PluginDiscoveryService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);

        await _pluginManager.DidNotReceive().DiscoverAndLoadAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WithDiscoveryPaths_CallsDiscoverAndLoad()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            AutoLoadOnStartup = true,
            DiscoveryPaths = ["./plugins"]
        });
        _pluginManager.DiscoverAndLoadAsync(Arg.Any<CancellationToken>())
            .Returns(new List<IPluginContext>());

        var service = new PluginDiscoveryService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);

        await _pluginManager.Received(1).DiscoverAndLoadAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WhenDiscoverReturnsPlugins_LogsPluginInfo()
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("Test.Plugin");
        manifest.Version.Returns("1.0.0");

        var context = Substitute.For<IPluginContext>();
        context.Manifest.Returns(manifest);

        var options = Options.Create(new PluginConfigurationOptions
        {
            AutoLoadOnStartup = true,
            DiscoveryPaths = ["./plugins"]
        });
        _pluginManager.DiscoverAndLoadAsync(Arg.Any<CancellationToken>())
            .Returns(new List<IPluginContext> { context });

        var service = new PluginDiscoveryService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);

        await _pluginManager.Received(1).DiscoverAndLoadAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WhenCancelled_HandlesGracefully()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            AutoLoadOnStartup = true,
            DiscoveryPaths = ["./plugins"]
        });
        _pluginManager.DiscoverAndLoadAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<IPluginContext>>(x => throw new OperationCanceledException());

        var service = new PluginDiscoveryService(_pluginManager, options, _logger);

        // Should not throw
        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WhenExceptionOccurs_HandlesGracefully()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            AutoLoadOnStartup = true,
            DiscoveryPaths = ["./plugins"]
        });
        _pluginManager.DiscoverAndLoadAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<IPluginContext>>(x => throw new InvalidOperationException("Test error"));

        var service = new PluginDiscoveryService(_pluginManager, options, _logger);

        // Should not throw
        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithMultipleDiscoveryPaths_CallsDiscoverOnce()
    {
        var options = Options.Create(new PluginConfigurationOptions
        {
            AutoLoadOnStartup = true,
            DiscoveryPaths = ["./plugins", "./extensions", "./addons"]
        });
        _pluginManager.DiscoverAndLoadAsync(Arg.Any<CancellationToken>())
            .Returns(new List<IPluginContext>());

        var service = new PluginDiscoveryService(_pluginManager, options, _logger);

        await service.StartAsync(CancellationToken.None);

        await _pluginManager.Received(1).DiscoverAndLoadAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_CompletesImmediately()
    {
        var options = Options.Create(new PluginConfigurationOptions());
        var service = new PluginDiscoveryService(_pluginManager, options, _logger);

        var task = service.StopAsync(CancellationToken.None);

        Assert.True(task.IsCompleted);
        await task;
    }

    [Fact]
    public async Task StopAsync_WithCancellation_CompletesImmediately()
    {
        var options = Options.Create(new PluginConfigurationOptions());
        var service = new PluginDiscoveryService(_pluginManager, options, _logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var task = service.StopAsync(cts.Token);

        Assert.True(task.IsCompleted);
        await task;
    }

    #endregion
}
