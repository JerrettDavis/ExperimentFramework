namespace ExperimentFramework.Validation;

/// <summary>
/// Exception thrown when conflicting trial registrations are detected.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown during framework configuration when:
/// <list type="bullet">
/// <item><description>Multiple trials are registered for the same service interface with overlapping time windows</description></item>
/// <item><description>Percentage allocations for a trial exceed 100%</description></item>
/// <item><description>Conflicting selection rules are detected</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class TrialConflictException : Exception
{
    /// <summary>
    /// Gets the conflicts that were detected.
    /// </summary>
    public IReadOnlyList<TrialConflict> Conflicts { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="TrialConflictException"/>.
    /// </summary>
    /// <param name="conflicts">The conflicts that were detected.</param>
    public TrialConflictException(IReadOnlyList<TrialConflict> conflicts)
        : base(BuildMessage(conflicts))
    {
        Conflicts = conflicts ?? throw new ArgumentNullException(nameof(conflicts));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TrialConflictException"/> with a single conflict.
    /// </summary>
    /// <param name="conflict">The conflict that was detected.</param>
    public TrialConflictException(TrialConflict conflict)
        : this(new List<TrialConflict> { conflict })
    {
    }

    private static string BuildMessage(IReadOnlyList<TrialConflict> conflicts)
    {
        if (conflicts == null || conflicts.Count == 0)
            return "Trial conflicts were detected.";

        if (conflicts.Count == 1)
            return $"Trial conflict detected: {conflicts[0].Description}";

        var descriptions = string.Join(Environment.NewLine + "  - ", conflicts.Select(c => c.Description));
        return $"Multiple trial conflicts detected:{Environment.NewLine}  - {descriptions}";
    }
}

/// <summary>
/// Represents a detected conflict between trial registrations.
/// </summary>
public sealed class TrialConflict
{
    /// <summary>
    /// Gets the type of conflict.
    /// </summary>
    public required TrialConflictType Type { get; init; }

    /// <summary>
    /// Gets the service type involved in the conflict.
    /// </summary>
    public required Type ServiceType { get; init; }

    /// <summary>
    /// Gets a human-readable description of the conflict.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the names of the experiments involved in the conflict, if applicable.
    /// </summary>
    public IReadOnlyList<string>? ExperimentNames { get; init; }
}

/// <summary>
/// Types of conflicts that can be detected between trials.
/// </summary>
public enum TrialConflictType
{
    /// <summary>
    /// Multiple trials for the same interface with overlapping time windows.
    /// </summary>
    OverlappingTimeWindows,

    /// <summary>
    /// Percentage allocations for conditions exceed 100%.
    /// </summary>
    ExcessivePercentageAllocation,

    /// <summary>
    /// Multiple trials for the same interface with no time differentiation.
    /// </summary>
    DuplicateServiceRegistration,

    /// <summary>
    /// A trial references a fallback condition key that doesn't exist.
    /// </summary>
    InvalidFallbackKey
}
