namespace ExperimentFramework.Targeting;

/// <summary>
/// Simple dictionary-based targeting context implementation.
/// </summary>
public sealed class SimpleTargetingContext : ITargetingContext
{
    private readonly Dictionary<string, object> _attributes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a new targeting context with the specified user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    public SimpleTargetingContext(string? userId = null)
    {
        UserId = userId;
    }

    /// <inheritdoc />
    public string? UserId { get; }

    /// <inheritdoc />
    public IEnumerable<string> AttributeNames => _attributes.Keys;

    /// <summary>
    /// Sets an attribute value.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>This context for chaining.</returns>
    public SimpleTargetingContext WithAttribute(string name, object value)
    {
        _attributes[name] = value;
        return this;
    }

    /// <inheritdoc />
    public object? GetAttribute(string attributeName)
        => _attributes.TryGetValue(attributeName, out var value) ? value : null;

    /// <inheritdoc />
    public bool HasAttribute(string attributeName)
        => _attributes.ContainsKey(attributeName);
}
