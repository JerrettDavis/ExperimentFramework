using System.Text.Json;
using ExperimentFramework.DataPlane.Abstractions;
using ExperimentFramework.DataPlane.SqlServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.DataPlane.SqlServer;

/// <summary>
/// SQL Server-based data backplane for durable, queryable event storage.
/// </summary>
public sealed class SqlServerDataBackplane : IDataBackplane
{
    private readonly ExperimentDataContext _context;
    private readonly SqlServerDataBackplaneOptions _options;
    private readonly ILogger<SqlServerDataBackplane> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _batchLock;
    private readonly List<ExperimentEventEntity> _batchBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerDataBackplane"/> class.
    /// </summary>
    public SqlServerDataBackplane(
        ExperimentDataContext context,
        IOptions<SqlServerDataBackplaneOptions> options,
        ILogger<SqlServerDataBackplane> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _batchLock = new SemaphoreSlim(1, 1);
        _batchBuffer = new List<ExperimentEventEntity>();

        if (_options.AutoMigrate)
        {
            try
            {
                _context.Database.Migrate();
                _logger.LogInformation("SQL Server database migrations applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply SQL Server database migrations");
            }
        }

        _logger.LogInformation(
            "SQL Server data backplane initialized (Schema: {Schema}, Table: {Table})",
            _options.Schema,
            _options.TableName);
    }

    /// <inheritdoc />
    public async ValueTask PublishAsync(DataPlaneEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            if (envelope == null)
            {
                _logger.LogWarning("Attempted to publish null envelope");
                return;
            }

            var entity = CreateEntity(envelope);

            await _batchLock.WaitAsync(cancellationToken);
            try
            {
                _batchBuffer.Add(entity);

                if (_batchBuffer.Count >= _options.BatchSize)
                {
                    await FlushBatchAsync(cancellationToken);
                }
            }
            finally
            {
                _batchLock.Release();
            }

            _logger.LogDebug(
                "Queued event {EventId} for SQL Server storage",
                envelope.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventId} to SQL Server",
                envelope?.EventId);
        }
    }

    /// <inheritdoc />
    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        await _batchLock.WaitAsync(cancellationToken);
        try
        {
            if (_batchBuffer.Count > 0)
            {
                await FlushBatchAsync(cancellationToken);
            }

            _logger.LogDebug("Flushed pending SQL Server events");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush SQL Server events");
        }
        finally
        {
            _batchLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask<BackplaneHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to execute a simple query to check database connectivity
            await _context.Database.CanConnectAsync(cancellationToken);

            return BackplaneHealth.Healthy(
                $"SQL Server backplane is operational (Schema: {_options.Schema}, Table: {_options.TableName})");
        }
        catch (Exception ex)
        {
            return BackplaneHealth.Unhealthy(
                $"SQL Server backplane is unhealthy: {ex.Message}");
        }
    }

    private async Task FlushBatchAsync(CancellationToken cancellationToken)
    {
        if (_batchBuffer.Count == 0)
            return;

        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (_options.EnableIdempotency)
                {
                    // Filter out events that already exist
                    var eventIds = _batchBuffer.Select(e => e.EventId).ToList();
                    var existingIds = await _context.ExperimentEvents
                        .Where(e => eventIds.Contains(e.EventId))
                        .Select(e => e.EventId)
                        .ToListAsync(cancellationToken);

                    var newEvents = _batchBuffer
                        .Where(e => !existingIds.Contains(e.EventId))
                        .ToList();

                    if (newEvents.Count > 0)
                    {
                        await _context.ExperimentEvents.AddRangeAsync(newEvents, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    var skipped = _batchBuffer.Count - newEvents.Count;
                    if (skipped > 0)
                    {
                        _logger.LogDebug("Skipped {Count} duplicate events", skipped);
                    }
                }
                else
                {
                    await _context.ExperimentEvents.AddRangeAsync(_batchBuffer, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Saved batch of {EventCount} events to SQL Server",
                    _batchBuffer.Count);

                _batchBuffer.Clear();
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save batch of {EventCount} events to SQL Server",
                _batchBuffer.Count);

            // Clear buffer to avoid infinite retry
            _batchBuffer.Clear();
        }
    }

    private ExperimentEventEntity CreateEntity(DataPlaneEnvelope envelope)
    {
        var payloadJson = JsonSerializer.Serialize(envelope.Payload, _jsonOptions);
        var metadataJson = envelope.Metadata != null
            ? JsonSerializer.Serialize(envelope.Metadata, _jsonOptions)
            : null;

        return new ExperimentEventEntity
        {
            EventId = envelope.EventId,
            Timestamp = envelope.Timestamp,
            EventType = envelope.EventType.ToString(),
            SchemaVersion = envelope.SchemaVersion,
            PayloadJson = payloadJson,
            CorrelationId = envelope.CorrelationId,
            MetadataJson = metadataJson,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
