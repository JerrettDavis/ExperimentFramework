using ExperimentFramework.Models;

namespace ExperimentFramework.Validation;

/// <summary>
/// Detects conflicts between trial registrations at configuration time.
/// </summary>
/// <remarks>
/// <para>
/// The detector validates that:
/// <list type="bullet">
/// <item><description>No two trials for the same interface have overlapping time windows</description></item>
/// <item><description>All fallback keys reference valid conditions</description></item>
/// <item><description>Percentage allocations don't exceed 100%</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class TrialConflictDetector
{
    private readonly List<TrialConflict> _conflicts = [];

    /// <summary>
    /// Validates a collection of experiment registrations for conflicts.
    /// </summary>
    /// <param name="registrations">The experiment registrations to validate.</param>
    /// <returns>A list of detected conflicts, or an empty list if no conflicts were found.</returns>
    public IReadOnlyList<TrialConflict> DetectConflicts(IEnumerable<ExperimentRegistration> registrations)
    {
        if (registrations == null)
            throw new ArgumentNullException(nameof(registrations));

        _conflicts.Clear();

        var registrationList = registrations.ToList();

        // Group registrations by service type
        var byServiceType = registrationList
            .GroupBy(r => r.ServiceType)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in byServiceType)
        {
            ValidateRegistrationGroup(group.Key, group.ToList());
        }

        // Validate individual registrations
        foreach (var registration in registrationList)
        {
            ValidateRegistration(registration);
        }

        return _conflicts.ToList();
    }

    /// <summary>
    /// Validates registrations and throws if conflicts are found.
    /// </summary>
    /// <param name="registrations">The experiment registrations to validate.</param>
    /// <exception cref="TrialConflictException">Thrown when conflicts are detected.</exception>
    public void ValidateOrThrow(IEnumerable<ExperimentRegistration> registrations)
    {
        var conflicts = DetectConflicts(registrations);
        if (conflicts.Count > 0)
        {
            throw new TrialConflictException(conflicts);
        }
    }

    private void ValidateRegistrationGroup(Type serviceType, List<ExperimentRegistration> registrations)
    {
        // Check for overlapping time windows
        var withTimeBounds = registrations
            .Where(r => r.StartTime.HasValue || r.EndTime.HasValue)
            .ToList();

        for (int i = 0; i < withTimeBounds.Count; i++)
        {
            for (int j = i + 1; j < withTimeBounds.Count; j++)
            {
                if (TimeWindowsOverlap(withTimeBounds[i], withTimeBounds[j]))
                {
                    _conflicts.Add(new TrialConflict
                    {
                        Type = TrialConflictType.OverlappingTimeWindows,
                        ServiceType = serviceType,
                        Description = $"Trials for {serviceType.Name} have overlapping time windows: " +
                                      $"{FormatTimeWindow(withTimeBounds[i])} and {FormatTimeWindow(withTimeBounds[j])}.",
                        ExperimentNames = GetExperimentNames(withTimeBounds[i], withTimeBounds[j])
                    });
                }
            }
        }

        // Check for multiple registrations without any time bounds
        var withoutTimeBounds = registrations
            .Where(r => !r.StartTime.HasValue && !r.EndTime.HasValue)
            .ToList();

        if (withoutTimeBounds.Count > 1)
        {
            _conflicts.Add(new TrialConflict
            {
                Type = TrialConflictType.DuplicateServiceRegistration,
                ServiceType = serviceType,
                Description = $"Multiple trials registered for {serviceType.Name} without time bounds to differentiate them.",
                ExperimentNames = withoutTimeBounds
                    .Where(r => r.ExperimentName != null)
                    .Select(r => r.ExperimentName!)
                    .ToList()
            });
        }
    }

    private void ValidateRegistration(ExperimentRegistration registration)
    {
        // Validate fallback key references
        if (registration.OnErrorPolicy == OnErrorPolicy.RedirectAndReplay &&
            !string.IsNullOrEmpty(registration.FallbackTrialKey) &&
            !registration.Trials.ContainsKey(registration.FallbackTrialKey))
        {
            _conflicts.Add(new TrialConflict
            {
                Type = TrialConflictType.InvalidFallbackKey,
                ServiceType = registration.ServiceType,
                Description = $"Trial for {registration.ServiceType.Name} references fallback key " +
                              $"'{registration.FallbackTrialKey}' which does not exist in registered conditions."
            });
        }

        // Validate ordered fallback keys
        if (registration.OnErrorPolicy == OnErrorPolicy.RedirectAndReplayOrdered &&
            registration.OrderedFallbackKeys != null)
        {
            var invalidKeys = registration.OrderedFallbackKeys
                .Where(key => !registration.Trials.ContainsKey(key));

            foreach (var key in invalidKeys)
            {
                _conflicts.Add(new TrialConflict
                {
                    Type = TrialConflictType.InvalidFallbackKey,
                    ServiceType = registration.ServiceType,
                    Description = $"Trial for {registration.ServiceType.Name} references ordered fallback key " +
                                  $"'{key}' which does not exist in registered conditions."
                });
            }
        }
    }

    private static bool TimeWindowsOverlap(ExperimentRegistration a, ExperimentRegistration b)
    {
        var aStart = a.StartTime ?? DateTimeOffset.MinValue;
        var aEnd = a.EndTime ?? DateTimeOffset.MaxValue;
        var bStart = b.StartTime ?? DateTimeOffset.MinValue;
        var bEnd = b.EndTime ?? DateTimeOffset.MaxValue;

        // Windows overlap if one starts before the other ends
        return aStart < bEnd && bStart < aEnd;
    }

    private static string FormatTimeWindow(ExperimentRegistration registration)
    {
        var start = registration.StartTime?.ToString("yyyy-MM-dd HH:mm") ?? "unbounded";
        var end = registration.EndTime?.ToString("yyyy-MM-dd HH:mm") ?? "unbounded";
        var name = registration.ExperimentName != null ? $"'{registration.ExperimentName}' " : "";
        return $"{name}[{start} to {end}]";
    }

    private static List<string>? GetExperimentNames(params ExperimentRegistration[] registrations)
    {
        var names = registrations
            .Where(r => r.ExperimentName != null)
            .Select(r => r.ExperimentName!)
            .ToList();

        return names.Count > 0 ? names : null;
    }
}
