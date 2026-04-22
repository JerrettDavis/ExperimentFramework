using ExperimentFramework.Governance.Policy;

namespace ExperimentFramework.DashboardHost.Demo;

/// <summary>
/// Minimal demo implementations of the three named policies referenced in governance seed data.
/// These are code-defined IExperimentPolicy instances — there is no string-based policy
/// registry in the framework. Policies are registered at startup via GovernanceBuilder.WithPolicy().
/// </summary>
/// <remarks>
/// Policy registration finding: GovernanceBuilder.WithPolicy(IExperimentPolicy) is the correct
/// registration path. There is no ExperimentFrameworkBuilder method for policies. These three
/// demo policies always pass (IsCompliant = true) except min-sample-size-1000 which checks the
/// "sampleCount" telemetry key and fails when it is below 1000 — matching the seeded audit record
/// where pricing-page-copy had only 712 samples.
/// </remarks>
public static class DemoPolicyDefinitions
{
    /// <summary>
    /// Policy: require-two-approvers.
    /// Blocks activation unless at least two distinct approvers are recorded.
    /// Demo implementation always passes (approval data lives in the backplane, not telemetry).
    /// </summary>
    public static IExperimentPolicy RequireTwoApprovers { get; } =
        new SimpleDemoPolicy(
            "require-two-approvers",
            "Requires at least two distinct approvers before an experiment can be activated.");

    /// <summary>
    /// Policy: no-friday-deploys.
    /// Blocks experiment launches on Fridays to reduce weekend risk.
    /// Demo implementation checks the day of week from DateTimeOffset.UtcNow.
    /// </summary>
    public static IExperimentPolicy NoFridayDeploys { get; } =
        new NoFridayDeployPolicy();

    /// <summary>
    /// Policy: min-sample-size-1000.
    /// Requires that each arm has at least 1,000 samples before a decision can be made.
    /// Expects "sampleCount" in the telemetry dictionary; fails when below threshold.
    /// </summary>
    public static IExperimentPolicy MinSampleSize1000 { get; } =
        new MinSampleSizePolicy(minimumSamples: 1000);
}

// ---------------------------------------------------------------------------
// Private policy implementations
// ---------------------------------------------------------------------------

/// <summary>
/// Always-passing demo stub for policies that can't be evaluated from telemetry alone.
/// </summary>
file sealed class SimpleDemoPolicy : IExperimentPolicy
{
    public SimpleDemoPolicy(string name, string description)
    {
        Name        = name;
        Description = description;
    }

    public string Name        { get; }
    public string Description { get; }

    public Task<PolicyEvaluationResult> EvaluateAsync(
        PolicyContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PolicyEvaluationResult
        {
            IsCompliant = true,
            PolicyName  = Name,
            Reason      = "Demo policy — always passes.",
        });
}

/// <summary>
/// Blocks transitions on Fridays (UTC).
/// </summary>
file sealed class NoFridayDeployPolicy : IExperimentPolicy
{
    public string Name        => "no-friday-deploys";
    public string Description => "Prevents experiment launches on Fridays to reduce weekend risk.";

    public Task<PolicyEvaluationResult> EvaluateAsync(
        PolicyContext context,
        CancellationToken cancellationToken = default)
    {
        var today      = DateTimeOffset.UtcNow.DayOfWeek;
        var isFriday   = today == DayOfWeek.Friday;

        return Task.FromResult(new PolicyEvaluationResult
        {
            IsCompliant = !isFriday,
            PolicyName  = Name,
            Reason      = isFriday
                ? "Experiment launches are blocked on Fridays."
                : $"Today is {today} — launch is permitted.",
            Severity    = isFriday ? PolicyViolationSeverity.Error : PolicyViolationSeverity.Info,
        });
    }
}

/// <summary>
/// Requires a minimum sample count in the "sampleCount" telemetry key.
/// </summary>
file sealed class MinSampleSizePolicy : IExperimentPolicy
{
    private readonly int _minimumSamples;

    public MinSampleSizePolicy(int minimumSamples)
    {
        _minimumSamples = minimumSamples;
    }

    public string Name        => "min-sample-size-1000";
    public string Description => $"Requires at least {_minimumSamples} samples per arm before a decision can be made.";

    public Task<PolicyEvaluationResult> EvaluateAsync(
        PolicyContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.Telemetry == null ||
            !context.Telemetry.TryGetValue("sampleCount", out var raw) ||
            raw is not int sampleCount)
        {
            // No data — pass with a note so evaluations are non-blocking without telemetry
            return Task.FromResult(new PolicyEvaluationResult
            {
                IsCompliant = true,
                PolicyName  = Name,
                Reason      = "No sampleCount telemetry available; policy not evaluated.",
            });
        }

        var passed = sampleCount >= _minimumSamples;
        return Task.FromResult(new PolicyEvaluationResult
        {
            IsCompliant = passed,
            PolicyName  = Name,
            Reason      = passed
                ? $"Sample count {sampleCount} meets the minimum of {_minimumSamples}."
                : $"Control arm had only {sampleCount} samples; minimum is {_minimumSamples}.",
            Severity    = passed ? PolicyViolationSeverity.Info : PolicyViolationSeverity.Critical,
        });
    }
}
