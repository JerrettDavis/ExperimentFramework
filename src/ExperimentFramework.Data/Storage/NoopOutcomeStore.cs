using ExperimentFramework.Data.Models;

namespace ExperimentFramework.Data.Storage;

/// <summary>
/// A no-operation outcome store that discards all data.
/// </summary>
/// <remarks>
/// <para>
/// Use this implementation when outcome collection is not needed,
/// providing zero overhead for recording operations.
/// </para>
/// <para>
/// All query operations return empty results.
/// </para>
/// </remarks>
public sealed class NoopOutcomeStore : IOutcomeStore
{
    /// <summary>
    /// Gets the singleton instance of the no-op store.
    /// </summary>
    public static readonly NoopOutcomeStore Instance = new();

    private static readonly IReadOnlyList<ExperimentOutcome> EmptyOutcomes = Array.Empty<ExperimentOutcome>();
    private static readonly IReadOnlyList<string> EmptyStrings = Array.Empty<string>();
    private static readonly IReadOnlyDictionary<string, OutcomeAggregation> EmptyAggregations =
        new Dictionary<string, OutcomeAggregation>();

    private NoopOutcomeStore() { }

    /// <inheritdoc />
    public ValueTask RecordAsync(ExperimentOutcome outcome, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask RecordBatchAsync(IEnumerable<ExperimentOutcome> outcomes, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<ExperimentOutcome>> QueryAsync(OutcomeQuery query, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(EmptyOutcomes);

    /// <inheritdoc />
    public ValueTask<IReadOnlyDictionary<string, OutcomeAggregation>> GetAggregationsAsync(
        string experimentName,
        string metricName,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(EmptyAggregations);

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<string>> GetTrialKeysAsync(
        string experimentName,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(EmptyStrings);

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<string>> GetMetricNamesAsync(
        string experimentName,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(EmptyStrings);

    /// <inheritdoc />
    public ValueTask<long> CountAsync(OutcomeQuery query, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(0L);

    /// <inheritdoc />
    public ValueTask<long> DeleteAsync(OutcomeQuery query, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(0L);
}
