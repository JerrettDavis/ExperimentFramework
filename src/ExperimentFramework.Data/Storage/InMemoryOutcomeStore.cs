using System.Collections.Concurrent;
using ExperimentFramework.Data.Models;

namespace ExperimentFramework.Data.Storage;

/// <summary>
/// A thread-safe in-memory implementation of <see cref="IOutcomeStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is suitable for:
/// <list type="bullet">
/// <item><description>Development and testing</description></item>
/// <item><description>Single-instance deployments</description></item>
/// <item><description>Short-lived experiments</description></item>
/// </list>
/// </para>
/// <para>
/// Data is not persisted across application restarts.
/// For production use with persistence, use a database-backed implementation.
/// </para>
/// </remarks>
public sealed class InMemoryOutcomeStore : IOutcomeStore
{
    private readonly ConcurrentDictionary<string, ExperimentOutcome> _outcomes = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, OutcomeAggregation>> _aggregations = new();
    private readonly object _aggregationLock = new();

    /// <inheritdoc />
    public ValueTask RecordAsync(ExperimentOutcome outcome, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _outcomes.TryAdd(outcome.Id, outcome);
        UpdateAggregation(outcome);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RecordBatchAsync(IEnumerable<ExperimentOutcome> outcomes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var outcome in outcomes)
        {
            _outcomes.TryAdd(outcome.Id, outcome);
            UpdateAggregation(outcome);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<ExperimentOutcome>> QueryAsync(OutcomeQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = _outcomes.Values.AsEnumerable();

        // Apply filters
        if (!string.IsNullOrEmpty(query.ExperimentName))
            results = results.Where(o => o.ExperimentName == query.ExperimentName);

        if (!string.IsNullOrEmpty(query.TrialKey))
            results = results.Where(o => o.TrialKey == query.TrialKey);

        if (!string.IsNullOrEmpty(query.MetricName))
            results = results.Where(o => o.MetricName == query.MetricName);

        if (!string.IsNullOrEmpty(query.SubjectId))
            results = results.Where(o => o.SubjectId == query.SubjectId);

        if (query.OutcomeType.HasValue)
            results = results.Where(o => o.OutcomeType == query.OutcomeType.Value);

        if (query.FromTimestamp.HasValue)
            results = results.Where(o => o.Timestamp >= query.FromTimestamp.Value);

        if (query.ToTimestamp.HasValue)
            results = results.Where(o => o.Timestamp < query.ToTimestamp.Value);

        // Apply ordering
        results = query.OrderByTimestampDescending
            ? results.OrderByDescending(o => o.Timestamp)
            : results.OrderBy(o => o.Timestamp);

        // Apply pagination
        if (query.Offset.HasValue)
            results = results.Skip(query.Offset.Value);

        if (query.Limit.HasValue)
            results = results.Take(query.Limit.Value);

        return ValueTask.FromResult<IReadOnlyList<ExperimentOutcome>>(results.ToList());
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyDictionary<string, OutcomeAggregation>> GetAggregationsAsync(
        string experimentName,
        string metricName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = GetAggregationKey(experimentName, metricName);

        if (_aggregations.TryGetValue(key, out var trialAggregations))
        {
            return ValueTask.FromResult<IReadOnlyDictionary<string, OutcomeAggregation>>(
                new Dictionary<string, OutcomeAggregation>(trialAggregations));
        }

        return ValueTask.FromResult<IReadOnlyDictionary<string, OutcomeAggregation>>(
            new Dictionary<string, OutcomeAggregation>());
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<string>> GetTrialKeysAsync(
        string experimentName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var trialKeys = _outcomes.Values
            .Where(o => o.ExperimentName == experimentName)
            .Select(o => o.TrialKey)
            .Distinct()
            .OrderBy(k => k)
            .ToList();

        return ValueTask.FromResult<IReadOnlyList<string>>(trialKeys);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<string>> GetMetricNamesAsync(
        string experimentName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var metricNames = _outcomes.Values
            .Where(o => o.ExperimentName == experimentName)
            .Select(o => o.MetricName)
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        return ValueTask.FromResult<IReadOnlyList<string>>(metricNames);
    }

    /// <inheritdoc />
    public ValueTask<long> CountAsync(OutcomeQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = _outcomes.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(query.ExperimentName))
            results = results.Where(o => o.ExperimentName == query.ExperimentName);

        if (!string.IsNullOrEmpty(query.TrialKey))
            results = results.Where(o => o.TrialKey == query.TrialKey);

        if (!string.IsNullOrEmpty(query.MetricName))
            results = results.Where(o => o.MetricName == query.MetricName);

        if (!string.IsNullOrEmpty(query.SubjectId))
            results = results.Where(o => o.SubjectId == query.SubjectId);

        if (query.OutcomeType.HasValue)
            results = results.Where(o => o.OutcomeType == query.OutcomeType.Value);

        if (query.FromTimestamp.HasValue)
            results = results.Where(o => o.Timestamp >= query.FromTimestamp.Value);

        if (query.ToTimestamp.HasValue)
            results = results.Where(o => o.Timestamp < query.ToTimestamp.Value);

        return ValueTask.FromResult((long)results.Count());
    }

    /// <inheritdoc />
    public ValueTask<long> DeleteAsync(OutcomeQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var toDelete = _outcomes.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(query.ExperimentName))
            toDelete = toDelete.Where(o => o.ExperimentName == query.ExperimentName);

        if (!string.IsNullOrEmpty(query.TrialKey))
            toDelete = toDelete.Where(o => o.TrialKey == query.TrialKey);

        if (!string.IsNullOrEmpty(query.MetricName))
            toDelete = toDelete.Where(o => o.MetricName == query.MetricName);

        if (!string.IsNullOrEmpty(query.SubjectId))
            toDelete = toDelete.Where(o => o.SubjectId == query.SubjectId);

        if (query.OutcomeType.HasValue)
            toDelete = toDelete.Where(o => o.OutcomeType == query.OutcomeType.Value);

        if (query.FromTimestamp.HasValue)
            toDelete = toDelete.Where(o => o.Timestamp >= query.FromTimestamp.Value);

        if (query.ToTimestamp.HasValue)
            toDelete = toDelete.Where(o => o.Timestamp < query.ToTimestamp.Value);

        var idsToDelete = toDelete.Select(o => o.Id).ToList();
        long deleted = idsToDelete.Count(id => _outcomes.TryRemove(id, out _));

        // Note: Aggregations are not updated on delete for simplicity
        // In a production implementation, you might want to rebuild aggregations

        return ValueTask.FromResult(deleted);
    }

    /// <summary>
    /// Clears all stored outcomes and aggregations.
    /// </summary>
    public void Clear()
    {
        _outcomes.Clear();
        _aggregations.Clear();
    }

    private void UpdateAggregation(ExperimentOutcome outcome)
    {
        var key = GetAggregationKey(outcome.ExperimentName, outcome.MetricName);

        var trialAggregations = _aggregations.GetOrAdd(key, _ => new ConcurrentDictionary<string, OutcomeAggregation>());

        lock (_aggregationLock)
        {
            var existing = trialAggregations.GetOrAdd(
                outcome.TrialKey,
                _ => OutcomeAggregation.Empty(outcome.TrialKey, outcome.MetricName));

            var isSuccess = outcome.OutcomeType == OutcomeType.Binary && outcome.Value >= 0.5;
            var updated = existing.WithValue(outcome.Value, isSuccess, outcome.Timestamp);

            trialAggregations[outcome.TrialKey] = updated;
        }
    }

    private static string GetAggregationKey(string experimentName, string metricName) =>
        $"{experimentName}::{metricName}";
}
