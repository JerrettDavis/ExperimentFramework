using System.Collections.Concurrent;
using ExperimentFramework.Audit;
using Microsoft.EntityFrameworkCore;

namespace AspireDemo.ApiService.Data;

/// <summary>
/// Audit sink that persists events to SQLite via EF Core.
/// Maintains an in-memory cache for fast reads on recent events.
/// </summary>
public class PersistentAuditSink : IAuditSink
{
    private readonly IDbContextFactory<ExperimentDbContext> _contextFactory;
    private readonly ILogger<PersistentAuditSink> _logger;
    private readonly ConcurrentQueue<AuditLogEntry> _recentEvents = new();
    private const int MaxCachedEvents = 1000;

    public PersistentAuditSink(
        IDbContextFactory<ExperimentDbContext> contextFactory,
        ILogger<PersistentAuditSink> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the sink by loading recent events from the database.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var recentEvents = await context.AuditEvents
                .OrderByDescending(e => e.Timestamp)
                .Take(MaxCachedEvents)
                .ToListAsync();

            // Load into cache in chronological order
            foreach (var entity in recentEvents.OrderBy(e => e.Timestamp))
            {
                _recentEvents.Enqueue(MapToLogEntry(entity));
            }

            _logger.LogInformation("Loaded {Count} recent audit events from database", recentEvents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load audit events from database");
        }
    }

    public async ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = auditEvent.EventType.ToString(),
            ExperimentName = auditEvent.ExperimentName,
            TrialName = auditEvent.SelectedTrialKey,
            Details = FormatDetails(auditEvent)
        };

        // Add to in-memory cache
        _recentEvents.Enqueue(entry);
        while (_recentEvents.Count > MaxCachedEvents && _recentEvents.TryDequeue(out _)) { }

        // Persist asynchronously
        _ = PersistEventAsync(entry);
    }

    /// <summary>
    /// Gets recent audit events from the in-memory cache.
    /// </summary>
    public IEnumerable<AuditLogEntry> GetEvents(int limit) =>
        _recentEvents.Reverse().Take(limit).ToList();

    /// <summary>
    /// Gets all audit events from the database.
    /// </summary>
    public async Task<List<AuditLogEntry>> GetEventsFromDatabaseAsync(int limit)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var events = await context.AuditEvents
                .OrderByDescending(e => e.Timestamp)
                .Take(limit)
                .ToListAsync();

            return events.Select(MapToLogEntry).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get audit events from database");
            return GetEvents(limit).ToList();
        }
    }

    private async Task PersistEventAsync(AuditLogEntry entry)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.AuditEvents.Add(new AuditEventEntity
            {
                Timestamp = entry.Timestamp,
                EventType = entry.EventType,
                ExperimentName = entry.ExperimentName,
                TrialName = entry.TrialName,
                Details = entry.Details
            });
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist audit event");
        }
    }

    private static AuditLogEntry MapToLogEntry(AuditEventEntity entity) => new()
    {
        Timestamp = entity.Timestamp,
        EventType = entity.EventType,
        ExperimentName = entity.ExperimentName,
        TrialName = entity.TrialName,
        Details = entity.Details
    };

    private static string FormatDetails(AuditEvent auditEvent)
    {
        if (auditEvent.Details == null || auditEvent.Details.Count == 0)
            return auditEvent.ToString() ?? "";

        return string.Join(", ", auditEvent.Details.Select(kv => $"{kv.Key}={kv.Value}"));
    }
}

/// <summary>
/// DTO for audit log entries returned by the API.
/// </summary>
public class AuditLogEntry
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? ExperimentName { get; set; }
    public string? TrialName { get; set; }
    public string Details { get; set; } = string.Empty;
}
