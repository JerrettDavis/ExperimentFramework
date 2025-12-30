namespace ExperimentFramework.Governance.Persistence;

/// <summary>
/// Exception thrown when an optimistic concurrency conflict is detected.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    /// <summary>
    /// Gets the experiment name involved in the conflict.
    /// </summary>
    public string ExperimentName { get; }

    /// <summary>
    /// Gets the expected ETag.
    /// </summary>
    public string ExpectedETag { get; }

    /// <summary>
    /// Gets the actual ETag (if available).
    /// </summary>
    public string? ActualETag { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class.
    /// </summary>
    public ConcurrencyConflictException(
        string experimentName,
        string expectedETag,
        string? actualETag = null)
        : base($"Concurrency conflict for experiment '{experimentName}'. Expected ETag: {expectedETag}, Actual: {actualETag ?? "unknown"}")
    {
        ExperimentName = experimentName;
        ExpectedETag = expectedETag;
        ActualETag = actualETag;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class with inner exception.
    /// </summary>
    public ConcurrencyConflictException(
        string experimentName,
        string expectedETag,
        string? actualETag,
        Exception innerException)
        : base($"Concurrency conflict for experiment '{experimentName}'. Expected ETag: {expectedETag}, Actual: {actualETag ?? "unknown"}", innerException)
    {
        ExperimentName = experimentName;
        ExpectedETag = expectedETag;
        ActualETag = actualETag;
    }
}
