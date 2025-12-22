namespace ExperimentFramework.Naming;

/// <summary>
/// Value object representing a strongly-typed selector name used for trial selection.
/// </summary>
/// <param name="Value">The selector name value (feature flag name or configuration key).</param>
/// <remarks>
/// This type provides implicit conversions to and from <see cref="string"/> for convenience
/// while maintaining type safety at the API boundary.
/// </remarks>
public readonly record struct ExperimentSelectorName(string Value)
{
    /// <summary>
    /// Implicitly converts a string to an <see cref="ExperimentSelectorName"/>.
    /// </summary>
    /// <param name="value">The selector name string.</param>
    public static implicit operator ExperimentSelectorName(string value) => new(value);

    /// <summary>
    /// Implicitly converts an <see cref="ExperimentSelectorName"/> to a string.
    /// </summary>
    /// <param name="name">The selector name instance.</param>
    public static implicit operator string(ExperimentSelectorName name) => name.Value;

    /// <inheritdoc/>
    public override string ToString() => Value;
}
