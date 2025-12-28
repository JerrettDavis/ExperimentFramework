using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Rollout;

/// <summary>
/// Well-known mode identifier for percentage-based rollout selection.
/// </summary>
public static class RolloutModes
{
    /// <summary>
    /// Mode identifier for percentage-based rollout selection.
    /// </summary>
    public const string Rollout = "Rollout";
}

/// <summary>
/// Selection mode provider that uses percentage-based allocation for trial selection.
/// </summary>
/// <remarks>
/// <para>
/// This provider uses <see cref="IRolloutIdentityProvider"/> to get a user identity,
/// then applies consistent hashing to deterministically include or exclude the user
/// from a rollout based on a configured percentage.
/// </para>
/// <para>
/// The same user will always get the same allocation for a given rollout,
/// but different rollouts can have independent allocations.
/// </para>
/// </remarks>
[SelectionMode(RolloutModes.Rollout)]
public sealed class RolloutProvider : ISelectionModeProvider
{
    private readonly IRolloutIdentityProvider? _identityProvider;
    private readonly RolloutOptions _options;

    /// <summary>
    /// Creates a new rollout provider.
    /// </summary>
    /// <param name="identityProvider">Optional identity provider for user identification.</param>
    /// <param name="options">Optional rollout options.</param>
    public RolloutProvider(
        IRolloutIdentityProvider? identityProvider = null,
        IOptions<RolloutOptions>? options = null)
    {
        _identityProvider = identityProvider;
        _options = options?.Value ?? new RolloutOptions();
    }

    /// <inheritdoc />
    public string ModeIdentifier => RolloutModes.Rollout;

    /// <inheritdoc />
    public ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        // If no identity provider, check if there's one in the service provider
        var identityProvider = _identityProvider ??
            context.ServiceProvider.GetService<IRolloutIdentityProvider>();

        if (identityProvider == null || !identityProvider.TryGetIdentity(out var identity))
        {
            // No identity - return excluded key or fall back to default
            return ValueTask.FromResult(_options.ExcludedKey);
        }

        // Get rollout-specific options if available
        var optionsSnapshot = context.ServiceProvider.GetService<IOptionsSnapshot<RolloutOptions>>();
        var options = optionsSnapshot?.Value ?? _options;

        var isIncluded = RolloutAllocator.IsIncluded(
            identity,
            context.SelectorName,
            options.Percentage,
            options.Seed);

        var selectedKey = isIncluded ? options.IncludedKey : options.ExcludedKey;
        return ValueTask.FromResult(selectedKey);
    }

    /// <inheritdoc />
    public string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
        => $"Rollout:{convention.FeatureFlagNameFor(serviceType)}";
}
