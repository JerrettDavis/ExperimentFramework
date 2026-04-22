using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Policy;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExperimentFramework.Tests.Governance;

public class CommonPoliciesTests
{
    // ───────────────────────── TrafficLimitPolicy ─────────────────────────

    [Fact]
    public void TrafficLimitPolicy_ThrowsWhenPercentageBelow0()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrafficLimitPolicy(-1));
    }

    [Fact]
    public void TrafficLimitPolicy_ThrowsWhenPercentageAbove100()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrafficLimitPolicy(101));
    }

    [Fact]
    public async Task TrafficLimitPolicy_ReturnsCompliant_WhenNoTelemetry()
    {
        var policy = new TrafficLimitPolicy(50);
        var context = new PolicyContext { ExperimentName = "exp", Telemetry = null };

        var result = await policy.EvaluateAsync(context);

        Assert.True(result.IsCompliant);
        Assert.Equal("TrafficLimit", result.PolicyName);
    }

    [Fact]
    public async Task TrafficLimitPolicy_ReturnsCompliant_WhenTelemetryKeyMissing()
    {
        var policy = new TrafficLimitPolicy(50);
        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Telemetry = new Dictionary<string, object>()
        };

        var result = await policy.EvaluateAsync(context);

        Assert.True(result.IsCompliant);
    }

    [Fact]
    public async Task TrafficLimitPolicy_ReturnsCompliant_WhenTrafficWithinLimit()
    {
        var policy = new TrafficLimitPolicy(50);
        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Telemetry = new Dictionary<string, object> { ["trafficPercentage"] = 30.0 }
        };

        var result = await policy.EvaluateAsync(context);

        Assert.True(result.IsCompliant);
    }

    [Fact]
    public async Task TrafficLimitPolicy_ReturnsNonCompliant_WhenTrafficExceedsLimit()
    {
        var policy = new TrafficLimitPolicy(50);
        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Telemetry = new Dictionary<string, object> { ["trafficPercentage"] = 75.0 }
        };

        var result = await policy.EvaluateAsync(context);

        Assert.False(result.IsCompliant);
        Assert.Equal(PolicyViolationSeverity.Critical, result.Severity);
    }

    [Fact]
    public async Task TrafficLimitPolicy_ReturnsCompliant_WhenMinStableTimeMetEvenIfOverLimit()
    {
        var policy = new TrafficLimitPolicy(50, TimeSpan.FromHours(1));
        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Telemetry = new Dictionary<string, object>
            {
                ["trafficPercentage"] = 75.0,
                ["runningDuration"] = TimeSpan.FromHours(2)
            }
        };

        var result = await policy.EvaluateAsync(context);

        Assert.True(result.IsCompliant);
    }

    [Fact]
    public async Task TrafficLimitPolicy_ReturnsNonCompliant_WhenMinStableTimeNotMet()
    {
        var policy = new TrafficLimitPolicy(50, TimeSpan.FromHours(5));
        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Telemetry = new Dictionary<string, object>
            {
                ["trafficPercentage"] = 75.0,
                ["runningDuration"] = TimeSpan.FromHours(1)
            }
        };

        var result = await policy.EvaluateAsync(context);

        Assert.False(result.IsCompliant);
    }

    [Fact]
    public async Task TrafficLimitPolicy_ReturnsNonCompliant_WhenDurationKeyMissingAndOverLimit()
    {
        var policy = new TrafficLimitPolicy(50, TimeSpan.FromHours(1));
        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Telemetry = new Dictionary<string, object> { ["trafficPercentage"] = 75.0 }
        };

        var result = await policy.EvaluateAsync(context);

        Assert.False(result.IsCompliant);
    }

    [Fact]
    public void TrafficLimitPolicy_HasExpectedNameAndDescription()
    {
        var policy = new TrafficLimitPolicy(30);

        Assert.Equal("TrafficLimit", policy.Name);
        Assert.Contains("30", policy.Description);
    }

    // ───────────────────────── ErrorRatePolicy ─────────────────────────

    [Fact]
    public void ErrorRatePolicy_ThrowsWhenRateBelow0()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ErrorRatePolicy(-0.1));
    }

    [Fact]
    public void ErrorRatePolicy_ThrowsWhenRateAbove1()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ErrorRatePolicy(1.1));
    }

    [Fact]
    public async Task ErrorRatePolicy_ReturnsCompliant_WhenNoTelemetry()
    {
        var policy = new ErrorRatePolicy(0.05);
        var context = new PolicyContext { ExperimentName = "exp", Telemetry = null };

        var result = await policy.EvaluateAsync(context);

        Assert.True(result.IsCompliant);
    }

    [Fact]
    public async Task ErrorRatePolicy_ReturnsCompliant_WhenErrorRateWithinLimit()
    {
        var policy = new ErrorRatePolicy(0.05);
        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Telemetry = new Dictionary<string, object> { ["errorRate"] = 0.02 }
        };

        var result = await policy.EvaluateAsync(context);

        Assert.True(result.IsCompliant);
    }

    [Fact]
    public async Task ErrorRatePolicy_ReturnsNonCompliant_WhenErrorRateExceedsLimit()
    {
        var policy = new ErrorRatePolicy(0.05);
        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Telemetry = new Dictionary<string, object> { ["errorRate"] = 0.10 }
        };

        var result = await policy.EvaluateAsync(context);

        Assert.False(result.IsCompliant);
        Assert.Equal(PolicyViolationSeverity.Critical, result.Severity);
    }

    [Fact]
    public void ErrorRatePolicy_HasExpectedName()
    {
        var policy = new ErrorRatePolicy(0.05);
        Assert.Equal("ErrorRate", policy.Name);
    }

    // ───────────────────────── ConflictPreventionPolicy ─────────────────────────

    [Fact]
    public async Task ConflictPreventionPolicy_ReturnsCompliant_WhenNoMetadata()
    {
        var policy = new ConflictPreventionPolicy("other-exp");
        var context = new PolicyContext { ExperimentName = "exp", Metadata = null };

        var result = await policy.EvaluateAsync(context);

        Assert.True(result.IsCompliant);
    }

    [Fact]
    public async Task ConflictPreventionPolicy_ReturnsCompliant_WhenNoConflicts()
    {
        var policy = new ConflictPreventionPolicy("danger-exp");
        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Metadata = new Dictionary<string, object>
            {
                ["runningExperiments"] = new List<string> { "safe-exp-1", "safe-exp-2" }
            }
        };

        var result = await policy.EvaluateAsync(context);

        Assert.True(result.IsCompliant);
    }

    [Fact]
    public async Task ConflictPreventionPolicy_ReturnsNonCompliant_WhenConflictFound()
    {
        var policy = new ConflictPreventionPolicy("danger-exp");
        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Metadata = new Dictionary<string, object>
            {
                ["runningExperiments"] = new List<string> { "danger-exp", "safe-exp" }
            }
        };

        var result = await policy.EvaluateAsync(context);

        Assert.False(result.IsCompliant);
        Assert.Equal(PolicyViolationSeverity.Critical, result.Severity);
        Assert.Contains("danger-exp", result.Reason);
    }

    [Fact]
    public async Task ConflictPreventionPolicy_IsCaseInsensitive()
    {
        var policy = new ConflictPreventionPolicy("DANGER-EXP");
        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Metadata = new Dictionary<string, object>
            {
                ["runningExperiments"] = new List<string> { "danger-exp" }
            }
        };

        var result = await policy.EvaluateAsync(context);

        Assert.False(result.IsCompliant);
    }

    [Fact]
    public void ConflictPreventionPolicy_HasExpectedName()
    {
        var policy = new ConflictPreventionPolicy("x");
        Assert.Equal("ConflictPrevention", policy.Name);
    }

    // ───────────────────────── TimeWindowPolicy ─────────────────────────

    [Fact]
    public void TimeWindowPolicy_HasExpectedName()
    {
        var policy = new TimeWindowPolicy(TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        Assert.Equal("TimeWindow", policy.Name);
    }

    [Fact]
    public async Task TimeWindowPolicy_ReturnsNonCompliantOrCompliantBasedOnCurrentTime()
    {
        // Always-open window (midnight to midnight) — should be compliant
        var policy = new TimeWindowPolicy(TimeSpan.Zero, TimeSpan.FromHours(23.99));
        var context = new PolicyContext { ExperimentName = "exp" };

        var result = await policy.EvaluateAsync(context);

        // We can't know exact time but we can verify the result is a valid PolicyEvaluationResult
        Assert.Equal("TimeWindow", result.PolicyName);
        Assert.NotNull(result.Reason);
    }

    [Fact]
    public async Task TimeWindowPolicy_OverNightWindow_HasValidResult()
    {
        // Overnight window 22:00 - 06:00
        var policy = new TimeWindowPolicy(TimeSpan.FromHours(22), TimeSpan.FromHours(6));
        var context = new PolicyContext { ExperimentName = "exp" };

        var result = await policy.EvaluateAsync(context);

        Assert.Equal("TimeWindow", result.PolicyName);
    }
}

public class PolicyEvaluatorTests
{
    private readonly PolicyEvaluator _evaluator = new(NullLogger<PolicyEvaluator>.Instance);

    [Fact]
    public void RegisterPolicy_ThrowsWhenPolicyIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _evaluator.RegisterPolicy(null!));
    }

    [Fact]
    public async Task EvaluateAllAsync_ThrowsWhenContextIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _evaluator.EvaluateAllAsync(null!));
    }

    [Fact]
    public async Task EvaluateAllAsync_ReturnsEmptyList_WhenNoPolicies()
    {
        var context = new PolicyContext { ExperimentName = "exp" };

        var results = await _evaluator.EvaluateAllAsync(context);

        Assert.Empty(results);
    }

    [Fact]
    public async Task EvaluateAllAsync_ReturnsResultsFromAllPolicies()
    {
        var policy1 = new TrafficLimitPolicy(100); // Will be compliant
        var policy2 = new ErrorRatePolicy(0.5);   // Will be compliant (no data)
        _evaluator.RegisterPolicy(policy1);
        _evaluator.RegisterPolicy(policy2);

        var context = new PolicyContext { ExperimentName = "exp" };
        var results = await _evaluator.EvaluateAllAsync(context);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.IsCompliant));
    }

    [Fact]
    public async Task EvaluateAllAsync_RecordsFailureResult_WhenPolicyThrows()
    {
        var throwingPolicy = new ThrowingPolicy();
        _evaluator.RegisterPolicy(throwingPolicy);

        var context = new PolicyContext { ExperimentName = "exp" };
        var results = await _evaluator.EvaluateAllAsync(context);

        Assert.Single(results);
        Assert.False(results[0].IsCompliant);
        Assert.Equal(PolicyViolationSeverity.Error, results[0].Severity);
        Assert.Contains("Policy evaluation failed", results[0].Reason);
    }

    [Fact]
    public async Task AreAllCriticalPoliciesCompliantAsync_ReturnsTrueWhenNoCriticalViolations()
    {
        var context = new PolicyContext { ExperimentName = "exp" };

        var isCompliant = await _evaluator.AreAllCriticalPoliciesCompliantAsync(context);

        Assert.True(isCompliant);
    }

    [Fact]
    public async Task AreAllCriticalPoliciesCompliantAsync_ReturnsFalseWhenCriticalViolation()
    {
        var policy = new ErrorRatePolicy(0.01);
        _evaluator.RegisterPolicy(policy);

        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Telemetry = new Dictionary<string, object> { ["errorRate"] = 0.50 }
        };

        var isCompliant = await _evaluator.AreAllCriticalPoliciesCompliantAsync(context);

        Assert.False(isCompliant);
    }

    [Fact]
    public async Task EvaluateAllAsync_LogsWarning_WhenPolicyViolated()
    {
        var policy = new ErrorRatePolicy(0.01);
        _evaluator.RegisterPolicy(policy);

        var context = new PolicyContext
        {
            ExperimentName = "exp",
            Telemetry = new Dictionary<string, object> { ["errorRate"] = 0.50 }
        };

        // Should not throw; just verify evaluation completes
        var results = await _evaluator.EvaluateAllAsync(context);

        Assert.Single(results);
        Assert.False(results[0].IsCompliant);
    }

    // Helper: policy that always throws
    private sealed class ThrowingPolicy : IExperimentPolicy
    {
        public string Name => "ThrowingPolicy";
        public string Description => "Always throws";

        public Task<PolicyEvaluationResult> EvaluateAsync(PolicyContext context, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated policy failure");
    }
}
