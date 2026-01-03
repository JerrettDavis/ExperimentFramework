namespace AspireDemo.Web.Services;

/// <summary>
/// Centralized state service for the demo application.
/// Maintains UI state across page navigation within a user session.
/// </summary>
public class DemoStateService
{
    // Kill switch states (experiment name -> is killed)
    private readonly Dictionary<string, bool> _killSwitches = new();

    // Expanded sections (page:itemId format)
    private readonly HashSet<string> _expandedItems = new();

    // Filter/search state for experiments page
    private string? _experimentFilterCategory;
    private string _experimentSearchQuery = string.Empty;

    // Active variants cache (synced with API)
    private readonly Dictionary<string, string> _activeVariants = new();

    // Active plugin implementations cache
    private readonly Dictionary<string, ActivePluginImplementation> _activePluginImplementations = new();

    /// <summary>
    /// Raised when any state changes, allowing components to re-render.
    /// </summary>
    public event Action? OnStateChanged;

    /// <summary>
    /// Raised when a specific kill switch changes.
    /// </summary>
    public event Action<string>? OnKillSwitchChanged;

    /// <summary>
    /// Raised when a variant changes.
    /// </summary>
    public event Action<string>? OnVariantChanged;

    // ============================================================================
    // Kill Switch Management
    // ============================================================================

    /// <summary>
    /// Gets the kill switch state for an experiment.
    /// </summary>
    public bool GetKillSwitch(string experimentName)
        => _killSwitches.GetValueOrDefault(experimentName, false);

    /// <summary>
    /// Sets the kill switch state for an experiment.
    /// </summary>
    public void SetKillSwitch(string experimentName, bool isKilled)
    {
        _killSwitches[experimentName] = isKilled;
        OnKillSwitchChanged?.Invoke(experimentName);
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Toggles the kill switch state for an experiment.
    /// </summary>
    public void ToggleKillSwitch(string experimentName)
        => SetKillSwitch(experimentName, !GetKillSwitch(experimentName));

    /// <summary>
    /// Gets all kill switch states.
    /// </summary>
    public IReadOnlyDictionary<string, bool> GetAllKillSwitches() => _killSwitches;

    /// <summary>
    /// Syncs kill switch states from API response.
    /// </summary>
    public void SyncKillSwitchesFromApi(IEnumerable<KillSwitchStatus> statuses)
    {
        foreach (var status in statuses)
        {
            _killSwitches[status.Experiment] = status.ExperimentDisabled;
        }
    }

    // ============================================================================
    // Expanded Items Management
    // ============================================================================

    /// <summary>
    /// Checks if an item is expanded on a specific page.
    /// </summary>
    public bool IsExpanded(string page, string itemId)
        => _expandedItems.Contains($"{page}:{itemId}");

    /// <summary>
    /// Sets the expanded state for an item on a specific page.
    /// </summary>
    public void SetExpanded(string page, string itemId, bool expanded)
    {
        var key = $"{page}:{itemId}";
        if (expanded)
            _expandedItems.Add(key);
        else
            _expandedItems.Remove(key);
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Toggles the expanded state for an item on a specific page.
    /// </summary>
    public void ToggleExpanded(string page, string itemId)
        => SetExpanded(page, itemId, !IsExpanded(page, itemId));

    // ============================================================================
    // Filter State
    // ============================================================================

    /// <summary>
    /// Gets or sets the experiment filter category.
    /// </summary>
    public string? ExperimentFilterCategory
    {
        get => _experimentFilterCategory;
        set
        {
            _experimentFilterCategory = value;
            OnStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Gets or sets the experiment search query.
    /// </summary>
    public string ExperimentSearchQuery
    {
        get => _experimentSearchQuery;
        set
        {
            _experimentSearchQuery = value;
            OnStateChanged?.Invoke();
        }
    }

    // ============================================================================
    // Variant Cache (for optimistic UI updates)
    // ============================================================================

    /// <summary>
    /// Gets the active variant for an experiment.
    /// </summary>
    public string GetActiveVariant(string experimentName)
        => _activeVariants.GetValueOrDefault(experimentName, "default");

    /// <summary>
    /// Sets the active variant for an experiment.
    /// </summary>
    public void SetActiveVariant(string experimentName, string variant)
    {
        _activeVariants[experimentName] = variant;
        OnVariantChanged?.Invoke(experimentName);
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Syncs active variants from API response.
    /// </summary>
    public void SyncVariantsFromApi(IEnumerable<ExperimentInfo> experiments)
    {
        foreach (var exp in experiments)
        {
            _activeVariants[exp.Name] = exp.ActiveVariant;
        }
    }

    // ============================================================================
    // Plugin Implementation Cache
    // ============================================================================

    /// <summary>
    /// Gets the active plugin implementation for an interface.
    /// </summary>
    public ActivePluginImplementation? GetActivePluginImplementation(string interfaceName)
        => _activePluginImplementations.GetValueOrDefault(interfaceName);

    /// <summary>
    /// Sets the active plugin implementation for an interface.
    /// </summary>
    public void SetActivePluginImplementation(string interfaceName, ActivePluginImplementation? impl)
    {
        if (impl == null)
            _activePluginImplementations.Remove(interfaceName);
        else
            _activePluginImplementations[interfaceName] = impl;
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Gets all active plugin implementations.
    /// </summary>
    public IReadOnlyDictionary<string, ActivePluginImplementation> GetAllActivePluginImplementations()
        => _activePluginImplementations;

    /// <summary>
    /// Syncs plugin implementations from API response.
    /// </summary>
    public void SyncPluginImplementationsFromApi(Dictionary<string, ActivePluginImplementation> impls)
    {
        _activePluginImplementations.Clear();
        foreach (var kvp in impls)
        {
            _activePluginImplementations[kvp.Key] = kvp.Value;
        }
    }
}
