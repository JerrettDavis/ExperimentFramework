using ExperimentFramework.KillSwitch;
using Microsoft.EntityFrameworkCore;

namespace AspireDemo.ApiService.Data;

/// <summary>
/// Kill switch provider that persists state to SQLite via EF Core.
/// Maintains an in-memory cache for fast reads.
/// </summary>
public sealed class PersistentKillSwitchProvider : IKillSwitchProvider
{
    private readonly IDbContextFactory<ExperimentDbContext> _contextFactory;
    private readonly ILogger<PersistentKillSwitchProvider> _logger;

    private readonly HashSet<string> _disabledTrials = [];
    private readonly HashSet<string> _disabledExperiments = [];
    private readonly object _lock = new();

    public PersistentKillSwitchProvider(
        IDbContextFactory<ExperimentDbContext> contextFactory,
        ILogger<PersistentKillSwitchProvider> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the provider by loading persisted state from the database.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var switches = await context.KillSwitches.Where(k => k.IsDisabled).ToListAsync();

            lock (_lock)
            {
                foreach (var sw in switches)
                {
                    if (sw.TrialKey == null)
                        _disabledExperiments.Add(sw.ServiceTypeName);
                    else
                        _disabledTrials.Add($"{sw.ServiceTypeName}:{sw.TrialKey}");
                }
            }

            _logger.LogInformation("Loaded {Count} kill switch states from database", switches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load kill switch states from database");
        }
    }

    public bool IsTrialDisabled(Type serviceType, string trialKey)
    {
        lock (_lock)
        {
            return _disabledTrials.Contains(GetTrialKey(serviceType, trialKey));
        }
    }

    public bool IsExperimentDisabled(Type serviceType)
    {
        lock (_lock)
        {
            return _disabledExperiments.Contains(GetExperimentKey(serviceType));
        }
    }

    public void DisableTrial(Type serviceType, string trialKey)
    {
        var key = GetTrialKey(serviceType, trialKey);
        lock (_lock) { _disabledTrials.Add(key); }

        _ = PersistKillSwitchAsync(serviceType.FullName!, trialKey, true);
    }

    public void DisableExperiment(Type serviceType)
    {
        var key = GetExperimentKey(serviceType);
        lock (_lock) { _disabledExperiments.Add(key); }

        _ = PersistKillSwitchAsync(serviceType.FullName!, null, true);
    }

    public void EnableTrial(Type serviceType, string trialKey)
    {
        var key = GetTrialKey(serviceType, trialKey);
        lock (_lock) { _disabledTrials.Remove(key); }

        _ = PersistKillSwitchAsync(serviceType.FullName!, trialKey, false);
    }

    public void EnableExperiment(Type serviceType)
    {
        var key = GetExperimentKey(serviceType);
        lock (_lock) { _disabledExperiments.Remove(key); }

        _ = PersistKillSwitchAsync(serviceType.FullName!, null, false);
    }

    private async Task PersistKillSwitchAsync(string serviceTypeName, string? trialKey, bool disabled)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.KillSwitches
                .FirstOrDefaultAsync(k => k.ServiceTypeName == serviceTypeName && k.TrialKey == trialKey);

            if (existing != null)
            {
                existing.IsDisabled = disabled;
                existing.ModifiedAt = DateTime.UtcNow;
            }
            else
            {
                context.KillSwitches.Add(new KillSwitchEntity
                {
                    ServiceTypeName = serviceTypeName,
                    TrialKey = trialKey,
                    IsDisabled = disabled,
                    ModifiedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            _logger.LogDebug("Persisted kill switch: {Type}:{Trial} = {Disabled}", serviceTypeName, trialKey ?? "(experiment)", disabled);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist kill switch state");
        }
    }

    private static string GetTrialKey(Type serviceType, string trialKey)
        => $"{serviceType.FullName}:{trialKey}";

    private static string GetExperimentKey(Type serviceType)
        => serviceType.FullName ?? serviceType.Name;
}
