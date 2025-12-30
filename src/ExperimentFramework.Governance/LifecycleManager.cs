using ExperimentFramework.Audit;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Governance;

/// <summary>
/// Default implementation of lifecycle manager with configurable state transition rules.
/// </summary>
public class LifecycleManager : ILifecycleManager
{
    private readonly ILogger<LifecycleManager> _logger;
    private readonly IAuditSink? _auditSink;
    private readonly Dictionary<string, List<StateTransition>> _history = new();
    private readonly Dictionary<ExperimentLifecycleState, HashSet<ExperimentLifecycleState>> _allowedTransitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LifecycleManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditSink">Optional audit sink for recording transitions.</param>
    public LifecycleManager(
        ILogger<LifecycleManager> logger,
        IAuditSink? auditSink = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditSink = auditSink;
        _allowedTransitions = BuildDefaultTransitionRules();
    }

    /// <inheritdoc/>
    public ExperimentLifecycleState? GetState(string experimentName)
    {
        if (string.IsNullOrWhiteSpace(experimentName))
            throw new ArgumentException("Experiment name cannot be null or empty.", nameof(experimentName));

        lock (_history)
        {
            if (!_history.TryGetValue(experimentName, out var transitions) || transitions.Count == 0)
                return null;

            return transitions[^1].ToState;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<StateTransition> GetHistory(string experimentName)
    {
        if (string.IsNullOrWhiteSpace(experimentName))
            throw new ArgumentException("Experiment name cannot be null or empty.", nameof(experimentName));

        lock (_history)
        {
            if (!_history.TryGetValue(experimentName, out var transitions))
                return Array.Empty<StateTransition>();

            return transitions.ToList().AsReadOnly();
        }
    }

    /// <inheritdoc/>
    public async Task TransitionAsync(
        string experimentName,
        ExperimentLifecycleState toState,
        string? actor = null,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(experimentName))
            throw new ArgumentException("Experiment name cannot be null or empty.", nameof(experimentName));

        var currentState = GetState(experimentName) ?? ExperimentLifecycleState.Draft;

        if (!CanTransition(experimentName, toState))
        {
            throw new InvalidOperationException(
                $"Invalid state transition for experiment '{experimentName}': {currentState} -> {toState}");
        }

        var transition = new StateTransition
        {
            FromState = currentState,
            ToState = toState,
            Timestamp = DateTimeOffset.UtcNow,
            Actor = actor,
            Reason = reason,
            Metadata = metadata
        };

        lock (_history)
        {
            if (!_history.ContainsKey(experimentName))
            {
                _history[experimentName] = new List<StateTransition>();
            }

            _history[experimentName].Add(transition);
        }

        _logger.LogInformation(
            "Experiment '{ExperimentName}' transitioned from {FromState} to {ToState} by {Actor}. Reason: {Reason}",
            experimentName, currentState, toState, actor ?? "system", reason ?? "none");

        // Emit audit event
        if (_auditSink != null)
        {
            var auditEvent = new AuditEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Timestamp = transition.Timestamp,
                EventType = AuditEventType.ExperimentModified,
                ExperimentName = experimentName,
                Actor = actor,
                Details = new Dictionary<string, object>
                {
                    ["lifecycleTransition"] = new
                    {
                        from = currentState.ToString(),
                        to = toState.ToString(),
                        reason = reason ?? "none"
                    }
                }
            };

            await _auditSink.RecordAsync(auditEvent, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public bool CanTransition(string experimentName, ExperimentLifecycleState toState)
    {
        if (string.IsNullOrWhiteSpace(experimentName))
            throw new ArgumentException("Experiment name cannot be null or empty.", nameof(experimentName));

        var currentState = GetState(experimentName) ?? ExperimentLifecycleState.Draft;

        return _allowedTransitions.TryGetValue(currentState, out var allowed) && allowed.Contains(toState);
    }

    /// <inheritdoc/>
    public IReadOnlyList<ExperimentLifecycleState> GetAllowedTransitions(string experimentName)
    {
        if (string.IsNullOrWhiteSpace(experimentName))
            throw new ArgumentException("Experiment name cannot be null or empty.", nameof(experimentName));

        var currentState = GetState(experimentName) ?? ExperimentLifecycleState.Draft;

        if (_allowedTransitions.TryGetValue(currentState, out var allowed))
        {
            return allowed.ToList().AsReadOnly();
        }

        return Array.Empty<ExperimentLifecycleState>();
    }

    /// <summary>
    /// Builds the default state transition rules.
    /// </summary>
    /// <returns>A dictionary of allowed state transitions.</returns>
    private static Dictionary<ExperimentLifecycleState, HashSet<ExperimentLifecycleState>> BuildDefaultTransitionRules()
    {
        return new Dictionary<ExperimentLifecycleState, HashSet<ExperimentLifecycleState>>
        {
            [ExperimentLifecycleState.Draft] = new()
            {
                ExperimentLifecycleState.PendingApproval,
                ExperimentLifecycleState.Archived // Can archive drafts that are no longer needed
            },
            [ExperimentLifecycleState.PendingApproval] = new()
            {
                ExperimentLifecycleState.Approved,
                ExperimentLifecycleState.Rejected,
                ExperimentLifecycleState.Draft // Can return to draft for revisions
            },
            [ExperimentLifecycleState.Approved] = new()
            {
                ExperimentLifecycleState.Running,
                ExperimentLifecycleState.Ramping,
                ExperimentLifecycleState.Archived // Can archive approved experiments that won't run
            },
            [ExperimentLifecycleState.Running] = new()
            {
                ExperimentLifecycleState.Ramping,
                ExperimentLifecycleState.Paused,
                ExperimentLifecycleState.RolledBack,
                ExperimentLifecycleState.Archived
            },
            [ExperimentLifecycleState.Ramping] = new()
            {
                ExperimentLifecycleState.Running,
                ExperimentLifecycleState.Paused,
                ExperimentLifecycleState.RolledBack,
                ExperimentLifecycleState.Archived
            },
            [ExperimentLifecycleState.Paused] = new()
            {
                ExperimentLifecycleState.Running,
                ExperimentLifecycleState.Ramping,
                ExperimentLifecycleState.RolledBack,
                ExperimentLifecycleState.Archived
            },
            [ExperimentLifecycleState.RolledBack] = new()
            {
                ExperimentLifecycleState.Draft, // Can fix and re-propose
                ExperimentLifecycleState.Archived
            },
            [ExperimentLifecycleState.Rejected] = new()
            {
                ExperimentLifecycleState.Draft, // Can revise and resubmit
                ExperimentLifecycleState.Archived
            },
            [ExperimentLifecycleState.Archived] = new()
            {
                // Terminal state - no transitions out
            }
        };
    }
}
