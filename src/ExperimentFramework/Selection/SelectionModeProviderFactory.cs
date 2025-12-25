using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Selection;

/// <summary>
/// Generic factory for creating selection mode providers without custom factory boilerplate.
/// </summary>
/// <typeparam name="TProvider">
/// The selection mode provider type. Must have a <see cref="SelectionModeAttribute"/>
/// or the mode identifier must be provided via the constructor.
/// </typeparam>
/// <remarks>
/// <para>
/// This factory eliminates the need to create custom <see cref="ISelectionModeProviderFactory"/>
/// implementations for most providers. It uses <see cref="ActivatorUtilities"/> to create
/// provider instances with dependency injection support.
/// </para>
/// <para>
/// The mode identifier is read from the <see cref="SelectionModeAttribute"/> on the provider type:
/// </para>
/// <code>
/// [SelectionMode("Redis")]
/// public class RedisProvider : SelectionModeProviderBase { ... }
///
/// // Registration (no custom factory needed):
/// services.AddSingleton&lt;ISelectionModeProviderFactory,
///     SelectionModeProviderFactory&lt;RedisProvider&gt;&gt;();
///
/// // Or use the extension method:
/// services.AddSelectionModeProvider&lt;RedisProvider&gt;();
/// </code>
/// </remarks>
public sealed class SelectionModeProviderFactory<TProvider> : ISelectionModeProviderFactory
    where TProvider : class, ISelectionModeProvider
{
    /// <inheritdoc />
    public string ModeIdentifier { get; }

    /// <summary>
    /// Creates a new factory that reads the mode identifier from <see cref="SelectionModeAttribute"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the provider type does not have a <see cref="SelectionModeAttribute"/>.
    /// </exception>
    public SelectionModeProviderFactory()
    {
        var attr = typeof(TProvider).GetCustomAttribute<SelectionModeAttribute>();
        ModeIdentifier = attr?.ModeIdentifier
            ?? throw new InvalidOperationException(
                $"Provider type {typeof(TProvider).Name} must have a [SelectionMode] attribute, " +
                $"or use the constructor overload that accepts a modeIdentifier parameter.");
    }

    /// <summary>
    /// Creates a new factory with an explicit mode identifier.
    /// </summary>
    /// <param name="modeIdentifier">The mode identifier for the provider.</param>
    /// <remarks>
    /// Use this constructor when the provider type does not have a <see cref="SelectionModeAttribute"/>
    /// or when you want to override the attribute value.
    /// </remarks>
    public SelectionModeProviderFactory(string modeIdentifier)
    {
        ArgumentNullException.ThrowIfNull(modeIdentifier);
        ModeIdentifier = modeIdentifier;
    }

    /// <inheritdoc />
    public ISelectionModeProvider Create(IServiceProvider scopedProvider)
    {
        return ActivatorUtilities.CreateInstance<TProvider>(scopedProvider);
    }
}
