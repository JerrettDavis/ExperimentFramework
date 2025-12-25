using System.Reflection;
using ExperimentFramework.Naming;

namespace ExperimentFramework.Selection;

/// <summary>
/// Base class for selection mode providers that reduces boilerplate.
/// </summary>
/// <remarks>
/// <para>
/// Extend this class and apply <see cref="SelectionModeAttribute"/> to create
/// a custom selection mode provider with minimal code:
/// </para>
/// <code>
/// [SelectionMode("Redis")]
/// public class RedisSelectionProvider : SelectionModeProviderBase
/// {
///     private readonly IConnectionMultiplexer _redis;
///
///     public RedisSelectionProvider(IConnectionMultiplexer redis)
///     {
///         _redis = redis;
///     }
///
///     public override async ValueTask&lt;string?&gt; SelectTrialKeyAsync(SelectionContext context)
///     {
///         var value = await _redis.GetDatabase().StringGetAsync(context.SelectorName);
///         return value.HasValue ? value.ToString() : null;
///     }
/// }
/// </code>
/// <para>
/// The <see cref="ModeIdentifier"/> is automatically derived from the
/// <see cref="SelectionModeAttribute"/> applied to the class.
/// </para>
/// <para>
/// Override <see cref="GetDefaultSelectorName"/> to customize how selector names
/// are generated when not explicitly provided.
/// </para>
/// </remarks>
public abstract class SelectionModeProviderBase : ISelectionModeProvider
{
    /// <summary>
    /// Initializes a new instance of <see cref="SelectionModeProviderBase"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the derived class does not have a <see cref="SelectionModeAttribute"/>.
    /// </exception>
    protected SelectionModeProviderBase()
    {
        var attr = GetType().GetCustomAttribute<SelectionModeAttribute>();
        ModeIdentifier = attr?.ModeIdentifier
            ?? throw new InvalidOperationException(
                $"Selection mode provider {GetType().Name} must have a [SelectionMode] attribute. " +
                $"Example: [SelectionMode(\"MyCustomMode\")]");
    }

    /// <summary>
    /// Initializes a new instance with an explicit mode identifier.
    /// </summary>
    /// <param name="modeIdentifier">The mode identifier for this provider.</param>
    /// <remarks>
    /// Use this constructor when you don't want to use the <see cref="SelectionModeAttribute"/>.
    /// </remarks>
    protected SelectionModeProviderBase(string modeIdentifier)
    {
        ArgumentNullException.ThrowIfNull(modeIdentifier);
        ModeIdentifier = modeIdentifier;
    }

    /// <inheritdoc />
    public string ModeIdentifier { get; }

    /// <inheritdoc />
    public abstract ValueTask<string?> SelectTrialKeyAsync(SelectionContext context);

    /// <inheritdoc />
    /// <remarks>
    /// The default implementation returns the feature flag name for the service type.
    /// Override this method to provide mode-specific naming conventions.
    /// </remarks>
    public virtual string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
        => convention.FeatureFlagNameFor(serviceType);
}
