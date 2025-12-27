using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;

namespace ExperimentFramework.Tests.Configuration;

public class ConfigurationValidatorTests
{
    private readonly ConfigurationValidator _validator = new();

    [Fact]
    public void Validate_EmptyConfig_ReturnsValid()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot();

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidTrial_ReturnsValid()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = "IMyService",
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = "MyService" }
                }
            ]
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MissingServiceType_ReturnsError()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = "",
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = "MyService" }
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
    public void Validate_InvalidSelectionMode_ReturnsError()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = "IMyService",
                    SelectionMode = new SelectionModeConfig { Type = "invalid" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = "MyService" }
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
    public void Validate_DuplicateConditionKeys_ReturnsError()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = "IMyService",
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = "MyService" },
                    Conditions =
                    [
                        new ConditionConfig { Key = "variant1", ImplementationType = "MyService1" },
                        new ConditionConfig { Key = "variant1", ImplementationType = "MyService2" } // Duplicate
                    ]
                }
            ]
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate"));
    }

    [Fact]
    public void Validate_InvalidActivationTimeRange_ReturnsError()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = "IMyService",
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = "MyService" },
                    Activation = new ActivationConfig
                    {
                        From = DateTimeOffset.Now.AddDays(1),
                        Until = DateTimeOffset.Now // Until is before From
                    }
                }
            ]
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("before"));
    }

    [Fact]
    public void Validate_ValidErrorPolicy_ReturnsValid()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = "IMyService",
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = "MyService" },
                    Conditions =
                    [
                        new ConditionConfig { Key = "variant1", ImplementationType = "MyService1" }
                    ],
                    ErrorPolicy = new ErrorPolicyConfig
                    {
                        Type = "fallbackTo",
                        FallbackKey = "control"
                    }
                }
            ]
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_TryInOrderWithMissingKeys_ReturnsWarning()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = "IMyService",
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig { Key = "control", ImplementationType = "MyService" },
                    ErrorPolicy = new ErrorPolicyConfig
                    {
                        Type = "tryInOrder",
                        FallbackKeys = ["nonexistent", "control"]
                    }
                }
            ]
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid); // Warnings don't invalidate
        Assert.Contains(result.Warnings, w => w.Message.Contains("nonexistent"));
    }

    [Fact]
    public void Validate_ValidExperiment_ReturnsValid()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Experiments =
            [
                new ExperimentConfig
                {
                    Name = "test-experiment",
                    Trials =
                    [
                        new TrialConfig
                        {
                            ServiceType = "IMyService",
                            SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                            Control = new ConditionConfig { Key = "control", ImplementationType = "MyService" }
                        }
                    ]
                }
            ]
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DuplicateExperimentNames_ReturnsError()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Experiments =
            [
                new ExperimentConfig
                {
                    Name = "test-experiment",
                    Trials =
                    [
                        new TrialConfig
                        {
                            ServiceType = "IMyService",
                            SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                            Control = new ConditionConfig { Key = "control", ImplementationType = "MyService" }
                        }
                    ]
                },
                new ExperimentConfig
                {
                    Name = "test-experiment", // Duplicate
                    Trials =
                    [
                        new TrialConfig
                        {
                            ServiceType = "IMyService2",
                            SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                            Control = new ConditionConfig { Key = "control", ImplementationType = "MyService2" }
                        }
                    ]
                }
            ]
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate"));
    }

    [Fact]
    public void Validate_CustomModeWithoutIdentifier_ReturnsError()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = "IMyService",
                    SelectionMode = new SelectionModeConfig { Type = "custom" }, // Missing modeIdentifier
                    Control = new ConditionConfig { Key = "control", ImplementationType = "MyService" }
                }
            ]
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.Contains("modeIdentifier"));
    }

    [Fact]
    public void Validate_InvalidDecoratorType_ReturnsError()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Decorators =
            [
                new DecoratorConfig { Type = "invalidDecorator" }
            ]
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.Contains("decorators"));
    }

    [Fact]
    public void Validate_CustomDecoratorWithoutTypeName_ReturnsError()
    {
        // Arrange
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Decorators =
            [
                new DecoratorConfig { Type = "custom" } // Missing typeName
            ]
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.Contains("typeName"));
    }
}
