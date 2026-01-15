namespace ExperimentFramework.Testing;

/// <summary>
/// Provides a scoped context for forcing trial selection in tests.
/// </summary>
/// <example>
/// <code>
/// using var scope = ExperimentTestScope.Begin()
///     .ForceCondition&lt;IMyDatabase&gt;("true")
///     .FreezeSelection();
/// 
/// var db = services.GetRequiredService&lt;IMyDatabase&gt;();
/// await db.QueryAsync(); // Will always use "true" condition
/// </code>
/// </example>
public sealed class ExperimentTestScope : IDisposable
{
    private readonly Dictionary<Type, string>? _previousForcedSelections;
    private readonly Dictionary<Type, string>? _previousFrozenSelections;
    private readonly bool _previousIsFrozen;
    private bool _disposed;

    private ExperimentTestScope()
    {
        // Save previous state
        _previousForcedSelections = TestSelectionContext.ForcedSelections;
        _previousFrozenSelections = TestSelectionContext.FrozenSelections;
        _previousIsFrozen = TestSelectionContext.IsFrozen;

        // Initialize new state
        TestSelectionContext.ForcedSelections = new Dictionary<Type, string>();
        TestSelectionContext.FrozenSelections = new Dictionary<Type, string>();
        TestSelectionContext.IsFrozen = false;
    }

    /// <summary>
    /// Begins a new test scope for forcing trial selection.
    /// </summary>
    /// <returns>A new test scope that can be configured and disposed.</returns>
    public static ExperimentTestScope Begin()
    {
        return new ExperimentTestScope();
    }

    /// <summary>
    /// Forces the control implementation to be selected for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <returns>This scope for fluent chaining.</returns>
    public ExperimentTestScope ForceControl<TService>()
        where TService : class
    {
        return ForceTrialKey<TService>("control");
    }

    /// <summary>
    /// Forces a specific condition to be selected for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="conditionKey">The condition key (e.g., "true", "variant-a").</param>
    /// <returns>This scope for fluent chaining.</returns>
    public ExperimentTestScope ForceCondition<TService>(string conditionKey)
        where TService : class
    {
        return ForceTrialKey<TService>(conditionKey);
    }

    /// <summary>
    /// Forces a specific trial key to be selected for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="trialKey">The trial key to force.</param>
    /// <returns>This scope for fluent chaining.</returns>
    public ExperimentTestScope ForceTrialKey<TService>(string trialKey)
        where TService : class
    {
        ArgumentException.ThrowIfNullOrEmpty(trialKey);

        TestSelectionContext.ForcedSelections ??= new Dictionary<Type, string>();
        TestSelectionContext.ForcedSelections[typeof(TService)] = trialKey;

        return this;
    }

    /// <summary>
    /// Freezes selection so that the first selected trial key for each service
    /// is consistently used for the remainder of the scope.
    /// </summary>
    /// <returns>This scope for fluent chaining.</returns>
    public ExperimentTestScope FreezeSelection()
    {
        TestSelectionContext.IsFrozen = true;
        return this;
    }

    /// <summary>
    /// Disposes the scope and restores the previous selection context.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // Restore previous state
        TestSelectionContext.ForcedSelections = _previousForcedSelections;
        TestSelectionContext.FrozenSelections = _previousFrozenSelections;
        TestSelectionContext.IsFrozen = _previousIsFrozen;

        _disposed = true;
    }
}
