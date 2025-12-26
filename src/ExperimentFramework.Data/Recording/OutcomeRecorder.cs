using ExperimentFramework.Data.Models;
using ExperimentFramework.Data.Storage;

namespace ExperimentFramework.Data.Recording;

/// <summary>
/// Default implementation of <see cref="IOutcomeRecorder"/>.
/// </summary>
public sealed class OutcomeRecorder : IOutcomeRecorder
{
    private readonly IOutcomeStore _store;
    private readonly OutcomeRecorderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutcomeRecorder"/> class.
    /// </summary>
    /// <param name="store">The outcome store.</param>
    /// <param name="options">The recorder options.</param>
    public OutcomeRecorder(IOutcomeStore store, OutcomeRecorderOptions? options = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options ?? new OutcomeRecorderOptions();
    }

    /// <inheritdoc />
    public ValueTask RecordBinaryAsync(
        string experimentName,
        string trialKey,
        string subjectId,
        string metricName,
        bool success,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var outcome = CreateOutcome(
            experimentName,
            trialKey,
            subjectId,
            metricName,
            OutcomeType.Binary,
            success ? 1.0 : 0.0,
            metadata);

        return _store.RecordAsync(outcome, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask RecordContinuousAsync(
        string experimentName,
        string trialKey,
        string subjectId,
        string metricName,
        double value,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var outcome = CreateOutcome(
            experimentName,
            trialKey,
            subjectId,
            metricName,
            OutcomeType.Continuous,
            value,
            metadata);

        return _store.RecordAsync(outcome, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask RecordCountAsync(
        string experimentName,
        string trialKey,
        string subjectId,
        string metricName,
        int count,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var outcome = CreateOutcome(
            experimentName,
            trialKey,
            subjectId,
            metricName,
            OutcomeType.Count,
            count,
            metadata);

        return _store.RecordAsync(outcome, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask RecordDurationAsync(
        string experimentName,
        string trialKey,
        string subjectId,
        string metricName,
        TimeSpan duration,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var outcome = CreateOutcome(
            experimentName,
            trialKey,
            subjectId,
            metricName,
            OutcomeType.Duration,
            duration.TotalSeconds,
            metadata);

        return _store.RecordAsync(outcome, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask RecordAsync(
        ExperimentOutcome outcome,
        CancellationToken cancellationToken = default)
    {
        return _store.RecordAsync(outcome, cancellationToken);
    }

    private ExperimentOutcome CreateOutcome(
        string experimentName,
        string trialKey,
        string subjectId,
        string metricName,
        OutcomeType outcomeType,
        double value,
        IReadOnlyDictionary<string, object>? metadata)
    {
        return new ExperimentOutcome
        {
            Id = _options.AutoGenerateIds ? Guid.NewGuid().ToString("N") : string.Empty,
            ExperimentName = experimentName,
            TrialKey = trialKey,
            SubjectId = subjectId,
            MetricName = metricName,
            OutcomeType = outcomeType,
            Value = value,
            Timestamp = _options.AutoSetTimestamps ? DateTimeOffset.UtcNow : default,
            Metadata = metadata
        };
    }
}
