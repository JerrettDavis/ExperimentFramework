namespace ExperimentFramework.Targeting;

/// <summary>
/// Defines a targeting rule that determines whether a context matches.
/// </summary>
public interface ITargetingRule
{
    /// <summary>
    /// Evaluates whether the given context matches this rule.
    /// </summary>
    /// <param name="context">The targeting context to evaluate.</param>
    /// <returns>True if the context matches; otherwise false.</returns>
    bool Evaluate(ITargetingContext context);
}

/// <summary>
/// Static methods for creating common targeting rules.
/// </summary>
public static class TargetingRules
{
    /// <summary>
    /// Creates a rule that always matches.
    /// </summary>
    public static ITargetingRule Always() => new AlwaysRule();

    /// <summary>
    /// Creates a rule that never matches.
    /// </summary>
    public static ITargetingRule Never() => new NeverRule();

    /// <summary>
    /// Creates a rule that matches specific user IDs.
    /// </summary>
    /// <param name="userIds">The user IDs to match.</param>
    public static ITargetingRule Users(params string[] userIds)
        => new UserIdsRule(userIds);

    /// <summary>
    /// Creates a rule that matches users with an attribute equal to a value.
    /// </summary>
    /// <param name="attributeName">The attribute name.</param>
    /// <param name="value">The expected value.</param>
    public static ITargetingRule AttributeEquals(string attributeName, object value)
        => new AttributeEqualsRule(attributeName, value);

    /// <summary>
    /// Creates a rule that matches users with an attribute in a set of values.
    /// </summary>
    /// <param name="attributeName">The attribute name.</param>
    /// <param name="values">The allowed values.</param>
    public static ITargetingRule AttributeIn(string attributeName, params object[] values)
        => new AttributeInRule(attributeName, values);

    /// <summary>
    /// Creates a rule that matches users who have a specific attribute.
    /// </summary>
    /// <param name="attributeName">The attribute name.</param>
    public static ITargetingRule HasAttribute(string attributeName)
        => new HasAttributeRule(attributeName);

    /// <summary>
    /// Creates a rule that matches if all sub-rules match (AND).
    /// </summary>
    /// <param name="rules">The rules to combine.</param>
    public static ITargetingRule All(params ITargetingRule[] rules)
        => new AllRule(rules);

    /// <summary>
    /// Creates a rule that matches if any sub-rule matches (OR).
    /// </summary>
    /// <param name="rules">The rules to combine.</param>
    public static ITargetingRule Any(params ITargetingRule[] rules)
        => new AnyRule(rules);

    /// <summary>
    /// Creates a rule that inverts another rule (NOT).
    /// </summary>
    /// <param name="rule">The rule to invert.</param>
    public static ITargetingRule Not(ITargetingRule rule)
        => new NotRule(rule);

    /// <summary>
    /// Creates a rule that matches a percentage of users based on their ID.
    /// </summary>
    /// <param name="percentage">The percentage of users to match (0-100).</param>
    /// <param name="seed">Optional seed for consistent hashing.</param>
    public static ITargetingRule Percentage(int percentage, string? seed = null)
        => new PercentageRule(percentage, seed);

    private sealed class AlwaysRule : ITargetingRule
    {
        public bool Evaluate(ITargetingContext context) => true;
    }

    private sealed class NeverRule : ITargetingRule
    {
        public bool Evaluate(ITargetingContext context) => false;
    }

    private sealed class UserIdsRule(IEnumerable<string> userIds) : ITargetingRule
    {
        private readonly HashSet<string> _userIds = new(userIds, StringComparer.OrdinalIgnoreCase);

        public bool Evaluate(ITargetingContext context)
            => context.UserId != null && _userIds.Contains(context.UserId);
    }

    private sealed class AttributeEqualsRule(string attributeName, object value) : ITargetingRule
    {
        public bool Evaluate(ITargetingContext context)
        {
            var attr = context.GetAttribute(attributeName);
            return attr != null && attr.Equals(value);
        }
    }

    private sealed class AttributeInRule(string attributeName, object[] values) : ITargetingRule
    {
        private readonly HashSet<object> _values = new(values);

        public bool Evaluate(ITargetingContext context)
        {
            var attr = context.GetAttribute(attributeName);
            return attr != null && _values.Contains(attr);
        }
    }

    private sealed class HasAttributeRule(string attributeName) : ITargetingRule
    {
        public bool Evaluate(ITargetingContext context)
            => context.HasAttribute(attributeName);
    }

    private sealed class AllRule(ITargetingRule[] rules) : ITargetingRule
    {
        public bool Evaluate(ITargetingContext context)
            => rules.All(r => r.Evaluate(context));
    }

    private sealed class AnyRule(ITargetingRule[] rules) : ITargetingRule
    {
        public bool Evaluate(ITargetingContext context)
            => rules.Any(r => r.Evaluate(context));
    }

    private sealed class NotRule(ITargetingRule rule) : ITargetingRule
    {
        public bool Evaluate(ITargetingContext context)
            => !rule.Evaluate(context);
    }

    private sealed class PercentageRule(int percentage, string? seed) : ITargetingRule
    {
        public bool Evaluate(ITargetingContext context)
        {
            if (context.UserId == null) return false;
            return Rollout.RolloutAllocator.IsIncluded(
                context.UserId,
                seed ?? "targeting",
                percentage);
        }
    }
}
