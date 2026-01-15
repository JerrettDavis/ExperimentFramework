namespace ExperimentFramework.Testing;

/// <summary>
/// Ambient context for scoped test selection overrides.
/// </summary>
internal static class TestSelectionContext
{
    private static readonly AsyncLocal<Dictionary<Type, string>?> _forcedSelections = new();
    private static readonly AsyncLocal<Dictionary<Type, string>?> _frozenSelections = new();
    private static readonly AsyncLocal<bool> _isFrozen = new();

    /// <summary>
    /// Gets or sets the forced selections for the current async context.
    /// </summary>
    public static Dictionary<Type, string>? ForcedSelections
    {
        get => _forcedSelections.Value;
        set => _forcedSelections.Value = value;
    }

    /// <summary>
    /// Gets or sets the frozen selections for the current async context.
    /// </summary>
    public static Dictionary<Type, string>? FrozenSelections
    {
        get => _frozenSelections.Value;
        set => _frozenSelections.Value = value;
    }

    /// <summary>
    /// Gets or sets whether selection is frozen in the current async context.
    /// </summary>
    public static bool IsFrozen
    {
        get => _isFrozen.Value;
        set => _isFrozen.Value = value;
    }

    /// <summary>
    /// Tries to get the forced selection for a service type.
    /// </summary>
    public static bool TryGetForcedSelection(Type serviceType, out string? trialKey)
    {
        trialKey = null;

        // Check frozen selections first
        if (IsFrozen && FrozenSelections?.TryGetValue(serviceType, out trialKey) == true)
        {
            return true;
        }

        // Then check forced selections
        if (ForcedSelections?.TryGetValue(serviceType, out trialKey) == true)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Freezes the current selection for a service type.
    /// </summary>
    public static void FreezeSelection(Type serviceType, string trialKey)
    {
        FrozenSelections ??= new Dictionary<Type, string>();
        FrozenSelections[serviceType] = trialKey;
    }
}
