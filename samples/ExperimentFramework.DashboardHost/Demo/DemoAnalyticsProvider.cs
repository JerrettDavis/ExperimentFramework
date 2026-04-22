using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.DashboardHost.Demo;

/// <summary>
/// In-memory IAnalyticsProvider with curated demo data.
/// checkout-button-v2 shows a stat-sig winner on variant-a (4.8% vs 3.1% conversion).
/// search-ranker-ml is inconclusive across three arms (18.2% / 18.5% / 18.4%).
/// </summary>
/// <remarks>
/// Delta from plan: IAnalyticsProvider returns IEnumerable&lt;T&gt; (not IReadOnlyList&lt;T&gt;) and
/// accepts tenantId/start/end filters. AssignmentEvent/ExposureEvent use TrialKey (the arm
/// identifier) instead of ArmName, and Timestamp instead of AssignedAt. AnalysisSignalEvent
/// uses MetricName + Value to encode conversion rates; one signal per arm per metric.
/// All three event types are defined in IAnalyticsProvider.cs (no separate event files exist).
/// </remarks>
public sealed class DemoAnalyticsProvider : IAnalyticsProvider
{
    private readonly IReadOnlyList<AssignmentEvent>    _assignments;
    private readonly IReadOnlyList<ExposureEvent>       _exposures;
    private readonly IReadOnlyList<AnalysisSignalEvent> _signals;

    public DemoAnalyticsProvider(DateTimeOffset frozenNow)
    {
        _assignments = BuildAssignments(frozenNow);
        _exposures   = BuildExposures(frozenNow);
        _signals     = BuildSignals(frozenNow);
    }

    // ---- IAnalyticsProvider implementation ----

    public Task<IEnumerable<AssignmentEvent>> GetAssignmentsAsync(
        string experimentName,
        string? tenantId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<AssignmentEvent> result = _assignments
            .Where(a => a.ExperimentName == experimentName);

        if (start.HasValue)
            result = result.Where(a => a.Timestamp >= start.Value);
        if (end.HasValue)
            result = result.Where(a => a.Timestamp <= end.Value);

        return Task.FromResult(result);
    }

    public Task<IEnumerable<ExposureEvent>> GetExposuresAsync(
        string experimentName,
        string? tenantId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<ExposureEvent> result = _exposures
            .Where(e => e.ExperimentName == experimentName);

        if (start.HasValue)
            result = result.Where(e => e.Timestamp >= start.Value);
        if (end.HasValue)
            result = result.Where(e => e.Timestamp <= end.Value);

        return Task.FromResult(result);
    }

    public Task<IEnumerable<AnalysisSignalEvent>> GetAnalysisSignalsAsync(
        string experimentName,
        string? tenantId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<AnalysisSignalEvent> result = _signals
            .Where(s => s.ExperimentName == experimentName);

        if (start.HasValue)
            result = result.Where(s => s.Timestamp >= start.Value);
        if (end.HasValue)
            result = result.Where(s => s.Timestamp <= end.Value);

        return Task.FromResult(result);
    }

    // ---- Data builders ----

    private static IReadOnlyList<AssignmentEvent> BuildAssignments(DateTimeOffset now)
    {
        var rng  = new Random(42);
        var list = new List<AssignmentEvent>();

        // checkout-button-v2: ~5k per arm over 14-day window
        // TrialKey holds the arm identifier (plan called it ArmName; real property is TrialKey).
        var checkoutWindow = (int)TimeSpan.FromDays(14).TotalSeconds;
        foreach (var (arm, count) in new[] { ("control", 5100), ("variant-a", 5200) })
        {
            for (var i = 0; i < count; i++)
            {
                list.Add(new AssignmentEvent
                {
                    ExperimentName = "checkout-button-v2",
                    SubjectId      = $"user-{rng.Next(1, 200_000)}",
                    TrialKey       = arm,
                    Timestamp      = now.AddDays(-14).AddSeconds(rng.Next(0, checkoutWindow)),
                });
            }
        }

        // search-ranker-ml: ~3k per arm over 7-day window
        var searchWindow = (int)TimeSpan.FromDays(7).TotalSeconds;
        foreach (var (arm, count) in new[] { ("baseline", 3100), ("ml-v1", 3050), ("ml-v2", 3020) })
        {
            for (var i = 0; i < count; i++)
            {
                list.Add(new AssignmentEvent
                {
                    ExperimentName = "search-ranker-ml",
                    SubjectId      = $"user-{rng.Next(1, 150_000)}",
                    TrialKey       = arm,
                    Timestamp      = now.AddDays(-7).AddSeconds(rng.Next(0, searchWindow)),
                });
            }
        }

        return list;
    }

    private static IReadOnlyList<ExposureEvent> BuildExposures(DateTimeOffset now)
    {
        // ~70% exposure rate: roughly 70% of assigned users actually saw the variant.
        var rng  = new Random(43); // different seed so exposure timestamps differ from assignment
        var list = new List<ExposureEvent>();

        var checkoutWindow = (int)TimeSpan.FromDays(14).TotalSeconds;
        foreach (var (arm, assignCount) in new[] { ("control", 5100), ("variant-a", 5200) })
        {
            var exposureCount = (int)(assignCount * 0.70);
            for (var i = 0; i < exposureCount; i++)
            {
                list.Add(new ExposureEvent
                {
                    ExperimentName = "checkout-button-v2",
                    SubjectId      = $"user-{rng.Next(1, 200_000)}",
                    TrialKey       = arm,
                    Timestamp      = now.AddDays(-14).AddSeconds(rng.Next(0, checkoutWindow)),
                });
            }
        }

        var searchWindow = (int)TimeSpan.FromDays(7).TotalSeconds;
        foreach (var (arm, assignCount) in new[] { ("baseline", 3100), ("ml-v1", 3050), ("ml-v2", 3020) })
        {
            var exposureCount = (int)(assignCount * 0.70);
            for (var i = 0; i < exposureCount; i++)
            {
                list.Add(new ExposureEvent
                {
                    ExperimentName = "search-ranker-ml",
                    SubjectId      = $"user-{rng.Next(1, 150_000)}",
                    TrialKey       = arm,
                    Timestamp      = now.AddDays(-7).AddSeconds(rng.Next(0, searchWindow)),
                });
            }
        }

        return list;
    }

    private static IReadOnlyList<AnalysisSignalEvent> BuildSignals(DateTimeOffset now)
    {
        // One signal per arm encoding the observed conversion rate as a metric value.
        // MetricName = "conversion_rate"; Value = observed rate.
        // checkout-button-v2: control 3.1%, variant-a 4.8% (stat-sig winner)
        // search-ranker-ml: baseline 18.2%, ml-v1 18.5%, ml-v2 18.4% (inconclusive)
        var signalTime = now.AddDays(-1);

        return new List<AnalysisSignalEvent>
        {
            // checkout-button-v2
            new()
            {
                ExperimentName = "checkout-button-v2",
                SubjectId      = "analysis-engine",
                TrialKey       = "control",
                MetricName     = "conversion_rate",
                Value          = 0.031,
                Timestamp      = signalTime,
            },
            new()
            {
                ExperimentName = "checkout-button-v2",
                SubjectId      = "analysis-engine",
                TrialKey       = "variant-a",
                MetricName     = "conversion_rate",
                Value          = 0.048,
                Timestamp      = signalTime,
            },
            // p-value signal — value < 0.05 indicates stat-sig
            new()
            {
                ExperimentName = "checkout-button-v2",
                SubjectId      = "analysis-engine",
                TrialKey       = "variant-a",
                MetricName     = "p_value",
                Value          = 0.0021,
                Timestamp      = signalTime,
            },

            // search-ranker-ml
            new()
            {
                ExperimentName = "search-ranker-ml",
                SubjectId      = "analysis-engine",
                TrialKey       = "baseline",
                MetricName     = "conversion_rate",
                Value          = 0.182,
                Timestamp      = signalTime,
            },
            new()
            {
                ExperimentName = "search-ranker-ml",
                SubjectId      = "analysis-engine",
                TrialKey       = "ml-v1",
                MetricName     = "conversion_rate",
                Value          = 0.185,
                Timestamp      = signalTime,
            },
            new()
            {
                ExperimentName = "search-ranker-ml",
                SubjectId      = "analysis-engine",
                TrialKey       = "ml-v2",
                MetricName     = "conversion_rate",
                Value          = 0.184,
                Timestamp      = signalTime,
            },
            // p-values > 0.05 = inconclusive
            new()
            {
                ExperimentName = "search-ranker-ml",
                SubjectId      = "analysis-engine",
                TrialKey       = "ml-v1",
                MetricName     = "p_value",
                Value          = 0.412,
                Timestamp      = signalTime,
            },
            new()
            {
                ExperimentName = "search-ranker-ml",
                SubjectId      = "analysis-engine",
                TrialKey       = "ml-v2",
                MetricName     = "p_value",
                Value          = 0.387,
                Timestamp      = signalTime,
            },
        };
    }
}
