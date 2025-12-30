using System.Collections.Concurrent;

namespace ExperimentFramework.Configuration.Extensions;

/// <summary>
/// Registry for configuration extension handlers.
/// This allows extension packages to register their own decorator and selection mode handlers
/// without the Configuration package having direct knowledge of them.
/// </summary>
public sealed class ConfigurationExtensionRegistry
{
    private readonly ConcurrentDictionary<string, IConfigurationDecoratorHandler> _decoratorHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IConfigurationSelectionModeHandler> _selectionModeHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IConfigurationBackplaneHandler> _backplaneHandlers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a decorator handler for the specified decorator type.
    /// </summary>
    /// <param name="handler">The handler to register.</param>
    /// <returns>True if registered, false if a handler for this type already exists.</returns>
    public bool RegisterDecoratorHandler(IConfigurationDecoratorHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _decoratorHandlers.TryAdd(handler.DecoratorType, handler);
    }

    /// <summary>
    /// Registers a selection mode handler for the specified mode type.
    /// </summary>
    /// <param name="handler">The handler to register.</param>
    /// <returns>True if registered, false if a handler for this type already exists.</returns>
    public bool RegisterSelectionModeHandler(IConfigurationSelectionModeHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _selectionModeHandlers.TryAdd(handler.ModeType, handler);
    }

    /// <summary>
    /// Gets the decorator handler for the specified type.
    /// </summary>
    /// <param name="decoratorType">The decorator type identifier.</param>
    /// <returns>The handler, or null if not found.</returns>
    public IConfigurationDecoratorHandler? GetDecoratorHandler(string decoratorType)
    {
        _decoratorHandlers.TryGetValue(decoratorType, out var handler);
        return handler;
    }

    /// <summary>
    /// Gets the selection mode handler for the specified type.
    /// </summary>
    /// <param name="modeType">The selection mode type identifier.</param>
    /// <returns>The handler, or null if not found.</returns>
    public IConfigurationSelectionModeHandler? GetSelectionModeHandler(string modeType)
    {
        _selectionModeHandlers.TryGetValue(modeType, out var handler);
        return handler;
    }

    /// <summary>
    /// Checks if a decorator type is registered.
    /// </summary>
    public bool HasDecoratorHandler(string decoratorType) =>
        _decoratorHandlers.ContainsKey(decoratorType);

    /// <summary>
    /// Checks if a selection mode type is registered.
    /// </summary>
    public bool HasSelectionModeHandler(string modeType) =>
        _selectionModeHandlers.ContainsKey(modeType);

    /// <summary>
    /// Gets all registered decorator type identifiers.
    /// </summary>
    public IEnumerable<string> GetRegisteredDecoratorTypes() =>
        _decoratorHandlers.Keys;

    /// <summary>
    /// Gets all registered selection mode type identifiers.
    /// </summary>
    public IEnumerable<string> GetRegisteredSelectionModeTypes() =>
        _selectionModeHandlers.Keys;

    /// <summary>
    /// Gets all registered decorator handlers.
    /// </summary>
    public IEnumerable<IConfigurationDecoratorHandler> GetDecoratorHandlers() =>
        _decoratorHandlers.Values;

    /// <summary>
    /// Gets all registered selection mode handlers.
    /// </summary>
    public IEnumerable<IConfigurationSelectionModeHandler> GetSelectionModeHandlers() =>
        _selectionModeHandlers.Values;

    /// <summary>
    /// Registers a backplane handler for the specified backplane type.
    /// </summary>
    /// <param name="handler">The handler to register.</param>
    /// <returns>True if registered, false if a handler for this type already exists.</returns>
    public bool RegisterBackplaneHandler(IConfigurationBackplaneHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _backplaneHandlers.TryAdd(handler.BackplaneType, handler);
    }

    /// <summary>
    /// Gets the backplane handler for the specified type.
    /// </summary>
    /// <param name="backplaneType">The backplane type identifier.</param>
    /// <returns>The handler, or null if not found.</returns>
    public IConfigurationBackplaneHandler? GetBackplaneHandler(string backplaneType)
    {
        _backplaneHandlers.TryGetValue(backplaneType, out var handler);
        return handler;
    }

    /// <summary>
    /// Checks if a backplane type is registered.
    /// </summary>
    public bool HasBackplaneHandler(string backplaneType) =>
        _backplaneHandlers.ContainsKey(backplaneType);

    /// <summary>
    /// Gets all registered backplane type identifiers.
    /// </summary>
    public IEnumerable<string> GetRegisteredBackplaneTypes() =>
        _backplaneHandlers.Keys;

    /// <summary>
    /// Gets all registered backplane handlers.
    /// </summary>
    public IEnumerable<IConfigurationBackplaneHandler> GetBackplaneHandlers() =>
        _backplaneHandlers.Values;
}
