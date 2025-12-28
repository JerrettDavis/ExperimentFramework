using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Audit;

/// <summary>
/// Audit sink that writes to the logging infrastructure.
/// </summary>
public sealed class LoggingAuditSink : IAuditSink
{
    private readonly ILogger<LoggingAuditSink> _logger;
    private readonly LogLevel _logLevel;

    /// <summary>
    /// Creates a new logging audit sink.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="logLevel">The log level for audit events.</param>
    public LoggingAuditSink(ILogger<LoggingAuditSink> logger, LogLevel logLevel = LogLevel.Information)
    {
        _logger = logger;
        _logLevel = logLevel;
    }

    /// <inheritdoc />
    public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        _logger.Log(
            _logLevel,
            "Audit: {EventType} | Experiment: {ExperimentName} | Service: {ServiceType} | Trial: {TrialKey} | Actor: {Actor} | Details: {Details}",
            auditEvent.EventType,
            auditEvent.ExperimentName ?? "(none)",
            auditEvent.ServiceType ?? "(none)",
            auditEvent.SelectedTrialKey ?? "(none)",
            auditEvent.Actor ?? "(system)",
            auditEvent.Details != null ? JsonSerializer.Serialize(auditEvent.Details) : "{}");

        return ValueTask.CompletedTask;
    }
}
