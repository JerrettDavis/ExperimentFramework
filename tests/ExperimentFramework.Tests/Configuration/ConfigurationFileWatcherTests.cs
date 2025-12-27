using ExperimentFramework.Configuration;
using ExperimentFramework.Configuration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration;

public class ConfigurationFileWatcherTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ITestOutputHelper _output;

    public ConfigurationFileWatcherTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDir = Path.Combine(Path.GetTempPath(), $"ConfigurationFileWatcherTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    #region Test Services

    public interface ITestService
    {
        string GetValue();
    }

    public class TestServiceA : ITestService
    {
        public string GetValue() => "A";
    }

    public class TestServiceB : ITestService
    {
        public string GetValue() => "B";
    }

    #endregion

    #region Helper Methods

    private string CreateYamlFile(string fileName, string content)
    {
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    private string GetValidYaml() => $"""
        experimentFramework:
          settings:
            proxyStrategy: dispatchProxy
          trials:
            - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
              selectionMode:
                type: featureFlag
                flagName: TestFlag
              control:
                key: control
                implementationType: "{typeof(TestServiceA).AssemblyQualifiedName}"
        """;

    private string GetUpdatedYaml() => $"""
        experimentFramework:
          settings:
            proxyStrategy: dispatchProxy
          trials:
            - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
              selectionMode:
                type: featureFlag
                flagName: UpdatedFlag
              control:
                key: control
                implementationType: "{typeof(TestServiceA).AssemblyQualifiedName}"
        """;

    private string GetInvalidYaml() => """
        experimentFramework:
          trials:
            - selectionMode:
                type: featureFlag
              control:
                key: control
                implementationType: "SomeType"
        """;

    #endregion

    [Fact]
    public void Hot_reload_registers_watcher_as_hosted_service()
    {
        // Arrange
        CreateYamlFile("experiments.yaml", GetValidYaml());
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();

        // Act
        services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
        {
            opts.BasePath = _tempDir;
            opts.ScanDefaultPaths = true;
            opts.EnableHotReload = true;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var hostedServices = provider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s.GetType().Name == "ConfigurationFileWatcher");
    }

    [Fact]
    public async Task Hot_reload_callback_invoked_on_file_change()
    {
        // Arrange
        var yamlPath = CreateYamlFile("experiments.yaml", GetValidYaml());
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();
        var callbackInvoked = false;
        ExperimentFrameworkConfigurationRoot? receivedConfig = null;

        services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
        {
            opts.BasePath = _tempDir;
            opts.ScanDefaultPaths = true;
            opts.EnableHotReload = true;
            opts.OnConfigurationChanged = config =>
            {
                callbackInvoked = true;
                receivedConfig = config;
            };
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();
        var watcher = hostedServices.FirstOrDefault(s => s.GetType().Name == "ConfigurationFileWatcher");
        Assert.NotNull(watcher);

        // Act
        await watcher.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        await File.WriteAllTextAsync(yamlPath, GetUpdatedYaml());
        await Task.Delay(1000);

        await watcher.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(callbackInvoked);
        Assert.NotNull(receivedConfig);
        Assert.NotNull(receivedConfig.Trials);
        Assert.NotEmpty(receivedConfig.Trials);
        Assert.Equal("UpdatedFlag", receivedConfig.Trials[0].SelectionMode.FlagName);
    }

    [Fact]
    public async Task Hot_reload_ignores_invalid_configuration()
    {
        // Arrange
        var yamlPath = CreateYamlFile("experiments.yaml", GetValidYaml());
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();
        var callbackCount = 0;

        services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
        {
            opts.BasePath = _tempDir;
            opts.ScanDefaultPaths = true;
            opts.EnableHotReload = true;
            opts.ThrowOnValidationErrors = false;
            opts.OnConfigurationChanged = _ => callbackCount++;
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();
        var watcher = hostedServices.FirstOrDefault(s => s.GetType().Name == "ConfigurationFileWatcher");
        Assert.NotNull(watcher);

        // Act
        await watcher.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        await File.WriteAllTextAsync(yamlPath, GetInvalidYaml());
        await Task.Delay(1000);

        await watcher.StopAsync(CancellationToken.None);

        // Assert - callback should not be invoked for invalid config
        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public async Task Hot_reload_debounces_rapid_changes()
    {
        // Arrange
        var yamlPath = CreateYamlFile("experiments.yaml", GetValidYaml());
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();
        var callbackCount = 0;

        services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
        {
            opts.BasePath = _tempDir;
            opts.ScanDefaultPaths = true;
            opts.EnableHotReload = true;
            opts.OnConfigurationChanged = _ => Interlocked.Increment(ref callbackCount);
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();
        var watcher = hostedServices.FirstOrDefault(s => s.GetType().Name == "ConfigurationFileWatcher");
        Assert.NotNull(watcher);

        // Act
        await watcher.StartAsync(CancellationToken.None);
        await Task.Delay(200); // Give watcher time to fully initialize

        // Make rapid changes
        for (var i = 0; i < 5; i++)
        {
            await File.WriteAllTextAsync(yamlPath, GetUpdatedYaml());
            await Task.Delay(50); // Less than debounce interval (500ms)
        }

        // Wait longer for file system events to be processed
        // File system watchers can be slow and may fire multiple events per write
        await Task.Delay(1500);
        await watcher.StopAsync(CancellationToken.None);

        // Assert - should be debounced to a small number of calls
        // Allow up to 3 callbacks since file system watchers may fire multiple events per write
        // and timing can vary across different systems
        _output.WriteLine($"Callback count: {callbackCount}");
        Assert.True(callbackCount <= 3, $"Expected at most 3 callbacks due to debouncing, got {callbackCount}");
    }

    [Fact]
    public async Task Hot_reload_only_watches_discovered_files()
    {
        // Arrange
        CreateYamlFile("experiments.yaml", GetValidYaml());
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();
        var callbackCount = 0;

        services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
        {
            opts.BasePath = _tempDir;
            opts.ScanDefaultPaths = true;
            opts.EnableHotReload = true;
            opts.OnConfigurationChanged = _ => Interlocked.Increment(ref callbackCount);
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();
        var watcher = hostedServices.FirstOrDefault(s => s.GetType().Name == "ConfigurationFileWatcher");
        Assert.NotNull(watcher);

        // Act
        await watcher.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        // Create and modify an unrelated yaml file
        var unrelatedPath = Path.Combine(_tempDir, "unrelated.yaml");
        await File.WriteAllTextAsync(unrelatedPath, "foo: bar");

        await Task.Delay(1000);
        await watcher.StopAsync(CancellationToken.None);

        // Assert - callback should not be invoked for unrelated file
        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public async Task Hot_reload_handles_file_deletion_gracefully()
    {
        // Arrange
        var yamlPath = CreateYamlFile("experiments.yaml", GetValidYaml());
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();

        services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
        {
            opts.BasePath = _tempDir;
            opts.ScanDefaultPaths = true;
            opts.EnableHotReload = true;
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();
        var watcher = hostedServices.FirstOrDefault(s => s.GetType().Name == "ConfigurationFileWatcher");
        Assert.NotNull(watcher);

        // Act
        await watcher.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        File.Delete(yamlPath);
        await Task.Delay(1000);

        // Assert - should not throw
        var exception = await Record.ExceptionAsync(async () =>
            await watcher.StopAsync(CancellationToken.None));
        Assert.Null(exception);
    }

    [Fact]
    public async Task Watcher_disposes_cleanly()
    {
        // Arrange
        CreateYamlFile("experiments.yaml", GetValidYaml());
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();

        services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
        {
            opts.BasePath = _tempDir;
            opts.ScanDefaultPaths = true;
            opts.EnableHotReload = true;
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();
        var watcher = hostedServices.FirstOrDefault(s => s.GetType().Name == "ConfigurationFileWatcher");
        Assert.NotNull(watcher);

        // Act
        await watcher.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await watcher.StopAsync(CancellationToken.None);

        // Assert - should not throw
        var exception = Record.Exception(() =>
        {
            if (watcher is IDisposable disposable)
            {
                disposable.Dispose();
            }
        });
        Assert.Null(exception);
    }

    [Fact]
    public void Hot_reload_with_no_files_succeeds()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Act
        services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
        {
            opts.BasePath = _tempDir;
            opts.ScanDefaultPaths = true;
            opts.EnableHotReload = true;
        });
        var provider = services.BuildServiceProvider();

        // Assert - should not throw
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task Multiple_configuration_files_watched()
    {
        // Arrange
        var defsDir = Path.Combine(_tempDir, "ExperimentDefinitions");
        Directory.CreateDirectory(defsDir);

        CreateYamlFile("experiments.yaml", GetValidYaml());
        var secondFile = Path.Combine(defsDir, "more-experiments.yaml");
        await File.WriteAllTextAsync(secondFile, $"""
                                                  experimentFramework:
                                                    trials:
                                                      - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                                                        selectionMode:
                                                          type: configurationKey
                                                          key: TestKey
                                                        control:
                                                          key: default
                                                          implementationType: "{typeof(TestServiceB).AssemblyQualifiedName}"
                                                  """);

        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();
        services.AddScoped<ITestService, TestServiceB>();
        var callbackCount = 0;

        services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
        {
            opts.BasePath = _tempDir;
            opts.ScanDefaultPaths = true;
            opts.EnableHotReload = true;
            opts.OnConfigurationChanged = _ => Interlocked.Increment(ref callbackCount);
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();
        var watcher = hostedServices.FirstOrDefault(s => s.GetType().Name == "ConfigurationFileWatcher");
        Assert.NotNull(watcher);

        // Act
        await watcher.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        // Modify the secondary file
        await File.WriteAllTextAsync(secondFile, $"""
                                                  experimentFramework:
                                                    trials:
                                                      - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                                                        selectionMode:
                                                          type: configurationKey
                                                          key: UpdatedKey
                                                        control:
                                                          key: default
                                                          implementationType: "{typeof(TestServiceB).AssemblyQualifiedName}"
                                                  """);

        await Task.Delay(1000);
        await watcher.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(callbackCount >= 1);
    }

    [Fact]
    public async Task File_rename_handled_gracefully()
    {
        // Arrange
        var yamlPath = CreateYamlFile("experiments.yaml", GetValidYaml());
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();

        services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
        {
            opts.BasePath = _tempDir;
            opts.ScanDefaultPaths = true;
            opts.EnableHotReload = true;
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();
        var watcher = hostedServices.FirstOrDefault(s => s.GetType().Name == "ConfigurationFileWatcher");
        Assert.NotNull(watcher);

        // Act
        await watcher.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        var newPath = Path.Combine(_tempDir, "experiments-renamed.yaml");
        File.Move(yamlPath, newPath);

        await Task.Delay(1000);

        // Assert - should not throw
        var exception = await Record.ExceptionAsync(async () =>
            await watcher.StopAsync(CancellationToken.None));
        Assert.Null(exception);
    }

    [Fact]
    public async Task Callback_receives_merged_configuration()
    {
        // Arrange
        var defsDir = Path.Combine(_tempDir, "ExperimentDefinitions");
        Directory.CreateDirectory(defsDir);

        CreateYamlFile("experiments.yaml", GetValidYaml());
        var secondFile = Path.Combine(defsDir, "more-experiments.yaml");
        await File.WriteAllTextAsync(secondFile, $"""
                                                  experimentFramework:
                                                    trials:
                                                      - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                                                        selectionMode:
                                                          type: configurationKey
                                                          key: SecondKey
                                                        control:
                                                          key: default
                                                          implementationType: "{typeof(TestServiceB).AssemblyQualifiedName}"
                                                  """);

        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();
        services.AddScoped<ITestService, TestServiceB>();
        ExperimentFrameworkConfigurationRoot? receivedConfig = null;

        services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
        {
            opts.BasePath = _tempDir;
            opts.ScanDefaultPaths = true;
            opts.EnableHotReload = true;
            opts.OnConfigurationChanged = config => receivedConfig = config;
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();
        var watcher = hostedServices.FirstOrDefault(s => s.GetType().Name == "ConfigurationFileWatcher");
        Assert.NotNull(watcher);

        // Act
        await watcher.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        // Trigger a change
        await File.WriteAllTextAsync(secondFile, $"""
                                                  experimentFramework:
                                                    trials:
                                                      - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                                                        selectionMode:
                                                          type: configurationKey
                                                          key: UpdatedSecondKey
                                                        control:
                                                          key: default
                                                          implementationType: "{typeof(TestServiceB).AssemblyQualifiedName}"
                                                  """);

        await Task.Delay(1000);
        await watcher.StopAsync(CancellationToken.None);

        // Assert - should receive merged config with both trials
        Assert.NotNull(receivedConfig);
        Assert.NotNull(receivedConfig.Trials);
        Assert.Equal(2, receivedConfig.Trials.Count);
    }
}
