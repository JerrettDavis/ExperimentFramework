namespace ExperimentFramework.Governance.Persistence;

/// <summary>
/// Represents the result of a persistence operation with optimistic concurrency tracking.
/// </summary>
/// <typeparam name="T">The type of the persisted entity.</typeparam>
public sealed class PersistenceResult<T>
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets or sets the persisted entity (if successful).
    /// </summary>
    public T? Entity { get; init; }

    /// <summary>
    /// Gets or sets the new ETag after the operation.
    /// </summary>
    public string? NewETag { get; init; }

    /// <summary>
    /// Gets or sets whether a concurrency conflict was detected.
    /// </summary>
    public bool ConflictDetected { get; init; }

    /// <summary>
    /// Gets or sets the error message (if not successful).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static PersistenceResult<T> Ok(T entity, string newETag) =>
        new()
        {
            Success = true,
            Entity = entity,
            NewETag = newETag,
            ConflictDetected = false
        };

    /// <summary>
    /// Creates a conflict result.
    /// </summary>
    public static PersistenceResult<T> Conflict(string message = "Concurrency conflict detected") =>
        new()
        {
            Success = false,
            ConflictDetected = true,
            ErrorMessage = message
        };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static PersistenceResult<T> Failure(string message) =>
        new()
        {
            Success = false,
            ConflictDetected = false,
            ErrorMessage = message
        };
}
