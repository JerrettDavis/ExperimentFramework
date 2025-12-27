using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Exceptions;
using ExperimentFramework.Configuration.Models;

namespace ExperimentFramework.Tests.Configuration;

public class ConfigurationExperimentBuilderTests
{
    private readonly ConfigurationExperimentBuilder _builder;

    public ConfigurationExperimentBuilderTests()
    {
        ITypeResolver typeResolver = new TypeResolver();
        _builder = new ConfigurationExperimentBuilder(typeResolver);
    }

    [Fact]
    public void Build_EmptyConfig_ReturnsBuilder()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot();

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithProxyStrategyDispatchProxy_ConfiguresBuilder()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Settings = new FrameworkSettingsConfig
            {
                ProxyStrategy = "dispatchProxy"
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
        // Builder is configured - we can verify by building the configuration
        var frameworkConfig = result.Build();
        Assert.True(frameworkConfig.UseRuntimeProxies);
    }

    [Fact]
    public void Build_WithProxyStrategySourceGenerators_ConfiguresBuilder()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Settings = new FrameworkSettingsConfig
            {
                ProxyStrategy = "sourceGenerators"
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
        var frameworkConfig = result.Build();
        Assert.False(frameworkConfig.UseRuntimeProxies);
    }

    [Fact]
    public void Build_WithLoggingDecorator_AddsDecorator()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Decorators =
            [
                new DecoratorConfig
                {
                    Type = "logging",
                    Options = new Dictionary<string, object>
                    {
                        ["benchmarks"] = true,
                        ["errorLogging"] = true
                    }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
        var frameworkConfig = result.Build();
        Assert.NotEmpty(frameworkConfig.DecoratorFactories);
    }

    [Fact]
    public void Build_WithTimeoutDecorator_AddsDecorator()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Decorators =
            [
                new DecoratorConfig
                {
                    Type = "timeout",
                    Options = new Dictionary<string, object>
                    {
                        ["timeout"] = "00:00:05",
                        ["onTimeout"] = "fallbackToDefault"
                    }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
        var frameworkConfig = result.Build();
        Assert.NotEmpty(frameworkConfig.DecoratorFactories);
    }

    [Fact]
    public void Build_WithCircuitBreakerDecorator_SkipsIfPackageNotLoaded()
    {
        // Arrange - Resilience package should be loaded in tests
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Decorators =
            [
                new DecoratorConfig
                {
                    Type = "circuitBreaker",
                    Options = new Dictionary<string, object>
                    {
                        ["failureThreshold"] = 5,
                        ["breakDuration"] = "00:00:30"
                    }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert - Should not throw, may or may not add decorator depending on package availability
        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithUnknownDecoratorType_SkipsDecorator()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Decorators =
            [
                new DecoratorConfig
                {
                    Type = "unknownType"
                }
            ]
        };

        // Act - Should not throw
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithValidTrial_AddsTrial()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag", FlagName = "TestFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! },
                    Conditions =
                    [
                        new ConditionConfig { Key = "variant", ImplementationType = typeof(TestServiceB).AssemblyQualifiedName! }
                    ]
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
        var frameworkConfig = result.Build();
        Assert.Single(frameworkConfig.Definitions);
    }

    [Fact]
    public void Build_WithTrialUsingConfigurationKey_AddsTrial()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "configurationKey", Key = "Test:Key" },
                    Control = new ConditionConfig { Key = "default", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
        var frameworkConfig = result.Build();
        Assert.Single(frameworkConfig.Definitions);
    }

    [Fact]
    public void Build_WithTrialUsingCustomMode_AddsTrial()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig
                    {
                        Type = "custom",
                        ModeIdentifier = "MyCustomMode",
                        SelectorName = "MySelectorName"
                    },
                    Control = new ConditionConfig { Key = "default", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
        var frameworkConfig = result.Build();
        Assert.Single(frameworkConfig.Definitions);
    }

    [Fact]
    public void Build_WithErrorPolicyThrow_ConfiguresPolicy()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! },
                    ErrorPolicy = new ErrorPolicyConfig { Type = "throw" }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
        var frameworkConfig = result.Build();
        Assert.Single(frameworkConfig.Definitions);
    }

    [Fact]
    public void Build_WithErrorPolicyFallbackToControl_ConfiguresPolicy()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! },
                    ErrorPolicy = new ErrorPolicyConfig { Type = "fallbackToControl" }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithErrorPolicyFallbackTo_ConfiguresPolicy()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! },
                    Conditions =
                    [
                        new ConditionConfig { Key = "fallback", ImplementationType = typeof(TestServiceB).AssemblyQualifiedName! }
                    ],
                    ErrorPolicy = new ErrorPolicyConfig { Type = "fallbackTo", FallbackKey = "fallback" }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithErrorPolicyTryInOrder_ConfiguresPolicy()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! },
                    Conditions =
                    [
                        new ConditionConfig { Key = "variant1", ImplementationType = typeof(TestServiceB).AssemblyQualifiedName! },
                        new ConditionConfig { Key = "variant2", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! }
                    ],
                    ErrorPolicy = new ErrorPolicyConfig { Type = "tryInOrder", FallbackKeys = ["variant1", "variant2"] }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithErrorPolicyTryAny_ConfiguresPolicy()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! },
                    ErrorPolicy = new ErrorPolicyConfig { Type = "tryAny" }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithActivationTimeRange_ConfiguresActivation()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! },
                    Activation = new ActivationConfig
                    {
                        From = now,
                        Until = now.AddDays(30)
                    }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithNamedExperiment_AddsExperiment()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Experiments =
            [
                new ExperimentConfig
                {
                    Name = "test-experiment",
                    Metadata = new Dictionary<string, object>
                    {
                        ["owner"] = "test-team",
                        ["ticket"] = "TEST-123"
                    },
                    Trials =
                    [
                        new TrialConfig
                        {
                            ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                            SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                            Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! }
                        }
                    ]
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
        var frameworkConfig = result.Build();
        Assert.Single(frameworkConfig.Definitions);
    }

    [Fact]
    public void Build_WithExperimentActivation_ConfiguresActivation()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Experiments =
            [
                new ExperimentConfig
                {
                    Name = "test-experiment",
                    Activation = new ActivationConfig
                    {
                        From = now,
                        Until = now.AddDays(30)
                    },
                    Trials =
                    [
                        new TrialConfig
                        {
                            ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                            SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                            Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! }
                        }
                    ]
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithInvalidServiceType_ThrowsException()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = "NonExistentType12345",
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = "AnotherNonExistentType" }
                }
            ]
        };

        // Act & Assert
        Assert.Throws<ExperimentConfigurationException>(() => _builder.Build(config));
    }

    [Fact]
    public void Build_WithInvalidImplementationType_ThrowsException()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = "NonExistentImplementation12345" }
                }
            ]
        };

        // Act & Assert
        Assert.Throws<ExperimentConfigurationException>(() => _builder.Build(config));
    }

    [Fact]
    public void Build_WithMultipleTrials_AddsAllTrials()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! }
                },
                new TrialConfig
                {
                    ServiceType = typeof(IAnotherTestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "configurationKey" },
                    Control = new ConditionConfig { Key = "default", ImplementationType = typeof(AnotherTestServiceA).AssemblyQualifiedName! }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
        var frameworkConfig = result.Build();
        Assert.Equal(2, frameworkConfig.Definitions.Length);
    }

    [Fact]
    public void Build_WithMultipleConditions_AddsAllConditions()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! },
                    Conditions =
                    [
                        new ConditionConfig { Key = "variant1", ImplementationType = typeof(TestServiceB).AssemblyQualifiedName! },
                        new ConditionConfig { Key = "variant2", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! },
                        new ConditionConfig { Key = "variant3", ImplementationType = typeof(TestServiceB).AssemblyQualifiedName! }
                    ]
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
        var frameworkConfig = result.Build();
        Assert.Single(frameworkConfig.Definitions);
        // Verify the service type is correct (control + 3 conditions are configured internally)
        Assert.Equal(typeof(ITestService), frameworkConfig.Definitions[0].ServiceType);
    }

    [Fact]
    public void MergeInto_AddsTrialsToExistingBuilder()
    {
        // Arrange
        var existingBuilder = ExperimentFrameworkBuilder.Create()
            .UseDispatchProxy();

        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! }
                }
            ]
        };

        // Act
        _builder.MergeInto(existingBuilder, config);

        // Assert
        var frameworkConfig = existingBuilder.Build();
        Assert.True(frameworkConfig.UseRuntimeProxies); // Original setting preserved
        Assert.Single(frameworkConfig.Definitions); // New trial added
    }

    [Fact]
    public void MergeInto_AddsDecoratorsToExistingBuilder()
    {
        // Arrange
        var existingBuilder = ExperimentFrameworkBuilder.Create();

        var config = new ExperimentFrameworkConfigurationRoot
        {
            Decorators =
            [
                new DecoratorConfig
                {
                    Type = "logging",
                    Options = new Dictionary<string, object>
                    {
                        ["benchmarks"] = true
                    }
                }
            ]
        };

        // Act
        _builder.MergeInto(existingBuilder, config);

        // Assert
        var frameworkConfig = existingBuilder.Build();
        Assert.NotEmpty(frameworkConfig.DecoratorFactories);
    }

    [Fact]
    public void Build_WithSelectionModeVariantFeatureFlag_AddsCustomMode()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig
                    {
                        Type = "variantFeatureFlag",
                        FlagName = "MyVariantFlag"
                    },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithSelectionModeOpenFeature_AddsCustomMode()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig
                    {
                        Type = "openFeature",
                        FlagKey = "my-flag-key"
                    },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithSelectionModeStickyRouting_AddsCustomMode()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = typeof(ITestService).AssemblyQualifiedName!,
                    SelectionMode = new SelectionModeConfig
                    {
                        Type = "stickyRouting",
                        SelectorName = "MyStickySelector"
                    },
                    Control = new ConditionConfig { Key = "control", ImplementationType = typeof(TestServiceA).AssemblyQualifiedName! }
                }
            ]
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        Assert.NotNull(result);
    }

    // Test interfaces and implementations for the tests
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

    public interface IAnotherTestService
    {
        int Calculate();
    }

    public class AnotherTestServiceA : IAnotherTestService
    {
        public int Calculate() => 42;
    }
}
