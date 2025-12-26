using ExperimentFramework.Data.Models;

namespace ExperimentFramework.Data.Recording;

/// <summary>
/// High-level interface for recording experiment outcomes.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a convenient API for recording different types
/// of outcomes without having to construct <see cref="ExperimentOutcome"/>
/// objects directly.
/// </para>
/// <para>
/// For direct access to the underlying storage, use <see cref="Storage.IOutcomeStore"/>.
/// </para>
/// </remarks>
public interface IOutcomeRecorder
{
    /// <summary>
    /// Records a binary outcome (success/failure).
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="trialKey">The trial key the subject was assigned to.</param>
    /// <param name="subjectId">The unique identifier for the subject.</param>
    /// <param name="metricName">The name of the metric.</param>
    /// <param name="success">Whether the outcome was successful.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask RecordBinaryAsync(
        string experimentName,
        string trialKey,
        string subjectId,
        string metricName,
        bool success,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a continuous outcome.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="trialKey">The trial key the subject was assigned to.</param>
    /// <param name="subjectId">The unique identifier for the subject.</param>
    /// <param name="metricName">The name of the metric.</param>
    /// <param name="value">The measured value.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask RecordContinuousAsync(
        string experimentName,
        string trialKey,
        string subjectId,
        string metricName,
        double value,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a count outcome.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="trialKey">The trial key the subject was assigned to.</param>
    /// <param name="subjectId">The unique identifier for the subject.</param>
    /// <param name="metricName">The name of the metric.</param>
    /// <param name="count">The count value.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask RecordCountAsync(
        string experimentName,
        string trialKey,
        string subjectId,
        string metricName,
        int count,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a duration outcome.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="trialKey">The trial key the subject was assigned to.</param>
    /// <param name="subjectId">The unique identifier for the subject.</param>
    /// <param name="metricName">The name of the metric.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask RecordDurationAsync(
        string experimentName,
        string trialKey,
        string subjectId,
        string metricName,
        TimeSpan duration,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a raw outcome with full control over all properties.
    /// </summary>
    /// <param name="outcome">The outcome to record.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask RecordAsync(
        ExperimentOutcome outcome,
        CancellationToken cancellationToken = default);
}
