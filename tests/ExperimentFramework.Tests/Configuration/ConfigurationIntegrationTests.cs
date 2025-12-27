using ExperimentFramework.Configuration;
using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Loading;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Tests.Configuration;

/// <summary>
/// Integration tests for the full configuration loading and service registration pipeline.
/// </summary>
public class ConfigurationIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ExperimentConfigurationLoader _loader = new();
    private readonly ConfigurationValidator _validator = new();

    public ConfigurationIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ExperimentFrameworkIntegrationTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
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

    public interface IAnotherService
    {
        int Calculate(int x);
    }

    public class AnotherServiceImpl : IAnotherService
    {
        public int Calculate(int x) => x * 2;
    }

    public class AnotherServiceAlt : IAnotherService
    {
        public int Calculate(int x) => x * 3;
    }

    #endregion

    [Fact]
    public void LoadFromYaml_BasicTrial_ParsesCorrectly()
    {
        // Arrange
        var yaml = $"""
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
                  conditions:
                    - key: variant
                      implementationType: "{typeof(TestServiceB).AssemblyQualifiedName}"
            """;

        var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
        File.WriteAllText(yamlPath, yaml);

        // Act
        var result = _loader.LoadFromFile(yamlPath);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Settings);
        Assert.Equal("dispatchProxy", result.Settings.ProxyStrategy);
        Assert.NotNull(result.Trials);
        Assert.Single(result.Trials);
        Assert.Equal("featureFlag", result.Trials[0].SelectionMode.Type);
        Assert.Single(result.Trials[0].Conditions ?? []);
    }

    [Fact]
    public void LoadFromJson_BasicTrial_ParsesCorrectly()
    {
        // Arrange
        var json = $$"""
            {
              "experimentFramework": {
                "settings": {
                  "proxyStrategy": "dispatchProxy"
                },
                "trials": [
                  {
                    "serviceType": "{{typeof(ITestService).AssemblyQualifiedName}}",
                    "selectionMode": {
                      "type": "featureFlag",
                      "flagName": "TestFlag"
                    },
                    "control": {
                      "key": "control",
                      "implementationType": "{{typeof(TestServiceA).AssemblyQualifiedName}}"
                    }
                  }
                ]
              }
            }
            """;

        var jsonPath = Path.Combine(_tempDir, "experiments.json");
        File.WriteAllText(jsonPath, json);

        // Act
        var result = _loader.LoadFromFile(jsonPath);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Settings);
        Assert.Equal("dispatchProxy", result.Settings.ProxyStrategy);
        Assert.NotNull(result.Trials);
        Assert.Single(result.Trials);
    }

    [Fact]
    public void LoadFromYaml_WithTypeAliases_BuildsCorrectly()
    {
        // Arrange
        var yaml = """
            experimentFramework:
              settings:
                proxyStrategy: dispatchProxy
              trials:
                - serviceType: "ITestService"
                  selectionMode:
                    type: featureFlag
                    flagName: TestFlag
                  control:
                    key: control
                    implementationType: "TestServiceA"
            """;

        var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
        File.WriteAllText(yamlPath, yaml);

        var typeAliases = new Dictionary<string, Type>
        {
            ["ITestService"] = typeof(ITestService),
            ["TestServiceA"] = typeof(TestServiceA)
        };
        var typeResolver = new TypeResolver(null, typeAliases);
        var builder = new ConfigurationExperimentBuilder(typeResolver);

        // Act
        var config = _loader.LoadFromFile(yamlPath);
        var frameworkBuilder = builder.Build(config);

        // Assert
        Assert.NotNull(frameworkBuilder);
        var frameworkConfig = frameworkBuilder.Build();
        Assert.Single(frameworkConfig.Definitions);
    }

    [Fact]
    public void LoadFromYaml_WithMultipleTrials_ParsesAll()
    {
        // Arrange
        var yaml = $"""
            experimentFramework:
              settings:
                proxyStrategy: dispatchProxy
              trials:
                - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                  selectionMode:
                    type: featureFlag
                    flagName: TestFlag1
                  control:
                    key: control
                    implementationType: "{typeof(TestServiceA).AssemblyQualifiedName}"
                - serviceType: "{typeof(IAnotherService).AssemblyQualifiedName}"
                  selectionMode:
                    type: featureFlag
                    flagName: TestFlag2
                  control:
                    key: control
                    implementationType: "{typeof(AnotherServiceImpl).AssemblyQualifiedName}"
            """;

        var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
        File.WriteAllText(yamlPath, yaml);

        // Act
        var result = _loader.LoadFromFile(yamlPath);

        // Assert
        Assert.NotNull(result.Trials);
        Assert.Equal(2, result.Trials.Count);
    }

    [Fact]
    public void LoadFromYaml_WithNamedExperiment_ParsesCorrectly()
    {
        // Arrange
        var yaml = $"""
            experimentFramework:
              settings:
                proxyStrategy: dispatchProxy
              experiments:
                - name: test-experiment
                  metadata:
                    owner: test-team
                    ticket: TEST-123
                  trials:
                    - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                      selectionMode:
                        type: featureFlag
                        flagName: TestFlag
                      control:
                        key: control
                        implementationType: "{typeof(TestServiceA).AssemblyQualifiedName}"
            """;

        var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
        File.WriteAllText(yamlPath, yaml);

        // Act
        var result = _loader.LoadFromFile(yamlPath);

        // Assert
        Assert.NotNull(result.Experiments);
        Assert.Single(result.Experiments);
        Assert.Equal("test-experiment", result.Experiments[0].Name);
        Assert.NotNull(result.Experiments[0].Metadata);
        Assert.Equal("test-team", result.Experiments[0].Metadata!["owner"]);
        Assert.Equal("TEST-123", result.Experiments[0].Metadata!["ticket"]);
    }

    [Fact]
    public void LoadFromYaml_WithActivation_ParsesCorrectly()
    {
        // Arrange
        var yaml = $"""
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
                  activation:
                    from: "2024-01-01T00:00:00Z"
                    until: "2030-12-31T23:59:59Z"
            """;

        var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
        File.WriteAllText(yamlPath, yaml);

        // Act
        var result = _loader.LoadFromFile(yamlPath);

        // Assert
        Assert.NotNull(result.Trials);
        Assert.NotNull(result.Trials[0].Activation);
        Assert.NotNull(result.Trials[0].Activation!.From);
        Assert.NotNull(result.Trials[0].Activation!.Until);
    }

    [Fact]
    public void LoadFromYaml_WithErrorPolicy_ParsesCorrectly()
    {
        // Arrange
        var yaml = $"""
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
                  conditions:
                    - key: variant
                      implementationType: "{typeof(TestServiceB).AssemblyQualifiedName}"
                  errorPolicy:
                    type: fallbackToControl
            """;

        var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
        File.WriteAllText(yamlPath, yaml);

        // Act
        var result = _loader.LoadFromFile(yamlPath);

        // Assert
        Assert.NotNull(result.Trials);
        Assert.NotNull(result.Trials[0].ErrorPolicy);
        Assert.Equal("fallbackToControl", result.Trials[0].ErrorPolicy!.Type);
    }

    [Fact]
    public void BuildFromConfig_HybridMode_MergesWithProgrammaticConfig()
    {
        // Arrange - Config has one trial
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag", FlagName = "TestFlag1" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! }
                }
            ]
        };

        // Programmatic builder has another trial
        var existingBuilder = ExperimentFrameworkBuilder.Create()
            .UseDispatchProxy()
            .Trial<IAnotherService>(t => t
                .UsingFeatureFlag("TestFlag2")
                .AddControl<AnotherServiceImpl>("control"));

        var typeResolver = new TypeResolver();
        var builder = new ConfigurationExperimentBuilder(typeResolver);

        // Act - Merge config into existing builder
        builder.MergeInto(existingBuilder, config);

        // Assert - Both definitions should be present
        var frameworkConfig = existingBuilder.Build();
        Assert.Equal(2, frameworkConfig.Definitions.Length);
    }

    [Fact]
    public void LoadFromAppsettings_ExperimentFrameworkSection_ParsesSettings()
    {
        // Arrange - Note: Only settings are bound from IConfiguration section
        // Complex nested objects like Trials should come from YAML/JSON files
        var json = """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information"
                }
              },
              "ExperimentFramework": {
                "Settings": {
                  "ProxyStrategy": "dispatchProxy",
                  "NamingConvention": "custom"
                }
              }
            }
            """;

        var jsonPath = Path.Combine(_tempDir, "appsettings.json");
        File.WriteAllText(jsonPath, json);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(_tempDir)
            .AddJsonFile("appsettings.json")
            .Build();

        // Act
        var result = _loader.Load(configuration, new ExperimentFrameworkConfigurationOptions
        {
            ScanDefaultPaths = false
        });

        // Assert
        Assert.NotNull(result.Settings);
        Assert.Equal("dispatchProxy", result.Settings.ProxyStrategy);
        Assert.Equal("custom", result.Settings.NamingConvention);
    }

    [Fact]
    public void LoadFromAppsettings_CustomSectionName_ParsesSettings()
    {
        // Arrange - Note: Only settings are bound from IConfiguration section
        var json = """
            {
              "MyExperiments": {
                "Settings": {
                  "ProxyStrategy": "dispatchProxy",
                  "NamingConvention": "camelCase"
                }
              }
            }
            """;

        var jsonPath = Path.Combine(_tempDir, "appsettings.json");
        File.WriteAllText(jsonPath, json);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(_tempDir)
            .AddJsonFile("appsettings.json")
            .Build();

        // Act
        var result = _loader.Load(configuration, new ExperimentFrameworkConfigurationOptions
        {
            ConfigurationSectionName = "MyExperiments",
            ScanDefaultPaths = false
        });

        // Assert
        Assert.NotNull(result.Settings);
        Assert.Equal("dispatchProxy", result.Settings.ProxyStrategy);
        Assert.Equal("camelCase", result.Settings.NamingConvention);
    }

    [Fact]
    public void Loader_LoadsMultipleYamlFiles_MergesConfiguration()
    {
        // Arrange
        var yaml1 = $"""
            experimentFramework:
              trials:
                - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                  selectionMode:
                    type: featureFlag
                    flagName: TestFlag1
                  control:
                    key: control
                    implementationType: "{typeof(TestServiceA).AssemblyQualifiedName}"
            """;

        var yaml2 = $"""
            experimentFramework:
              trials:
                - serviceType: "{typeof(IAnotherService).AssemblyQualifiedName}"
                  selectionMode:
                    type: featureFlag
                    flagName: TestFlag2
                  control:
                    key: control
                    implementationType: "{typeof(AnotherServiceImpl).AssemblyQualifiedName}"
            """;

        var definitionsDir = Path.Combine(_tempDir, "ExperimentDefinitions");
        Directory.CreateDirectory(definitionsDir);
        File.WriteAllText(Path.Combine(definitionsDir, "test1.yaml"), yaml1);
        File.WriteAllText(Path.Combine(definitionsDir, "test2.yaml"), yaml2);

        var configuration = new ConfigurationBuilder().Build();

        // Act
        var result = _loader.Load(configuration, new ExperimentFrameworkConfigurationOptions
        {
            BasePath = _tempDir,
            ScanDefaultPaths = true
        });

        // Assert
        Assert.NotNull(result.Trials);
        Assert.Equal(2, result.Trials.Count);
    }

    [Fact]
    public void Validator_ReturnsError_WhenServiceTypeMissing()
    {
        // Arrange - Config with missing service type
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = "", // Missing
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = "SomeType" }
                }
            ]
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.Contains("serviceType"));
    }

    [Fact]
    public void Validator_ReturnsError_WhenSelectionModeInvalid()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = "SomeService",
                    SelectionMode = new SelectionModeConfig { Type = "invalidMode" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = "SomeType" }
                }
            ]
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.Contains("selectionMode"));
    }

    [Fact]
    public void EndToEnd_YamlToServices_Works()
    {
        // Arrange
        var yaml = $"""
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

        var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
        File.WriteAllText(yamlPath, yaml);

        _ = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExperimentFramework:Settings:ProxyStrategy"] = "dispatchProxy"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<TestServiceA>();

        // Act - Load, validate, build, register
        var config = _loader.LoadFromFile(yamlPath);
        var validationResult = _validator.Validate(config);
        Assert.True(validationResult.IsValid);

        var typeResolver = new TypeResolver();
        var builder = new ConfigurationExperimentBuilder(typeResolver);
        var frameworkBuilder = builder.Build(config);

        var frameworkConfig = frameworkBuilder.Build();

        // Assert
        Assert.Single(frameworkConfig.Definitions);
        Assert.Equal(typeof(ITestService), frameworkConfig.Definitions[0].ServiceType);
    }

    [Fact]
    public void LoadFromYaml_AllSelectionModes_ParseCorrectly()
    {
        // Arrange
        var yaml = $"""
            experimentFramework:
              trials:
                - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                  selectionMode:
                    type: featureFlag
                    flagName: Flag1
                  control:
                    key: control
                    implementationType: "{typeof(TestServiceA).AssemblyQualifiedName}"
                - serviceType: "{typeof(IAnotherService).AssemblyQualifiedName}"
                  selectionMode:
                    type: configurationKey
                    key: ConfigKey1
                  control:
                    key: control
                    implementationType: "{typeof(AnotherServiceImpl).AssemblyQualifiedName}"
            """;

        var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
        File.WriteAllText(yamlPath, yaml);

        // Act
        var result = _loader.LoadFromFile(yamlPath);

        // Assert
        Assert.NotNull(result.Trials);
        Assert.Equal(2, result.Trials.Count);
        Assert.Equal("featureFlag", result.Trials[0].SelectionMode.Type);
        Assert.Equal("Flag1", result.Trials[0].SelectionMode.FlagName);
        Assert.Equal("configurationKey", result.Trials[1].SelectionMode.Type);
        Assert.Equal("ConfigKey1", result.Trials[1].SelectionMode.Key);
    }

    [Fact]
    public void LoadFromYaml_AllErrorPolicies_ParseCorrectly()
    {
        // Arrange
        var yaml = $"""
            experimentFramework:
              trials:
                - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                  selectionMode:
                    type: featureFlag
                  control:
                    key: control
                    implementationType: "{typeof(TestServiceA).AssemblyQualifiedName}"
                  conditions:
                    - key: variant1
                      implementationType: "{typeof(TestServiceB).AssemblyQualifiedName}"
                  errorPolicy:
                    type: tryInOrder
                    fallbackKeys:
                      - variant1
                      - control
            """;

        var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
        File.WriteAllText(yamlPath, yaml);

        // Act
        var result = _loader.LoadFromFile(yamlPath);

        // Assert
        Assert.NotNull(result.Trials);
        Assert.NotNull(result.Trials[0].ErrorPolicy);
        Assert.Equal("tryInOrder", result.Trials[0].ErrorPolicy!.Type);
        Assert.NotNull(result.Trials[0].ErrorPolicy!.FallbackKeys);
        Assert.Equal(2, result.Trials[0].ErrorPolicy!.FallbackKeys!.Count);
    }

    [Fact]
    public void LoadFromYaml_WithHypothesis_ParsesCorrectly()
    {
        // Arrange
        var yaml = $"""
            experimentFramework:
              experiments:
                - name: performance-test
                  trials:
                    - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                      selectionMode:
                        type: featureFlag
                      control:
                        key: control
                        implementationType: "{typeof(TestServiceA).AssemblyQualifiedName}"
                  hypothesis:
                    name: latency-hypothesis
                    type: superiority
                    nullHypothesis: "No difference in latency"
                    alternativeHypothesis: "New implementation has lower latency"
                    primaryEndpoint:
                      name: response_time_ms
                      outcomeType: continuous
                      lowerIsBetter: true
                    expectedEffectSize: 0.2
                    successCriteria:
                      alpha: 0.05
                      power: 0.80
            """;

        var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
        File.WriteAllText(yamlPath, yaml);

        // Act
        var result = _loader.LoadFromFile(yamlPath);

        // Assert
        Assert.NotNull(result.Experiments);
        Assert.Single(result.Experiments);
        Assert.NotNull(result.Experiments[0].Hypothesis);
        Assert.Equal("latency-hypothesis", result.Experiments[0].Hypothesis!.Name);
        Assert.Equal("superiority", result.Experiments[0].Hypothesis!.Type);
        Assert.NotNull(result.Experiments[0].Hypothesis!.PrimaryEndpoint);
        Assert.Equal("response_time_ms", result.Experiments[0].Hypothesis!.PrimaryEndpoint.Name);
    }

    [Fact]
    public void LoadFromYaml_WithDecorators_ParsesCorrectly()
    {
        // Arrange
        var yaml = """
            experimentFramework:
              decorators:
                - type: logging
                  options:
                    benchmarks: true
                    errorLogging: true
                - type: timeout
                  options:
                    timeout: "00:00:30"
            """;

        var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
        File.WriteAllText(yamlPath, yaml);

        // Act
        var result = _loader.LoadFromFile(yamlPath);

        // Assert
        Assert.NotNull(result.Decorators);
        Assert.Equal(2, result.Decorators.Count);
        Assert.Equal("logging", result.Decorators[0].Type);
        Assert.Equal("timeout", result.Decorators[1].Type);
    }
}
