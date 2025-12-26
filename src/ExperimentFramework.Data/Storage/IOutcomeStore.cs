using ExperimentFramework.Data.Models;

namespace ExperimentFramework.Data.Storage;

/// <summary>
/// Defines the contract for storing and retrieving experiment outcomes.
/// </summary>
/// <remarks>
/// <para>
/// Implementations must be thread-safe for concurrent read/write operations.
/// </para>
/// <para>
/// The store is responsible for:
/// <list type="bullet">
/// <item><description>Persisting individual outcomes</description></item>
/// <item><description>Querying outcomes with filtering</description></item>
/// <item><description>Computing aggregated statistics efficiently</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IOutcomeStore
{
    /// <summary>
    /// Records an outcome asynchronously.
    /// </summary>
    /// <param name="outcome">The outcome to record.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the outcome is recorded.</returns>
    ValueTask RecordAsync(ExperimentOutcome outcome, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records multiple outcomes in batch.
    /// </summary>
    /// <param name="outcomes">The outcomes to record.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when all outcomes are recorded.</returns>
    ValueTask RecordBatchAsync(IEnumerable<ExperimentOutcome> outcomes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves outcomes matching the query.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of matching outcomes.</returns>
    ValueTask<IReadOnlyList<ExperimentOutcome>> QueryAsync(OutcomeQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated statistics for an experiment's metric, grouped by trial.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="metricName">The metric name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A dictionary mapping trial keys to their aggregated statistics.</returns>
    ValueTask<IReadOnlyDictionary<string, OutcomeAggregation>> GetAggregationsAsync(
        string experimentName,
        string metricName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the distinct trial keys for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of trial keys.</returns>
    ValueTask<IReadOnlyList<string>> GetTrialKeysAsync(
        string experimentName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the distinct metric names for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of metric names.</returns>
    ValueTask<IReadOnlyList<string>> GetMetricNamesAsync(
        string experimentName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of outcomes matching the query.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The count of matching outcomes.</returns>
    ValueTask<long> CountAsync(OutcomeQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all outcomes matching the query.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of deleted outcomes.</returns>
    ValueTask<long> DeleteAsync(OutcomeQuery query, CancellationToken cancellationToken = default);
}
