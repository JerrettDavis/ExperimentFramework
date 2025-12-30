using ExperimentFramework.Governance.Policy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ExperimentFramework.Governance.Tests;

public class PolicyEvaluatorTests
{
    private readonly ILogger<PolicyEvaluator> _logger;
    private readonly PolicyEvaluator _sut;

    public PolicyEvaluatorTests()
    {
        _logger = Substitute.For<ILogger<PolicyEvaluator>>();
        _sut = new PolicyEvaluator(_logger);
    }

    [Fact]
    public async Task EvaluateAllAsync_ReturnsEmpty_WhenNoPoliciesRegistered()
    {
        // Arrange
        var context = new PolicyContext
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Running
        };

        // Act
        var results = await _sut.EvaluateAllAsync(context);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAllAsync_EvaluatesAllRegisteredPolicies()
    {
        // Arrange
        _sut.RegisterPolicy(new TrafficLimitPolicy(10));
        _sut.RegisterPolicy(new ErrorRatePolicy(0.05));

        var context = new PolicyContext
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Running
        };

        // Act
        var results = await _sut.EvaluateAllAsync(context);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task AreAllCriticalPoliciesCompliantAsync_ReturnsTrue_WhenAllCompliant()
    {
        // Arrange
        _sut.RegisterPolicy(new TrafficLimitPolicy(50));

        var context = new PolicyContext
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Running,
            Telemetry = new Dictionary<string, object>
            {
                ["trafficPercentage"] = 30.0
            }
        };

        // Act
        var isCompliant = await _sut.AreAllCriticalPoliciesCompliantAsync(context);

        // Assert
        isCompliant.Should().BeTrue();
    }

    [Fact]
    public async Task AreAllCriticalPoliciesCompliantAsync_ReturnsFalse_WhenAnyCriticalViolated()
    {
        // Arrange
        _sut.RegisterPolicy(new TrafficLimitPolicy(10));

        var context = new PolicyContext
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Running,
            Telemetry = new Dictionary<string, object>
            {
                ["trafficPercentage"] = 50.0
            }
        };

        // Act
        var isCompliant = await _sut.AreAllCriticalPoliciesCompliantAsync(context);

        // Assert
        isCompliant.Should().BeFalse();
    }
}

public class CommonPoliciesTests
{
    [Theory]
    [InlineData(10.0, 5.0, true)]
    [InlineData(10.0, 10.0, true)]
    [InlineData(10.0, 15.0, false)]
    public async Task TrafficLimitPolicy_EvaluatesCorrectly(double limit, double actual, bool expectedCompliant)
    {
        // Arrange
        var policy = new TrafficLimitPolicy(limit);
        var context = new PolicyContext
        {
            ExperimentName = "test",
            Telemetry = new Dictionary<string, object>
            {
                ["trafficPercentage"] = actual
            }
        };

        // Act
        var result = await policy.EvaluateAsync(context);

        // Assert
        result.IsCompliant.Should().Be(expectedCompliant);
    }

    [Fact]
    public async Task TrafficLimitPolicy_AllowsExceedingLimit_WhenStableTimeRequirementMet()
    {
        // Arrange
        var policy = new TrafficLimitPolicy(10.0, TimeSpan.FromMinutes(30));
        var context = new PolicyContext
        {
            ExperimentName = "test",
            Telemetry = new Dictionary<string, object>
            {
                ["trafficPercentage"] = 50.0,
                ["runningDuration"] = TimeSpan.FromHours(1)
            }
        };

        // Act
        var result = await policy.EvaluateAsync(context);

        // Assert
        result.IsCompliant.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.05, 0.03, true)]
    [InlineData(0.05, 0.05, true)]
    [InlineData(0.05, 0.10, false)]
    public async Task ErrorRatePolicy_EvaluatesCorrectly(double limit, double actual, bool expectedCompliant)
    {
        // Arrange
        var policy = new ErrorRatePolicy(limit);
        var context = new PolicyContext
        {
            ExperimentName = "test",
            Telemetry = new Dictionary<string, object>
            {
                ["errorRate"] = actual
            }
        };

        // Act
        var result = await policy.EvaluateAsync(context);

        // Assert
        result.IsCompliant.Should().Be(expectedCompliant);
    }

    [Fact]
    public async Task ErrorRatePolicy_SetsCorrectSeverity_OnViolation()
    {
        // Arrange
        var policy = new ErrorRatePolicy(0.05);
        var context = new PolicyContext
        {
            ExperimentName = "test",
            Telemetry = new Dictionary<string, object>
            {
                ["errorRate"] = 0.10
            }
        };

        // Act
        var result = await policy.EvaluateAsync(context);

        // Assert
        result.Severity.Should().Be(PolicyViolationSeverity.Critical);
    }

    [Fact]
    public async Task TimeWindowPolicy_AllowsOperationsInWindow()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow.TimeOfDay;
        var start = now.Subtract(TimeSpan.FromHours(1));
        var end = now.Add(TimeSpan.FromHours(1));
        var policy = new TimeWindowPolicy(start, end);

        var context = new PolicyContext
        {
            ExperimentName = "test"
        };

        // Act
        var result = await policy.EvaluateAsync(context);

        // Assert
        result.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public async Task TimeWindowPolicy_BlocksOperationsOutsideWindow()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow.TimeOfDay;
        var start = now.Add(TimeSpan.FromHours(2));
        var end = now.Add(TimeSpan.FromHours(3));
        var policy = new TimeWindowPolicy(start, end);

        var context = new PolicyContext
        {
            ExperimentName = "test"
        };

        // Act
        var result = await policy.EvaluateAsync(context);

        // Assert
        result.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public async Task ConflictPreventionPolicy_DetectsConflicts()
    {
        // Arrange
        var policy = new ConflictPreventionPolicy("experiment-A", "experiment-B");
        var context = new PolicyContext
        {
            ExperimentName = "new-experiment",
            Metadata = new Dictionary<string, object>
            {
                ["runningExperiments"] = new[] { "experiment-A", "experiment-C" }
            }
        };

        // Act
        var result = await policy.EvaluateAsync(context);

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.Reason.Should().Contain("experiment-A");
    }

    [Fact]
    public async Task ConflictPreventionPolicy_PassesWhenNoConflicts()
    {
        // Arrange
        var policy = new ConflictPreventionPolicy("experiment-A", "experiment-B");
        var context = new PolicyContext
        {
            ExperimentName = "new-experiment",
            Metadata = new Dictionary<string, object>
            {
                ["runningExperiments"] = new[] { "experiment-C", "experiment-D" }
            }
        };

        // Act
        var result = await policy.EvaluateAsync(context);

        // Assert
        result.IsCompliant.Should().BeTrue();
    }
}
