using ExperimentFramework.Activation;
using ExperimentFramework.Models;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Tests;

/// <summary>
/// Tests for the Activation namespace including ActivationEvaluator and time providers.
/// </summary>
public sealed class ActivationTests
{
    private sealed class TestTimeProvider : IExperimentTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    }

    #region SystemTimeProvider Tests

    [Fact]
    public void SystemTimeProvider_returns_current_time()
    {
        var before = DateTimeOffset.UtcNow;
        var result = SystemTimeProvider.Instance.UtcNow;
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(result, before, after);
    }

    [Fact]
    public void SystemTimeProvider_is_singleton()
    {
        var instance1 = SystemTimeProvider.Instance;
        var instance2 = SystemTimeProvider.Instance;

        Assert.Same(instance1, instance2);
    }

    #endregion

    #region ActivationEvaluator Constructor Tests

    [Fact]
    public void ActivationEvaluator_throws_when_serviceprovider_null()
    {
        Assert.Throws<ArgumentNullException>(() => new ActivationEvaluator(null!));
    }

    [Fact]
    public void ActivationEvaluator_throws_when_timeprovider_null()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        Assert.Throws<ArgumentNullException>(() => new ActivationEvaluator(null!, services));
    }

    #endregion

    #region ActivationEvaluator with ExperimentRegistration Tests

    [Fact]
    public void ActivationEvaluator_IsActive_returns_true_when_no_constraints()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(services);

        var registration = CreateRegistration();

        Assert.True(evaluator.IsActive(registration));
    }

    [Fact]
    public void ActivationEvaluator_IsActive_returns_false_before_starttime()
    {
        var timeProvider = new TestTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(timeProvider, services);

        var registration = CreateRegistration(startTime: timeProvider.UtcNow.AddHours(1));

        Assert.False(evaluator.IsActive(registration));
    }

    [Fact]
    public void ActivationEvaluator_IsActive_returns_true_after_starttime()
    {
        var timeProvider = new TestTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(timeProvider, services);

        var registration = CreateRegistration(startTime: timeProvider.UtcNow.AddHours(-1));

        Assert.True(evaluator.IsActive(registration));
    }

    [Fact]
    public void ActivationEvaluator_IsActive_returns_false_after_endtime()
    {
        var timeProvider = new TestTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(timeProvider, services);

        var registration = CreateRegistration(endTime: timeProvider.UtcNow.AddHours(-1));

        Assert.False(evaluator.IsActive(registration));
    }

    [Fact]
    public void ActivationEvaluator_IsActive_returns_true_before_endtime()
    {
        var timeProvider = new TestTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(timeProvider, services);

        var registration = CreateRegistration(endTime: timeProvider.UtcNow.AddHours(1));

        Assert.True(evaluator.IsActive(registration));
    }

    [Fact]
    public void ActivationEvaluator_IsActive_returns_false_when_predicate_returns_false()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(services);

        var registration = CreateRegistration(activationPredicate: _ => false);

        Assert.False(evaluator.IsActive(registration));
    }

    [Fact]
    public void ActivationEvaluator_IsActive_returns_true_when_predicate_returns_true()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(services);

        var registration = CreateRegistration(activationPredicate: _ => true);

        Assert.True(evaluator.IsActive(registration));
    }

    [Fact]
    public void ActivationEvaluator_IsActive_returns_false_when_predicate_throws()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(services);

        var registration = CreateRegistration(
            activationPredicate: _ => throw new InvalidOperationException("Test error"));

        Assert.False(evaluator.IsActive(registration));
    }

    [Fact]
    public void ActivationEvaluator_IsActive_throws_when_registration_null()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(services);

        Assert.Throws<ArgumentNullException>(() => evaluator.IsActive((ExperimentRegistration)null!));
    }

    #endregion

    #region ActivationEvaluator with SelectionRule Tests

    [Fact]
    public void ActivationEvaluator_IsActive_with_SelectionRule()
    {
        var timeProvider = new TestTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(timeProvider, services);

        var rule = new SelectionRule
        {
            Mode = SelectionMode.BooleanFeatureFlag,
            SelectorName = "TestFeature",
            StartTime = timeProvider.UtcNow.AddHours(-1),
            EndTime = timeProvider.UtcNow.AddHours(1)
        };

        Assert.True(evaluator.IsActive(rule));
    }

    #endregion

    #region ActivationEvaluator with Experiment Tests

    [Fact]
    public void ActivationEvaluator_IsActive_with_Experiment()
    {
        var timeProvider = new TestTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        var services = new ServiceCollection().BuildServiceProvider();
        var evaluator = new ActivationEvaluator(timeProvider, services);

        var experiment = new Experiment
        {
            Name = "test-experiment",
            Trials = [],
            StartTime = timeProvider.UtcNow.AddHours(-1),
            EndTime = timeProvider.UtcNow.AddHours(1)
        };

        Assert.True(evaluator.IsActive(experiment));
    }

    #endregion

    #region Helper Methods

    private static ExperimentRegistration CreateRegistration(
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        Func<IServiceProvider, bool>? activationPredicate = null)
    {
        return new ExperimentRegistration
        {
            ServiceType = typeof(ITestService),
            Mode = SelectionMode.BooleanFeatureFlag,
            ModeIdentifier = "BooleanFeatureFlag",
            SelectorName = "TestFeature",
            Trials = new Dictionary<string, Type> { ["control"] = typeof(StableService) },
            DefaultKey = "control",
            OnErrorPolicy = OnErrorPolicy.Throw,
            StartTime = startTime,
            EndTime = endTime,
            ActivationPredicate = activationPredicate
        };
    }

    #endregion
}
