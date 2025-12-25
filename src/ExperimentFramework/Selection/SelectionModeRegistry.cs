using System.Collections.Concurrent;

namespace ExperimentFramework.Selection;

/// <summary>
/// Thread-safe registry for selection mode provider factories.
/// </summary>
/// <remarks>
/// <para>
/// The registry is populated at application startup with both built-in and
/// external provider factories. External packages register their factories
/// via dependency injection:
/// <code>
/// services.AddSingleton&lt;ISelectionModeProviderFactory, MyProviderFactory&gt;();
/// </code>
/// </para>
/// <para>
/// The registry is then used at runtime to create provider instances for
/// each experiment invocation.
/// </para>
/// </remarks>
public sealed class SelectionModeRegistry
{
    private readonly ConcurrentDictionary<string, ISelectionModeProviderFactory> _factories = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a provider factory for a selection mode.
    /// </summary>
    /// <param name="factory">The factory to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
    public void Register(ISelectionModeProviderFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factories[factory.ModeIdentifier] = factory;
    }

    /// <summary>
    /// Gets a provider for the specified mode identifier.
    /// </summary>
    /// <param name="modeIdentifier">The mode identifier to look up.</param>
    /// <param name="scopedProvider">The scoped service provider for provider creation.</param>
    /// <returns>The provider instance, or <c>null</c> if not found.</returns>
    public ISelectionModeProvider? GetProvider(string modeIdentifier, IServiceProvider scopedProvider)
    {
        if (string.IsNullOrEmpty(modeIdentifier))
            return null;

        return _factories.TryGetValue(modeIdentifier, out var factory)
            ? factory.Create(scopedProvider)
            : null;
    }

    /// <summary>
    /// Checks if a provider is registered for the specified mode identifier.
    /// </summary>
    /// <param name="modeIdentifier">The mode identifier to check.</param>
    /// <returns><c>true</c> if a provider is registered; otherwise, <c>false</c>.</returns>
    public bool IsRegistered(string modeIdentifier)
    {
        return !string.IsNullOrEmpty(modeIdentifier) && _factories.ContainsKey(modeIdentifier);
    }

    /// <summary>
    /// Gets all registered mode identifiers.
    /// </summary>
    public IEnumerable<string> RegisteredModes => _factories.Keys;

    /// <summary>
    /// Gets the number of registered providers.
    /// </summary>
    public int Count => _factories.Count;
}
