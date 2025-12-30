using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Approval;
using ExperimentFramework.Governance.Policy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace ExperimentFramework.Configuration.Extensions.Handlers;

/// <summary>
/// Handles governance configuration from YAML/JSON.
/// </summary>
public class GovernanceConfigurationHandler
{
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GovernanceConfigurationHandler"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public GovernanceConfigurationHandler(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Applies governance configuration to the service collection.
    /// </summary>
    public void ApplyGovernanceConfiguration(
        IServiceCollection services,
        GovernanceConfig? governanceConfig)
    {
        if (governanceConfig == null)
        {
            _logger?.LogDebug("No governance configuration provided");
            return;
        }

        services.AddExperimentGovernance(gov =>
        {
            // Add approval gates
            if (governanceConfig.ApprovalGates != null)
            {
                foreach (var gateConfig in governanceConfig.ApprovalGates)
                {
                    ApplyApprovalGate(gov, gateConfig);
                }
            }

            // Add policies
            if (governanceConfig.Policies != null)
            {
                foreach (var policyConfig in governanceConfig.Policies)
                {
                    ApplyPolicy(gov, policyConfig);
                }
            }
        });

        _logger?.LogInformation("Governance configuration applied successfully");
    }

    private void ApplyApprovalGate(GovernanceBuilder gov, ApprovalGateConfig gateConfig)
    {
        ExperimentLifecycleState? fromState = null;
        if (!string.IsNullOrEmpty(gateConfig.FromState))
        {
            if (!Enum.TryParse<ExperimentLifecycleState>(gateConfig.FromState, true, out var parsed))
            {
                _logger?.LogWarning(
                    "Invalid fromState '{FromState}' in approval gate configuration. Skipping.",
                    gateConfig.FromState);
                return;
            }
            fromState = parsed;
        }

        if (!Enum.TryParse<ExperimentLifecycleState>(gateConfig.ToState, true, out var toState))
        {
            _logger?.LogWarning(
                "Invalid toState '{ToState}' in approval gate configuration. Skipping.",
                gateConfig.ToState);
            return;
        }

        switch (gateConfig.Type.ToLowerInvariant())
        {
            case "automatic":
                gov.WithAutomaticApproval(fromState, toState);
                _logger?.LogDebug(
                    "Added automatic approval gate: {FromState} → {ToState}",
                    fromState?.ToString() ?? "any",
                    toState);
                break;

            case "manual":
                gov.WithManualApproval(fromState, toState);
                _logger?.LogDebug(
                    "Added manual approval gate: {FromState} → {ToState}",
                    fromState?.ToString() ?? "any",
                    toState);
                break;

            case "rolebased":
            case "role-based":
                if (gateConfig.AllowedRoles == null || gateConfig.AllowedRoles.Count == 0)
                {
                    _logger?.LogWarning(
                        "RoleBased approval gate requires allowedRoles. Skipping gate for {ToState}.",
                        toState);
                    return;
                }

                gov.WithRoleBasedApproval(fromState, toState, gateConfig.AllowedRoles.ToArray());
                _logger?.LogDebug(
                    "Added role-based approval gate: {FromState} → {ToState} (roles: {Roles})",
                    fromState?.ToString() ?? "any",
                    toState,
                    string.Join(", ", gateConfig.AllowedRoles));
                break;

            default:
                _logger?.LogWarning(
                    "Unknown approval gate type '{Type}'. Skipping.",
                    gateConfig.Type);
                break;
        }
    }

    private void ApplyPolicy(GovernanceBuilder gov, PolicyConfig policyConfig)
    {
        switch (policyConfig.Type.ToLowerInvariant())
        {
            case "trafficlimit":
            case "traffic-limit":
                if (!policyConfig.MaxTrafficPercentage.HasValue)
                {
                    _logger?.LogWarning("TrafficLimit policy requires maxTrafficPercentage. Skipping.");
                    return;
                }

                TimeSpan? minStableTime = null;
                if (!string.IsNullOrEmpty(policyConfig.MinStableTime))
                {
                    if (TimeSpan.TryParse(policyConfig.MinStableTime, out var parsed))
                    {
                        minStableTime = parsed;
                    }
                    else
                    {
                        _logger?.LogWarning(
                            "Invalid minStableTime format '{MinStableTime}'. Expected format: HH:mm:ss or similar.",
                            policyConfig.MinStableTime);
                    }
                }

                gov.WithTrafficLimitPolicy(policyConfig.MaxTrafficPercentage.Value, minStableTime);
                _logger?.LogDebug(
                    "Added traffic limit policy: max {MaxTraffic}%, min stable time {MinStableTime}",
                    policyConfig.MaxTrafficPercentage.Value,
                    minStableTime?.ToString() ?? "none");
                break;

            case "errorrate":
            case "error-rate":
                if (!policyConfig.MaxErrorRate.HasValue)
                {
                    _logger?.LogWarning("ErrorRate policy requires maxErrorRate. Skipping.");
                    return;
                }

                gov.WithErrorRatePolicy(policyConfig.MaxErrorRate.Value);
                _logger?.LogDebug(
                    "Added error rate policy: max {MaxErrorRate:P}",
                    policyConfig.MaxErrorRate.Value);
                break;

            case "timewindow":
            case "time-window":
                if (string.IsNullOrEmpty(policyConfig.AllowedStartTime) ||
                    string.IsNullOrEmpty(policyConfig.AllowedEndTime))
                {
                    _logger?.LogWarning(
                        "TimeWindow policy requires allowedStartTime and allowedEndTime. Skipping.");
                    return;
                }

                if (!TryParseTimeSpan(policyConfig.AllowedStartTime, out var startTime))
                {
                    _logger?.LogWarning(
                        "Invalid allowedStartTime format '{AllowedStartTime}'. Expected HH:mm format.",
                        policyConfig.AllowedStartTime);
                    return;
                }

                if (!TryParseTimeSpan(policyConfig.AllowedEndTime, out var endTime))
                {
                    _logger?.LogWarning(
                        "Invalid allowedEndTime format '{AllowedEndTime}'. Expected HH:mm format.",
                        policyConfig.AllowedEndTime);
                    return;
                }

                gov.WithTimeWindowPolicy(startTime, endTime);
                _logger?.LogDebug(
                    "Added time window policy: {StartTime:hh\\:mm} - {EndTime:hh\\:mm}",
                    startTime,
                    endTime);
                break;

            case "conflictprevention":
            case "conflict-prevention":
                if (policyConfig.ConflictingExperiments == null ||
                    policyConfig.ConflictingExperiments.Count == 0)
                {
                    _logger?.LogWarning(
                        "ConflictPrevention policy requires conflictingExperiments. Skipping.");
                    return;
                }

                gov.WithConflictPreventionPolicy(policyConfig.ConflictingExperiments.ToArray());
                _logger?.LogDebug(
                    "Added conflict prevention policy for experiments: {Experiments}",
                    string.Join(", ", policyConfig.ConflictingExperiments));
                break;

            default:
                _logger?.LogWarning(
                    "Unknown policy type '{Type}'. Skipping.",
                    policyConfig.Type);
                break;
        }
    }

    private static bool TryParseTimeSpan(string timeString, out TimeSpan result)
    {
        // Try parsing as HH:mm format
        if (TimeSpan.TryParseExact(timeString, "hh\\:mm", CultureInfo.InvariantCulture, out result))
            return true;

        // Try parsing as H:mm format
        if (TimeSpan.TryParseExact(timeString, "h\\:mm", CultureInfo.InvariantCulture, out result))
            return true;

        // Try general parse
        return TimeSpan.TryParse(timeString, out result);
    }
}
