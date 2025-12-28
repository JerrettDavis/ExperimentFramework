namespace ExperimentFramework.Rollout;

/// <summary>
/// Extension methods for configuring rollout selection modes in the experiment builder.
/// </summary>
public static class ExperimentBuilderExtensions
{
    /// <summary>
    /// Configures the trial to use percentage-based rollout for variant selection.
    /// </summary>
    /// <typeparam name="TService">The service type being experimented on.</typeparam>
    /// <param name="builder">The service experiment builder.</param>
    /// <param name="rolloutName">Optional name for the rollout (used for consistent hashing).</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This selection mode uses consistent hashing based on user identity to determine
    /// whether a user should be included in the rollout.
    /// </para>
    /// <para>
    /// Requires:
    /// <list type="bullet">
    /// <item><description><see cref="IRolloutIdentityProvider"/> to be registered</description></item>
    /// <item><description><see cref="ServiceCollectionExtensions.AddExperimentRollout"/> to be called</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Define&lt;IPaymentProcessor&gt;(exp =&gt; exp
    ///     .UsingRollout("payment-v2")
    ///     .AddControl&lt;StripeV1Processor&gt;("false")
    ///     .AddCondition&lt;StripeV2Processor&gt;("true"));
    /// </code>
    /// </example>
    public static ServiceExperimentBuilder<TService> UsingRollout<TService>(
        this ServiceExperimentBuilder<TService> builder,
        string? rolloutName = null)
        where TService : class
    {
        return builder.UsingCustomMode(RolloutModes.Rollout, rolloutName);
    }

    /// <summary>
    /// Configures the trial to use staged rollout for variant selection.
    /// </summary>
    /// <typeparam name="TService">The service type being experimented on.</typeparam>
    /// <param name="builder">The service experiment builder.</param>
    /// <param name="rolloutName">Optional name for the rollout (used for consistent hashing).</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This selection mode uses time-based stages to gradually increase the rollout percentage.
    /// Users who are included at a lower percentage will remain included as the percentage increases.
    /// </para>
    /// <para>
    /// Requires:
    /// <list type="bullet">
    /// <item><description><see cref="IRolloutIdentityProvider"/> to be registered</description></item>
    /// <item><description><see cref="ServiceCollectionExtensions.AddExperimentStagedRollout"/> to be called with stage configuration</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddExperimentStagedRollout(opts =&gt;
    /// {
    ///     opts.Stages.Add(new RolloutStage { StartsAt = DateTimeOffset.UtcNow, Percentage = 5 });
    ///     opts.Stages.Add(new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(1), Percentage = 25 });
    ///     opts.Stages.Add(new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(3), Percentage = 50 });
    ///     opts.Stages.Add(new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(7), Percentage = 100 });
    /// });
    ///
    /// builder.Define&lt;IPaymentProcessor&gt;(exp =&gt; exp
    ///     .UsingStagedRollout("payment-v2")
    ///     .AddControl&lt;StripeV1Processor&gt;("false")
    ///     .AddCondition&lt;StripeV2Processor&gt;("true"));
    /// </code>
    /// </example>
    public static ServiceExperimentBuilder<TService> UsingStagedRollout<TService>(
        this ServiceExperimentBuilder<TService> builder,
        string? rolloutName = null)
        where TService : class
    {
        return builder.UsingCustomMode(StagedRolloutModes.StagedRollout, rolloutName);
    }
}
