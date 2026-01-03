using ExperimentFramework.Governance.Approval;
using ExperimentFramework.Governance.Policy;
using ExperimentFramework.Governance.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Governance;

/// <summary>
/// Options for configuring governance features.
/// </summary>
internal class GovernanceOptions
{
    public List<(ExperimentLifecycleState?, ExperimentLifecycleState, IApprovalGate)> ApprovalGates { get; } = new();
    public List<IExperimentPolicy> Policies { get; } = new();
}

/// <summary>
/// Holder for configured governance services.
/// </summary>
internal class GovernanceConfiguration
{
    public GovernanceConfiguration(IApprovalManager approvalManager, IPolicyEvaluator policyEvaluator)
    {
        ApprovalManager = approvalManager;
        PolicyEvaluator = policyEvaluator;
    }

    public IApprovalManager ApprovalManager { get; }
    public IPolicyEvaluator PolicyEvaluator { get; }
}

/// <summary>
/// Extension methods for registering governance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds experiment governance services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentGovernance(this IServiceCollection services)
    {
        services.TryAddSingleton<ILifecycleManager, LifecycleManager>();
        services.TryAddSingleton<IApprovalManager, ApprovalManager>();
        services.TryAddSingleton<IVersionManager, VersionManager>();
        services.TryAddSingleton<IPolicyEvaluator, PolicyEvaluator>();

        services.AddOptions<GovernanceOptions>();

        return services;
    }

    /// <summary>
    /// Adds experiment governance services with configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentGovernance(
        this IServiceCollection services,
        Action<GovernanceBuilder> configure)
    {
        services.AddExperimentGovernance();

        var builder = new GovernanceBuilder(services);
        configure(builder);

        // Post-build: register gates and policies from options
        services.AddSingleton(sp =>
        {
            var approvalManager = sp.GetRequiredService<IApprovalManager>();
            var policyEvaluator = sp.GetRequiredService<IPolicyEvaluator>();
            var options = sp.GetRequiredService<IOptions<GovernanceOptions>>().Value;

            foreach (var (fromState, toState, gate) in options.ApprovalGates)
            {
                approvalManager.RegisterGate(fromState, toState, gate);
            }

            foreach (var policy in options.Policies)
            {
                policyEvaluator.RegisterPolicy(policy);
            }

            return new GovernanceConfiguration(approvalManager, policyEvaluator);
        });

        return services;
    }
}

/// <summary>
/// Builder for configuring governance features.
/// </summary>
public class GovernanceBuilder
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="GovernanceBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public GovernanceBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Registers an approval gate for specific transitions.
    /// </summary>
    /// <param name="fromState">Source state (null for any state).</param>
    /// <param name="toState">Target state.</param>
    /// <param name="gate">The approval gate.</param>
    /// <returns>The builder for chaining.</returns>
    public GovernanceBuilder WithApprovalGate(
        ExperimentLifecycleState? fromState,
        ExperimentLifecycleState toState,
        IApprovalGate gate)
    {
        // Store gate for post-configuration
        _services.Configure<GovernanceOptions>(options =>
        {
            options.ApprovalGates.Add((fromState, toState, gate));
        });

        return this;
    }

    /// <summary>
    /// Registers a policy.
    /// </summary>
    /// <param name="policy">The policy to register.</param>
    /// <returns>The builder for chaining.</returns>
    public GovernanceBuilder WithPolicy(IExperimentPolicy policy)
    {
        // Store policy for post-configuration
        _services.Configure<GovernanceOptions>(options =>
        {
            options.Policies.Add(policy);
        });

        return this;
    }

    /// <summary>
    /// Configures automatic approval for specific transitions.
    /// </summary>
    /// <param name="fromState">Source state (null for any state).</param>
    /// <param name="toState">Target state.</param>
    /// <returns>The builder for chaining.</returns>
    public GovernanceBuilder WithAutomaticApproval(
        ExperimentLifecycleState? fromState,
        ExperimentLifecycleState toState)
    {
        return WithApprovalGate(fromState, toState, new AutomaticApprovalGate());
    }

    /// <summary>
    /// Configures manual approval for specific transitions.
    /// </summary>
    /// <param name="fromState">Source state (null for any state).</param>
    /// <param name="toState">Target state.</param>
    /// <returns>The builder for chaining.</returns>
    public GovernanceBuilder WithManualApproval(
        ExperimentLifecycleState? fromState,
        ExperimentLifecycleState toState)
    {
        var gate = new ManualApprovalGate();
        _services.AddSingleton(gate);
        return WithApprovalGate(fromState, toState, gate);
    }

    /// <summary>
    /// Configures role-based approval for specific transitions.
    /// </summary>
    /// <param name="fromState">Source state (null for any state).</param>
    /// <param name="toState">Target state.</param>
    /// <param name="allowedRoles">Roles allowed to approve.</param>
    /// <returns>The builder for chaining.</returns>
    public GovernanceBuilder WithRoleBasedApproval(
        ExperimentLifecycleState? fromState,
        ExperimentLifecycleState toState,
        params string[] allowedRoles)
    {
        return WithApprovalGate(fromState, toState, new RoleBasedApprovalGate(allowedRoles));
    }

    /// <summary>
    /// Adds a traffic limit policy.
    /// </summary>
    /// <param name="maxTrafficPercentage">Maximum traffic percentage.</param>
    /// <param name="minStableTime">Minimum stable time.</param>
    /// <returns>The builder for chaining.</returns>
    public GovernanceBuilder WithTrafficLimitPolicy(
        double maxTrafficPercentage,
        TimeSpan? minStableTime = null)
    {
        return WithPolicy(new TrafficLimitPolicy(maxTrafficPercentage, minStableTime));
    }

    /// <summary>
    /// Adds an error rate policy.
    /// </summary>
    /// <param name="maxErrorRate">Maximum error rate (0.0-1.0).</param>
    /// <returns>The builder for chaining.</returns>
    public GovernanceBuilder WithErrorRatePolicy(double maxErrorRate)
    {
        return WithPolicy(new ErrorRatePolicy(maxErrorRate));
    }

    /// <summary>
    /// Adds a time window policy.
    /// </summary>
    /// <param name="allowedStartTime">Start of allowed window.</param>
    /// <param name="allowedEndTime">End of allowed window.</param>
    /// <returns>The builder for chaining.</returns>
    public GovernanceBuilder WithTimeWindowPolicy(
        TimeSpan allowedStartTime,
        TimeSpan allowedEndTime)
    {
        return WithPolicy(new TimeWindowPolicy(allowedStartTime, allowedEndTime));
    }

    /// <summary>
    /// Adds a conflict prevention policy.
    /// </summary>
    /// <param name="conflictingExperiments">Names of conflicting experiments.</param>
    /// <returns>The builder for chaining.</returns>
    public GovernanceBuilder WithConflictPreventionPolicy(params string[] conflictingExperiments)
    {
        return WithPolicy(new ConflictPreventionPolicy(conflictingExperiments));
    }

    /// <summary>
    /// Configures persistence backplane for governance state.
    /// </summary>
    /// <param name="configurePersistence">Action to configure persistence.</param>
    /// <returns>The builder for chaining.</returns>
    public GovernanceBuilder UsePersistence(Action<IServiceCollection> configurePersistence)
    {
        configurePersistence(_services);
        return this;
    }
}
