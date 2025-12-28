using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Targeting;

/// <summary>
/// Well-known mode identifier for targeting-based selection.
/// </summary>
public static class TargetingModes
{
    /// <summary>
    /// Mode identifier for targeting-based selection.
    /// </summary>
    public const string Targeting = "Targeting";
}

/// <summary>
/// Selection mode provider that uses targeting rules for trial selection.
/// </summary>
/// <remarks>
/// <para>
/// This provider evaluates targeting rules against the current context to determine
/// which trial variant to select. Rules are evaluated in order, and the first matching
/// rule determines the selection.
/// </para>
/// </remarks>
[SelectionMode(TargetingModes.Targeting)]
public sealed class TargetingProvider : ISelectionModeProvider
{
    private readonly ITargetingContextProvider? _contextProvider;
    private readonly TargetingOptions? _options;

    /// <summary>
    /// Creates a new targeting provider.
    /// </summary>
    /// <param name="contextProvider">Optional context provider.</param>
    /// <param name="options">Optional targeting options.</param>
    public TargetingProvider(
        ITargetingContextProvider? contextProvider = null,
        TargetingOptions? options = null)
    {
        _contextProvider = contextProvider;
        _options = options;
    }

    /// <inheritdoc />
    public string ModeIdentifier => TargetingModes.Targeting;

    /// <inheritdoc />
    public async ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        var contextProvider = _contextProvider ??
            context.ServiceProvider.GetService<ITargetingContextProvider>();

        if (contextProvider == null)
            return null;

        var targetingContext = await contextProvider.GetContextAsync();
        if (targetingContext == null)
            return null;

        // Get targeting configuration for this selector
        var config = context.ServiceProvider.GetService<ITargetingConfigurationProvider>();
        var rules = config?.GetRulesFor(context.SelectorName);

        if (rules == null || rules.Count == 0)
        {
            // Use default options if no specific rules
            var options = _options ??
                context.ServiceProvider.GetService<TargetingOptions>();

            if (options?.DefaultRule != null && options.DefaultRule.Evaluate(targetingContext))
            {
                return options.MatchedKey;
            }

            return options?.UnmatchedKey;
        }

        // Evaluate rules in order
        foreach (var (rule, key) in rules)
        {
            if (rule.Evaluate(targetingContext))
            {
                return key;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
        => $"Targeting:{convention.FeatureFlagNameFor(serviceType)}";
}
