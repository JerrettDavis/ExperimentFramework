namespace ExperimentFramework.Targeting;

/// <summary>
/// Configuration options for targeting.
/// </summary>
public sealed class TargetingOptions
{
    /// <summary>
    /// Gets or sets the default targeting rule applied when no specific rules are configured.
    /// </summary>
    public ITargetingRule? DefaultRule { get; set; }

    /// <summary>
    /// Gets or sets the trial key to select when the targeting rule matches.
    /// </summary>
    public string MatchedKey { get; set; } = "true";

    /// <summary>
    /// Gets or sets the trial key to select when the targeting rule does not match.
    /// </summary>
    public string? UnmatchedKey { get; set; }
}

/// <summary>
/// Provides targeting configuration for specific selectors.
/// </summary>
public interface ITargetingConfigurationProvider
{
    /// <summary>
    /// Gets the targeting rules for a specific selector.
    /// </summary>
    /// <param name="selectorName">The selector name.</param>
    /// <returns>A list of (rule, key) pairs to evaluate.</returns>
    IReadOnlyList<(ITargetingRule Rule, string Key)>? GetRulesFor(string selectorName);
}

/// <summary>
/// In-memory implementation of targeting configuration.
/// </summary>
public sealed class InMemoryTargetingConfiguration : ITargetingConfigurationProvider
{
    private readonly Dictionary<string, List<(ITargetingRule, string)>> _rules = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a targeting rule for a selector.
    /// </summary>
    /// <param name="selectorName">The selector name.</param>
    /// <param name="rule">The targeting rule.</param>
    /// <param name="key">The trial key to select when the rule matches.</param>
    /// <returns>This configuration for chaining.</returns>
    public InMemoryTargetingConfiguration AddRule(string selectorName, ITargetingRule rule, string key)
    {
        if (!_rules.TryGetValue(selectorName, out var list))
        {
            list = [];
            _rules[selectorName] = list;
        }

        list.Add((rule, key));
        return this;
    }

    /// <inheritdoc />
    public IReadOnlyList<(ITargetingRule Rule, string Key)>? GetRulesFor(string selectorName)
        => _rules.TryGetValue(selectorName, out var rules) ? rules : null;
}
