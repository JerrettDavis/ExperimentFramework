namespace ExperimentFramework.Governance.Policy;

/// <summary>
/// Policy that enforces maximum traffic percentage until certain conditions are met.
/// </summary>
public class TrafficLimitPolicy : IExperimentPolicy
{
    private readonly double _maxTrafficPercentage;
    private readonly TimeSpan? _minStableTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrafficLimitPolicy"/> class.
    /// </summary>
    /// <param name="maxTrafficPercentage">Maximum allowed traffic percentage (0-100).</param>
    /// <param name="minStableTime">Minimum time experiment must run before exceeding limit.</param>
    public TrafficLimitPolicy(double maxTrafficPercentage, TimeSpan? minStableTime = null)
    {
        if (maxTrafficPercentage < 0 || maxTrafficPercentage > 100)
            throw new ArgumentOutOfRangeException(nameof(maxTrafficPercentage), "Must be between 0 and 100");

        _maxTrafficPercentage = maxTrafficPercentage;
        _minStableTime = minStableTime;
    }

    /// <inheritdoc/>
    public string Name => "TrafficLimit";

    /// <inheritdoc/>
    public string Description => $"Enforces maximum traffic of {_maxTrafficPercentage}%";

    /// <inheritdoc/>
    public Task<PolicyEvaluationResult> EvaluateAsync(PolicyContext context, CancellationToken cancellationToken = default)
    {
        if (context.Telemetry == null ||
            !context.Telemetry.TryGetValue("trafficPercentage", out var trafficObj) ||
            trafficObj is not double currentTraffic)
        {
            return Task.FromResult(new PolicyEvaluationResult
            {
                IsCompliant = true,
                PolicyName = Name,
                Reason = "No traffic data available"
            });
        }

        if (currentTraffic <= _maxTrafficPercentage)
        {
            return Task.FromResult(new PolicyEvaluationResult
            {
                IsCompliant = true,
                PolicyName = Name,
                Reason = $"Traffic {currentTraffic}% is within limit {_maxTrafficPercentage}%"
            });
        }

        // Check if minimum stable time has passed
        if (_minStableTime.HasValue &&
            context.Telemetry.TryGetValue("runningDuration", out var durationObj) &&
            durationObj is TimeSpan duration &&
            duration >= _minStableTime.Value)
        {
            return Task.FromResult(new PolicyEvaluationResult
            {
                IsCompliant = true,
                PolicyName = Name,
                Reason = $"Traffic {currentTraffic}% exceeds limit but stable time requirement met"
            });
        }

        return Task.FromResult(new PolicyEvaluationResult
        {
            IsCompliant = false,
            PolicyName = Name,
            Reason = $"Traffic {currentTraffic}% exceeds limit {_maxTrafficPercentage}%",
            Severity = PolicyViolationSeverity.Critical
        });
    }
}

/// <summary>
/// Policy that enforces maximum error rate threshold.
/// </summary>
public class ErrorRatePolicy : IExperimentPolicy
{
    private readonly double _maxErrorRate;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorRatePolicy"/> class.
    /// </summary>
    /// <param name="maxErrorRate">Maximum allowed error rate (0.0-1.0).</param>
    public ErrorRatePolicy(double maxErrorRate)
    {
        if (maxErrorRate < 0 || maxErrorRate > 1)
            throw new ArgumentOutOfRangeException(nameof(maxErrorRate), "Must be between 0 and 1");

        _maxErrorRate = maxErrorRate;
    }

    /// <inheritdoc/>
    public string Name => "ErrorRate";

    /// <inheritdoc/>
    public string Description => $"Enforces maximum error rate of {_maxErrorRate:P}";

    /// <inheritdoc/>
    public Task<PolicyEvaluationResult> EvaluateAsync(PolicyContext context, CancellationToken cancellationToken = default)
    {
        if (context.Telemetry == null ||
            !context.Telemetry.TryGetValue("errorRate", out var errorRateObj) ||
            errorRateObj is not double currentErrorRate)
        {
            return Task.FromResult(new PolicyEvaluationResult
            {
                IsCompliant = true,
                PolicyName = Name,
                Reason = "No error rate data available"
            });
        }

        if (currentErrorRate <= _maxErrorRate)
        {
            return Task.FromResult(new PolicyEvaluationResult
            {
                IsCompliant = true,
                PolicyName = Name,
                Reason = $"Error rate {currentErrorRate:P} is within limit {_maxErrorRate:P}"
            });
        }

        return Task.FromResult(new PolicyEvaluationResult
        {
            IsCompliant = false,
            PolicyName = Name,
            Reason = $"Error rate {currentErrorRate:P} exceeds limit {_maxErrorRate:P}",
            Severity = PolicyViolationSeverity.Critical
        });
    }
}

/// <summary>
/// Policy that restricts operations to specific time windows.
/// </summary>
public class TimeWindowPolicy : IExperimentPolicy
{
    private readonly TimeSpan _allowedStartTime;
    private readonly TimeSpan _allowedEndTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeWindowPolicy"/> class.
    /// </summary>
    /// <param name="allowedStartTime">Start of allowed time window (time of day).</param>
    /// <param name="allowedEndTime">End of allowed time window (time of day).</param>
    public TimeWindowPolicy(TimeSpan allowedStartTime, TimeSpan allowedEndTime)
    {
        _allowedStartTime = allowedStartTime;
        _allowedEndTime = allowedEndTime;
    }

    /// <inheritdoc/>
    public string Name => "TimeWindow";

    /// <inheritdoc/>
    public string Description => $"Restricts operations to {_allowedStartTime:hh\\:mm} - {_allowedEndTime:hh\\:mm}";

    /// <inheritdoc/>
    public Task<PolicyEvaluationResult> EvaluateAsync(PolicyContext context, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow.TimeOfDay;

        bool isInWindow;
        if (_allowedStartTime <= _allowedEndTime)
        {
            // Normal case: e.g., 09:00-17:00
            isInWindow = now >= _allowedStartTime && now <= _allowedEndTime;
        }
        else
        {
            // Wrapping case: e.g., 22:00-06:00 (overnight)
            isInWindow = now >= _allowedStartTime || now <= _allowedEndTime;
        }

        if (isInWindow)
        {
            return Task.FromResult(new PolicyEvaluationResult
            {
                IsCompliant = true,
                PolicyName = Name,
                Reason = "Current time is within allowed window"
            });
        }

        return Task.FromResult(new PolicyEvaluationResult
        {
            IsCompliant = false,
            PolicyName = Name,
            Reason = $"Current time {now:hh\\:mm} is outside allowed window",
            Severity = PolicyViolationSeverity.Error
        });
    }
}

/// <summary>
/// Policy that prevents conflicting experiments from running simultaneously.
/// </summary>
public class ConflictPreventionPolicy : IExperimentPolicy
{
    private readonly HashSet<string> _conflictingExperiments;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictPreventionPolicy"/> class.
    /// </summary>
    /// <param name="conflictingExperiments">Names of experiments that conflict.</param>
    public ConflictPreventionPolicy(params string[] conflictingExperiments)
    {
        _conflictingExperiments = new HashSet<string>(conflictingExperiments, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public string Name => "ConflictPrevention";

    /// <inheritdoc/>
    public string Description => $"Prevents conflicting experiments: {string.Join(", ", _conflictingExperiments)}";

    /// <inheritdoc/>
    public Task<PolicyEvaluationResult> EvaluateAsync(PolicyContext context, CancellationToken cancellationToken = default)
    {
        if (context.Metadata == null ||
            !context.Metadata.TryGetValue("runningExperiments", out var runningObj) ||
            runningObj is not IEnumerable<string> runningExperiments)
        {
            return Task.FromResult(new PolicyEvaluationResult
            {
                IsCompliant = true,
                PolicyName = Name,
                Reason = "No running experiments data available"
            });
        }

        var conflicts = runningExperiments.Where(e => _conflictingExperiments.Contains(e)).ToList();
        if (conflicts.Count == 0)
        {
            return Task.FromResult(new PolicyEvaluationResult
            {
                IsCompliant = true,
                PolicyName = Name,
                Reason = "No conflicting experiments detected"
            });
        }

        return Task.FromResult(new PolicyEvaluationResult
        {
            IsCompliant = false,
            PolicyName = Name,
            Reason = $"Conflicting experiments detected: {string.Join(", ", conflicts)}",
            Severity = PolicyViolationSeverity.Critical
        });
    }
}
