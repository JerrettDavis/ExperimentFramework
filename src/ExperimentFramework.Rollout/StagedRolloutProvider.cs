using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Rollout;

/// <summary>
/// Well-known mode identifier for staged rollout selection.
/// </summary>
public static class StagedRolloutModes
{
    /// <summary>
    /// Mode identifier for staged rollout selection.
    /// </summary>
    public const string StagedRollout = "StagedRollout";
}

/// <summary>
/// Selection mode provider for staged rollouts that increase percentage over time.
/// </summary>
/// <remarks>
/// <para>
/// This provider uses <see cref="StagedRolloutOptions"/> to determine the current
/// rollout percentage based on a schedule of stages, then applies consistent hashing
/// to include or exclude users.
/// </para>
/// <para>
/// Users who are included at a lower percentage will remain included as the
/// percentage increases. This ensures a stable rollout experience.
/// </para>
/// </remarks>
[SelectionMode(StagedRolloutModes.StagedRollout)]
public sealed class StagedRolloutProvider : ISelectionModeProvider
{
    private readonly IRolloutIdentityProvider? _identityProvider;
    private readonly StagedRolloutOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates a new staged rollout provider.
    /// </summary>
    /// <param name="identityProvider">Optional identity provider for user identification.</param>
    /// <param name="options">Optional staged rollout options.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    public StagedRolloutProvider(
        IRolloutIdentityProvider? identityProvider = null,
        IOptions<StagedRolloutOptions>? options = null,
        TimeProvider? timeProvider = null)
    {
        _identityProvider = identityProvider;
        _options = options?.Value ?? new StagedRolloutOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public string ModeIdentifier => StagedRolloutModes.StagedRollout;

    /// <inheritdoc />
    public ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        // Get identity
        var identityProvider = _identityProvider ??
            context.ServiceProvider.GetService<IRolloutIdentityProvider>();

        if (identityProvider == null || !identityProvider.TryGetIdentity(out var identity))
        {
            return ValueTask.FromResult(_options.ExcludedKey);
        }

        // Get options snapshot if available
        var optionsSnapshot = context.ServiceProvider.GetService<IOptionsSnapshot<StagedRolloutOptions>>();
        var options = optionsSnapshot?.Value ?? _options;

        // Get current percentage from stages
        var currentTime = _timeProvider.GetUtcNow();
        var currentPercentage = options.GetCurrentPercentage(currentTime);

        var isIncluded = RolloutAllocator.IsIncluded(
            identity,
            context.SelectorName,
            currentPercentage,
            options.Seed);

        var selectedKey = isIncluded ? options.IncludedKey : options.ExcludedKey;
        return ValueTask.FromResult(selectedKey);
    }

    /// <inheritdoc />
    public string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
        => $"StagedRollout:{convention.FeatureFlagNameFor(serviceType)}";
}
