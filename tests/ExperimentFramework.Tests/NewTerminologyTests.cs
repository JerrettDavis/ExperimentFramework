using ExperimentFramework.Activation;
using ExperimentFramework.Models;
using ExperimentFramework.Selection;
using ExperimentFramework.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Tests;

/// <summary>
/// Tests for the new terminology API (AddControl, AddCondition, AddVariant, etc.)
/// </summary>
public class NewTerminologyTests
{
    #region Test Services

    public interface ITestService
    {
        string GetValue();
    }

    public class ControlImplementation : ITestService
    {
        public string GetValue() => "control";
    }

    public class ConditionAImplementation : ITestService
    {
        public string GetValue() => "condition-a";
    }

    public class ConditionBImplementation : ITestService
    {
        public string GetValue() => "condition-b";
    }

    public interface IAnotherService
    {
        int Calculate();
    }

    public class DefaultCalculator : IAnotherService
    {
        public int Calculate() => 42;
    }

    public class FastCalculator : IAnotherService
    {
        public int Calculate() => 100;
    }

    #endregion

    #region AddControl/AddCondition/AddVariant Tests

    [Fact]
    public void AddControl_WithoutKey_RegistersServiceCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ControlImplementation>();
        services.AddScoped<ConditionAImplementation>();
        services.AddScoped<ITestService, ControlImplementation>(); // Required before AddExperimentFramework

        var inMemoryConfig = new Dictionary<string, string?> { ["test"] = "control" };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<ITestService>(t => t
                .UsingConfigurationKey("test")
                .AddControl<ControlImplementation>()
                .AddCondition<ConditionAImplementation>("a"))
            .UseDispatchProxy();

        // Act
        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<ITestService>();

        // Assert
        Assert.Equal("control", service.GetValue());
    }

    [Fact]
    public void AddCondition_SelectsCorrectImplementation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ControlImplementation>();
        services.AddScoped<ConditionAImplementation>();
        services.AddScoped<ITestService, ControlImplementation>(); // Required before AddExperimentFramework

        var inMemoryConfig = new Dictionary<string, string?> { ["test"] = "a" };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<ITestService>(t => t
                .UsingConfigurationKey("test")
                .AddControl<ControlImplementation>()
                .AddCondition<ConditionAImplementation>("a"))
            .UseDispatchProxy();

        // Act
        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<ITestService>();

        // Assert
        Assert.Equal("condition-a", service.GetValue());
    }

    [Fact]
    public void AddVariant_IsEquivalentToAddCondition()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ControlImplementation>();
        services.AddScoped<ConditionAImplementation>();
        services.AddScoped<ITestService, ControlImplementation>(); // Required before AddExperimentFramework

        var inMemoryConfig = new Dictionary<string, string?> { ["test"] = "a" };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<ITestService>(t => t
                .UsingConfigurationKey("test")
                .AddControl<ControlImplementation>()
                .AddVariant<ConditionAImplementation>("a")) // Using AddVariant instead of AddCondition
            .UseDispatchProxy();

        // Act
        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<ITestService>();

        // Assert
        Assert.Equal("condition-a", service.GetValue());
    }

    [Fact]
    public void Trial_MethodWorks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ControlImplementation>();
        services.AddScoped<ITestService, ControlImplementation>(); // Required before AddExperimentFramework

        var inMemoryConfig = new Dictionary<string, string?> { ["test"] = "control" };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Use Trial<T> instead of Define<T>
        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<ITestService>(t => t
                .UsingConfigurationKey("test")
                .AddControl<ControlImplementation>())
            .UseDispatchProxy();

        // Act
        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<ITestService>();

        // Assert
        Assert.Equal("control", service.GetValue());
    }

    #endregion

    #region Named Experiment Tests

    [Fact]
    public void Experiment_RegistersMultipleServicesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ControlImplementation>();
        services.AddScoped<ConditionAImplementation>();
        services.AddScoped<DefaultCalculator>();
        services.AddScoped<FastCalculator>();
        services.AddScoped<ITestService, ControlImplementation>(); // Required before AddExperimentFramework
        services.AddScoped<IAnotherService, DefaultCalculator>(); // Required before AddExperimentFramework

        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["test1"] = "control",
            ["test2"] = "default"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        var builder = ExperimentFrameworkBuilder.Create()
            .Experiment("q1-migration", exp => exp
                .Trial<ITestService>(t => t
                    .UsingConfigurationKey("test1")
                    .AddControl<ControlImplementation>()
                    .AddCondition<ConditionAImplementation>("a"))
                .Trial<IAnotherService>(t => t
                    .UsingConfigurationKey("test2")
                    .AddControl<DefaultCalculator>("default")
                    .AddCondition<FastCalculator>("fast")))
            .UseDispatchProxy();

        // Act
        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();
        var testService = sp.GetRequiredService<ITestService>();
        var anotherService = sp.GetRequiredService<IAnotherService>();

        // Assert
        Assert.Equal("control", testService.GetValue());
        Assert.Equal(42, anotherService.Calculate());
    }

    #endregion

    #region ActivationEvaluator Tests

    [Fact]
    public void ActivationEvaluator_ReturnsTrue_WhenNoConstraints()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(services);
        var registration = new ExperimentRegistration
        {
            ServiceType = typeof(ITestService),
            Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
            SelectorName = "test",
            DefaultKey = "control",
            Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
            OnErrorPolicy = OnErrorPolicy.Throw
        };

        // Act
        var result = evaluator.IsActive(registration);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ActivationEvaluator_ReturnsFalse_BeforeStartTime()
    {
        // Arrange
        var futureTime = DateTimeOffset.UtcNow.AddDays(1);
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(services);
        var registration = new ExperimentRegistration
        {
            ServiceType = typeof(ITestService),
            Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
            SelectorName = "test",
            DefaultKey = "control",
            Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
            OnErrorPolicy = OnErrorPolicy.Throw,
            StartTime = futureTime
        };

        // Act
        var result = evaluator.IsActive(registration);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ActivationEvaluator_ReturnsFalse_AfterEndTime()
    {
        // Arrange
        var pastTime = DateTimeOffset.UtcNow.AddDays(-1);
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(services);
        var registration = new ExperimentRegistration
        {
            ServiceType = typeof(ITestService),
            Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
            SelectorName = "test",
            DefaultKey = "control",
            Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
            OnErrorPolicy = OnErrorPolicy.Throw,
            EndTime = pastTime
        };

        // Act
        var result = evaluator.IsActive(registration);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ActivationEvaluator_ReturnsTrue_WithinTimeWindow()
    {
        // Arrange
        var pastTime = DateTimeOffset.UtcNow.AddDays(-1);
        var futureTime = DateTimeOffset.UtcNow.AddDays(1);
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(services);
        var registration = new ExperimentRegistration
        {
            ServiceType = typeof(ITestService),
            Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
            SelectorName = "test",
            DefaultKey = "control",
            Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
            OnErrorPolicy = OnErrorPolicy.Throw,
            StartTime = pastTime,
            EndTime = futureTime
        };

        // Act
        var result = evaluator.IsActive(registration);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ActivationEvaluator_ReturnsFalse_WhenPredicateReturnsFalse()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(services);
        var registration = new ExperimentRegistration
        {
            ServiceType = typeof(ITestService),
            Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
            SelectorName = "test",
            DefaultKey = "control",
            Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
            OnErrorPolicy = OnErrorPolicy.Throw,
            ActivationPredicate = _ => false
        };

        // Act
        var result = evaluator.IsActive(registration);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ActivationEvaluator_ReturnsTrue_WhenPredicateReturnsTrue()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(services);
        var registration = new ExperimentRegistration
        {
            ServiceType = typeof(ITestService),
            Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
            SelectorName = "test",
            DefaultKey = "control",
            Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
            OnErrorPolicy = OnErrorPolicy.Throw,
            ActivationPredicate = _ => true
        };

        // Act
        var result = evaluator.IsActive(registration);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ActivationEvaluator_ReturnsFalse_WhenPredicateThrows()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(services);
        var registration = new ExperimentRegistration
        {
            ServiceType = typeof(ITestService),
            Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
            SelectorName = "test",
            DefaultKey = "control",
            Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
            OnErrorPolicy = OnErrorPolicy.Throw,
            ActivationPredicate = _ => throw new InvalidOperationException("Test exception")
        };

        // Act
        var result = evaluator.IsActive(registration);

        // Assert - should return false for safety when predicate throws
        Assert.False(result);
    }

    [Fact]
    public void ActivationEvaluator_UsesCustomTimeProvider()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(timeProvider, services);

        var startTime = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2025, 6, 30, 23, 59, 59, TimeSpan.Zero);

        var registration = new ExperimentRegistration
        {
            ServiceType = typeof(ITestService),
            Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
            SelectorName = "test",
            DefaultKey = "control",
            Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
            OnErrorPolicy = OnErrorPolicy.Throw,
            StartTime = startTime,
            EndTime = endTime
        };

        // Act
        var result = evaluator.IsActive(registration);

        // Assert
        Assert.True(result);
    }

    private class FakeTimeProvider(DateTimeOffset fixedTime) : IExperimentTimeProvider
    {
        public DateTimeOffset UtcNow => fixedTime;
    }

    #endregion

    #region Conflict Detection Tests

    [Fact]
    public void ConflictDetector_DetectsInvalidFallbackKey()
    {
        // Arrange
        var detector = new TrialConflictDetector();
        var registrations = new[]
        {
            new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
                SelectorName = "test",
                DefaultKey = "control",
                Trials = new Dictionary<string, Type>
                {
                    ["control"] = typeof(ControlImplementation),
                    ["a"] = typeof(ConditionAImplementation)
                },
                OnErrorPolicy = OnErrorPolicy.RedirectAndReplay,
                FallbackTrialKey = "nonexistent" // Invalid key
            }
        };

        // Act
        var conflicts = detector.DetectConflicts(registrations);

        // Assert
        Assert.Single(conflicts);
        Assert.Equal(TrialConflictType.InvalidFallbackKey, conflicts[0].Type);
    }

    [Fact]
    public void ConflictDetector_DetectsInvalidOrderedFallbackKeys()
    {
        // Arrange
        var detector = new TrialConflictDetector();
        var registrations = new[]
        {
            new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
                SelectorName = "test",
                DefaultKey = "control",
                Trials = new Dictionary<string, Type>
                {
                    ["control"] = typeof(ControlImplementation),
                    ["a"] = typeof(ConditionAImplementation)
                },
                OnErrorPolicy = OnErrorPolicy.RedirectAndReplayOrdered,
                OrderedFallbackKeys = ["a", "invalid"] // "invalid" doesn't exist
            }
        };

        // Act
        var conflicts = detector.DetectConflicts(registrations);

        // Assert
        Assert.Single(conflicts);
        Assert.Equal(TrialConflictType.InvalidFallbackKey, conflicts[0].Type);
    }

    [Fact]
    public void ConflictDetector_DetectsDuplicateRegistrationsWithoutTimeBounds()
    {
        // Arrange
        var detector = new TrialConflictDetector();
        var registrations = new[]
        {
            new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
                SelectorName = "test1",
                DefaultKey = "control",
                Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
                OnErrorPolicy = OnErrorPolicy.Throw
            },
            new ExperimentRegistration
            {
                ServiceType = typeof(ITestService), // Same service type
                Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
                SelectorName = "test2",
                DefaultKey = "control",
                Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
                OnErrorPolicy = OnErrorPolicy.Throw
            }
        };

        // Act
        var conflicts = detector.DetectConflicts(registrations);

        // Assert
        Assert.Single(conflicts);
        Assert.Equal(TrialConflictType.DuplicateServiceRegistration, conflicts[0].Type);
    }

    [Fact]
    public void ConflictDetector_DetectsOverlappingTimeWindows()
    {
        // Arrange
        var detector = new TrialConflictDetector();
        var registrations = new[]
        {
            new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
                SelectorName = "test1",
                DefaultKey = "control",
                Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
                OnErrorPolicy = OnErrorPolicy.Throw,
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow.AddDays(10)
            },
            new ExperimentRegistration
            {
                ServiceType = typeof(ITestService), // Same service type
                Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
                SelectorName = "test2",
                DefaultKey = "control",
                Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
                OnErrorPolicy = OnErrorPolicy.Throw,
                StartTime = DateTimeOffset.UtcNow.AddDays(5), // Overlaps with first
                EndTime = DateTimeOffset.UtcNow.AddDays(15)
            }
        };

        // Act
        var conflicts = detector.DetectConflicts(registrations);

        // Assert
        Assert.Single(conflicts);
        Assert.Equal(TrialConflictType.OverlappingTimeWindows, conflicts[0].Type);
    }

    [Fact]
    public void ConflictDetector_AllowsNonOverlappingTimeWindows()
    {
        // Arrange
        var detector = new TrialConflictDetector();
        var registrations = new[]
        {
            new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
                SelectorName = "test1",
                DefaultKey = "control",
                Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
                OnErrorPolicy = OnErrorPolicy.Throw,
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow.AddDays(10)
            },
            new ExperimentRegistration
            {
                ServiceType = typeof(ITestService), // Same service type
                Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
                SelectorName = "test2",
                DefaultKey = "control",
                Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
                OnErrorPolicy = OnErrorPolicy.Throw,
                StartTime = DateTimeOffset.UtcNow.AddDays(11), // Starts after first ends
                EndTime = DateTimeOffset.UtcNow.AddDays(20)
            }
        };

        // Act
        var conflicts = detector.DetectConflicts(registrations);

        // Assert
        Assert.Empty(conflicts);
    }

    [Fact]
    public void ConflictDetector_ValidateOrThrow_ThrowsOnConflict()
    {
        // Arrange
        var detector = new TrialConflictDetector();
        var registrations = new[]
        {
            new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
                SelectorName = "test",
                DefaultKey = "control",
                Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
                OnErrorPolicy = OnErrorPolicy.RedirectAndReplay,
                FallbackTrialKey = "invalid"
            }
        };

        // Act & Assert
        Assert.Throws<TrialConflictException>(() => detector.ValidateOrThrow(registrations));
    }

    [Fact]
    public void ConflictDetector_ValidateOrThrow_DoesNotThrowWhenNoConflicts()
    {
        // Arrange
        var detector = new TrialConflictDetector();
        var registrations = new[]
        {
            new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
                SelectorName = "test",
                DefaultKey = "control",
                Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
                OnErrorPolicy = OnErrorPolicy.Throw
            }
        };

        // Act & Assert - should not throw
        var exception = Record.Exception(() => detector.ValidateOrThrow(registrations));
        Assert.Null(exception);
    }

    #endregion

    #region Experiment Model Tests

    [Fact]
    public void Experiment_HasCorrectProperties()
    {
        // Arrange
        var trials = new List<Trial>
        {
            new()
            {
                ServiceType = typeof(ITestService),
                ControlKey = "control",
                ControlType = typeof(ControlImplementation),
                Conditions = new Dictionary<string, Type>(),
                SelectionRule = new SelectionRule
                {
                    Mode = SelectionMode.ConfigurationValue,
                    SelectorName = "test"
                },
                BehaviorRule = BehaviorRule.Default
            }
        };

        // Act
        var experiment = new Experiment
        {
            Name = "test-experiment",
            Trials = trials,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(30),
            ActivationPredicate = _ => true,
            Metadata = new Dictionary<string, object> { ["owner"] = "test-team" }
        };

        // Assert
        Assert.Equal("test-experiment", experiment.Name);
        Assert.Single(experiment.Trials);
        Assert.NotNull(experiment.StartTime);
        Assert.NotNull(experiment.EndTime);
        Assert.NotNull(experiment.ActivationPredicate);
        Assert.NotNull(experiment.Metadata);
        Assert.Equal("test-team", experiment.Metadata["owner"]);
    }

    [Fact]
    public void Trial_HasCorrectProperties()
    {
        // Arrange & Act
        var trial = new Trial
        {
            ServiceType = typeof(ITestService),
            ControlKey = "control",
            ControlType = typeof(ControlImplementation),
            Conditions = new Dictionary<string, Type>
            {
                ["a"] = typeof(ConditionAImplementation),
                ["b"] = typeof(ConditionBImplementation)
            },
            SelectionRule = new SelectionRule
            {
                Mode = SelectionMode.ConfigurationValue,
                SelectorName = "test"
            },
            BehaviorRule = BehaviorRule.Default
        };

        // Assert
        Assert.Equal(typeof(ITestService), trial.ServiceType);
        Assert.Equal("control", trial.ControlKey);
        Assert.Equal(typeof(ControlImplementation), trial.ControlType);
        Assert.Equal(2, trial.Conditions.Count);
        Assert.Equal(3, trial.AllImplementations.Count); // 2 conditions + 1 control
    }

    [Fact]
    public void SelectionRule_HasCorrectProperties()
    {
        // Arrange & Act
        var rule = new SelectionRule
        {
            Mode = SelectionMode.ConfigurationValue,
            SelectorName = "test-selector",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(30),
            ActivationPredicate = _ => true,
            PercentageAllocation = 50.0,
            UserSegments = ["beta-users"]
        };

        // Assert
        Assert.Equal(SelectionMode.ConfigurationValue, rule.Mode);
        Assert.Equal("test-selector", rule.SelectorName);
        Assert.NotNull(rule.StartTime);
        Assert.NotNull(rule.EndTime);
        Assert.NotNull(rule.ActivationPredicate);
        Assert.Equal(50.0, rule.PercentageAllocation);
        Assert.Single(rule.UserSegments!);
    }

    [Fact]
    public void BehaviorRule_HasCorrectDefaultValues()
    {
        // Arrange & Act
        var rule = BehaviorRule.Default;

        // Assert
        Assert.Equal(OnErrorPolicy.Throw, rule.OnErrorPolicy);
        Assert.Null(rule.FallbackConditionKey);
        Assert.Null(rule.OrderedFallbackKeys);
        Assert.Null(rule.Timeout);
        Assert.Equal(TimeoutAction.ThrowException, rule.TimeoutAction);
    }

    [Fact]
    public void BehaviorRule_FromRegistration_CreatesCorrectRule()
    {
        // Arrange & Act
        var rule = BehaviorRule.FromRegistration(
            OnErrorPolicy.RedirectAndReplayOrdered,
            "fallback",
            ["a", "b", "c"]);

        // Assert
        Assert.Equal(OnErrorPolicy.RedirectAndReplayOrdered, rule.OnErrorPolicy);
        Assert.Equal("fallback", rule.FallbackConditionKey);
        Assert.Equal(3, rule.OrderedFallbackKeys!.Count);
    }

    #endregion

    #region ExperimentRegistration Equivalent Property Tests

    [Fact]
    public void ExperimentRegistration_ControlKey_EquivalentToDefaultKey()
    {
        // Arrange
        var registration = new ExperimentRegistration
        {
            ServiceType = typeof(ITestService),
            Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
            SelectorName = "test",
            DefaultKey = "my-control",
            Trials = new Dictionary<string, Type> { ["my-control"] = typeof(ControlImplementation) },
            OnErrorPolicy = OnErrorPolicy.Throw
        };

        // Act & Assert
        Assert.Equal(registration.DefaultKey, registration.ControlKey);
        Assert.Equal("my-control", registration.ControlKey);
    }

    [Fact]
    public void ExperimentRegistration_Conditions_EquivalentToTrials()
    {
        // Arrange
        var trials = new Dictionary<string, Type>
        {
            ["control"] = typeof(ControlImplementation),
            ["a"] = typeof(ConditionAImplementation)
        };

        var registration = new ExperimentRegistration
        {
            ServiceType = typeof(ITestService),
            Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
            SelectorName = "test",
            DefaultKey = "control",
            Trials = trials,
            OnErrorPolicy = OnErrorPolicy.Throw
        };

        // Act & Assert
        Assert.Same(registration.Trials, registration.Conditions);
        Assert.Equal(2, registration.Conditions.Count);
    }

    [Fact]
    public void ExperimentRegistration_FallbackConditionKey_EquivalentToFallbackTrialKey()
    {
        // Arrange
        var registration = new ExperimentRegistration
        {
            ServiceType = typeof(ITestService),
            Mode = SelectionMode.ConfigurationValue,
                ModeIdentifier = SelectionModes.ConfigurationValue,
            SelectorName = "test",
            DefaultKey = "control",
            Trials = new Dictionary<string, Type> { ["control"] = typeof(ControlImplementation) },
            OnErrorPolicy = OnErrorPolicy.RedirectAndReplay,
            FallbackTrialKey = "my-fallback"
        };

        // Act & Assert
        Assert.Equal(registration.FallbackTrialKey, registration.FallbackConditionKey);
        Assert.Equal("my-fallback", registration.FallbackConditionKey);
    }

    #endregion
}
