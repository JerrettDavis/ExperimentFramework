using System.Text;

namespace ExperimentFramework.Science.Reporting;

/// <summary>
/// Generates experiment reports in Markdown format.
/// </summary>
/// <remarks>
/// Produces publication-ready reports suitable for documentation,
/// wiki pages, or GitHub/GitLab markdown rendering.
/// </remarks>
public sealed class MarkdownReporter : IExperimentReporter
{
    private readonly ReporterOptions _options;

    /// <summary>
    /// Creates a new Markdown reporter with default options.
    /// </summary>
    public MarkdownReporter() : this(new ReporterOptions())
    {
    }

    /// <summary>
    /// Creates a new Markdown reporter with specified options.
    /// </summary>
    public MarkdownReporter(ReporterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Task<string> GenerateAsync(ExperimentReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var sb = new StringBuilder();

        // Title
        sb.AppendLine($"# Experiment Report: {report.ExperimentName}");
        sb.AppendLine();

        // Summary section
        WriteSummarySection(sb, report);

        // Hypothesis section
        if (report.Hypothesis != null)
        {
            WriteHypothesisSection(sb, report);
        }

        // Sample sizes
        WriteSampleSizeSection(sb, report);

        // Condition summaries
        if (report.ConditionSummaries?.Count > 0)
        {
            WriteConditionSummariesSection(sb, report);
        }

        // Primary result
        if (report.PrimaryResult != null)
        {
            WritePrimaryResultSection(sb, report);
        }

        // Secondary results
        if (report.SecondaryResults?.Count > 0)
        {
            WriteSecondaryResultsSection(sb, report);
        }

        // Effect size
        if (_options.IncludeEffectSize && report.EffectSize != null)
        {
            WriteEffectSizeSection(sb, report);
        }

        // Power analysis
        if (_options.IncludePowerAnalysis && report.PowerAnalysis != null)
        {
            WritePowerAnalysisSection(sb, report);
        }

        // Warnings
        if (_options.IncludeWarnings && report.Warnings?.Count > 0)
        {
            WriteWarningsSection(sb, report);
        }

        // Recommendations
        if (_options.IncludeRecommendations && report.Recommendations?.Count > 0)
        {
            WriteRecommendationsSection(sb, report);
        }

        // Footer
        sb.AppendLine("---");
        sb.AppendLine($"*Report generated at {report.AnalyzedAt.ToString(_options.DateTimeFormat)}*");

        return Task.FromResult(sb.ToString());
    }

    private void WriteSummarySection(StringBuilder sb, ExperimentReport report)
    {
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"| Property | Value |");
        sb.AppendLine("|----------|-------|");
        sb.AppendLine($"| **Status** | {FormatStatus(report.Status)} |");
        sb.AppendLine($"| **Conclusion** | {FormatConclusion(report.Conclusion)} |");

        if (report.StartedAt.HasValue)
        {
            sb.AppendLine($"| **Started** | {report.StartedAt.Value.ToString(_options.DateTimeFormat)} |");
        }

        if (report.Duration.HasValue)
        {
            sb.AppendLine($"| **Duration** | {FormatDuration(report.Duration.Value)} |");
        }

        var totalSamples = report.SampleSizes.Values.Sum();
        sb.AppendLine($"| **Total Samples** | {totalSamples:N0} |");
        sb.AppendLine();
    }

    private void WriteHypothesisSection(StringBuilder sb, ExperimentReport report)
    {
        var h = report.Hypothesis!;
        sb.AppendLine("## Hypothesis");
        sb.AppendLine();
        sb.AppendLine($"**Type:** {h.Type}");
        sb.AppendLine();
        sb.AppendLine($"- **H0 (Null):** {h.NullHypothesis}");
        sb.AppendLine($"- **H1 (Alternative):** {h.AlternativeHypothesis}");
        sb.AppendLine();
        sb.AppendLine($"**Primary Endpoint:** {h.PrimaryEndpoint.Name}");
        if (!string.IsNullOrEmpty(h.PrimaryEndpoint.Description))
        {
            sb.AppendLine($"  - {h.PrimaryEndpoint.Description}");
        }
        sb.AppendLine();
    }

    private void WriteSampleSizeSection(StringBuilder sb, ExperimentReport report)
    {
        sb.AppendLine("## Sample Sizes");
        sb.AppendLine();
        sb.AppendLine("| Condition | n |");
        sb.AppendLine("|-----------|---|");
        foreach (var (condition, size) in report.SampleSizes.OrderBy(x => x.Key))
        {
            sb.AppendLine($"| {condition} | {size:N0} |");
        }
        sb.AppendLine();
    }

    private void WriteConditionSummariesSection(StringBuilder sb, ExperimentReport report)
    {
        sb.AppendLine("## Descriptive Statistics");
        sb.AppendLine();

        var first = report.ConditionSummaries!.Values.First();
        var isBinary = first.SuccessRate.HasValue;

        if (isBinary)
        {
            sb.AppendLine("| Condition | n | Successes | Success Rate |");
            sb.AppendLine("|-----------|---|-----------|--------------|");
            foreach (var summary in report.ConditionSummaries.Values.OrderBy(x => x.Condition))
            {
                sb.AppendLine($"| {summary.Condition} | {summary.SampleSize:N0} | {summary.SuccessCount:N0} | {summary.SuccessRate:P2} |");
            }
        }
        else
        {
            sb.AppendLine("| Condition | n | Mean | Std Dev | Median | Min | Max |");
            sb.AppendLine("|-----------|---|------|---------|--------|-----|-----|");
            foreach (var summary in report.ConditionSummaries.Values.OrderBy(x => x.Condition))
            {
                sb.AppendLine($"| {summary.Condition} | {summary.SampleSize:N0} | {FormatNumber(summary.Mean)} | {FormatNumber(summary.StandardDeviation)} | {FormatNumber(summary.Median)} | {FormatNumber(summary.Minimum)} | {FormatNumber(summary.Maximum)} |");
            }
        }
        sb.AppendLine();
    }

    private void WritePrimaryResultSection(StringBuilder sb, ExperimentReport report)
    {
        var result = report.PrimaryResult!;
        sb.AppendLine("## Primary Analysis");
        sb.AppendLine();
        sb.AppendLine($"**Test:** {result.TestName}");
        sb.AppendLine();
        sb.AppendLine("| Statistic | Value |");
        sb.AppendLine("|-----------|-------|");
        sb.AppendLine($"| Test Statistic | {result.TestStatistic.ToString($"F{_options.DecimalPlaces}")} |");
        sb.AppendLine($"| p-value | {FormatPValue(result.PValue)} |");
        sb.AppendLine($"| Significance Level (α) | {result.Alpha} |");
        sb.AppendLine($"| Significant | {(result.IsSignificant ? "**Yes**" : "No")} |");

        if (result.DegreesOfFreedom.HasValue)
        {
            sb.AppendLine($"| Degrees of Freedom | {result.DegreesOfFreedom.Value.ToString($"F{_options.DecimalPlaces}")} |");
        }

        sb.AppendLine($"| Point Estimate | {result.PointEstimate.ToString($"F{_options.DecimalPlaces}")} |");
        sb.AppendLine($"| {(1 - result.Alpha) * 100}% CI | [{FormatNumber(result.ConfidenceIntervalLower)}, {FormatNumber(result.ConfidenceIntervalUpper)}] |");
        sb.AppendLine();
    }

    private void WriteSecondaryResultsSection(StringBuilder sb, ExperimentReport report)
    {
        sb.AppendLine("## Secondary Analyses");
        sb.AppendLine();

        foreach (var (endpoint, result) in report.SecondaryResults!)
        {
            sb.AppendLine($"### {endpoint}");
            sb.AppendLine();
            sb.AppendLine($"- **Test:** {result.TestName}");
            sb.AppendLine($"- **p-value:** {FormatPValue(result.PValue)} ({(result.IsSignificant ? "significant" : "not significant")})");
            sb.AppendLine($"- **Effect:** {result.PointEstimate.ToString($"F{_options.DecimalPlaces}")} [{FormatNumber(result.ConfidenceIntervalLower)}, {FormatNumber(result.ConfidenceIntervalUpper)}]");
            sb.AppendLine();
        }
    }

    private void WriteEffectSizeSection(StringBuilder sb, ExperimentReport report)
    {
        var effect = report.EffectSize!;
        sb.AppendLine("## Effect Size");
        sb.AppendLine();
        sb.AppendLine($"- **Measure:** {effect.MeasureName}");
        sb.AppendLine($"- **Value:** {effect.Value.ToString($"F{_options.DecimalPlaces}")}");
        sb.AppendLine($"- **Magnitude:** {effect.Magnitude}");

        if (effect is { ConfidenceIntervalLower: not null, ConfidenceIntervalUpper: not null })
        {
            sb.AppendLine($"- **95% CI:** [{FormatNumber(effect.ConfidenceIntervalLower)}, {FormatNumber(effect.ConfidenceIntervalUpper)}]");
        }
        sb.AppendLine();
    }

    private void WritePowerAnalysisSection(StringBuilder sb, ExperimentReport report)
    {
        var power = report.PowerAnalysis!;
        sb.AppendLine("## Power Analysis");
        sb.AppendLine();
        sb.AppendLine($"- **Achieved Power:** {power.AchievedPower:P1}");
        sb.AppendLine($"- **Target Power:** {power.TargetPower:P1}");
        sb.AppendLine($"- **Current Sample Size:** {power.CurrentSampleSize:N0}");

        if (power.RequiredSampleSize.HasValue)
        {
            sb.AppendLine($"- **Required Sample Size:** {power.RequiredSampleSize.Value:N0}");
        }

        sb.AppendLine($"- **Adequately Powered:** {(power.IsAdequatelyPowered ? "Yes" : "No")}");

        if (power.MinimumDetectableEffect.HasValue)
        {
            sb.AppendLine($"- **Minimum Detectable Effect:** {power.MinimumDetectableEffect.Value.ToString($"F{_options.DecimalPlaces}")}");
        }
        sb.AppendLine();
    }

    private static void WriteWarningsSection(StringBuilder sb, ExperimentReport report)
    {
        sb.AppendLine("## Warnings");
        sb.AppendLine();
        foreach (var warning in report.Warnings!)
        {
            sb.AppendLine($"- ⚠️ {warning}");
        }
        sb.AppendLine();
    }

    private static void WriteRecommendationsSection(StringBuilder sb, ExperimentReport report)
    {
        sb.AppendLine("## Recommendations");
        sb.AppendLine();
        foreach (var rec in report.Recommendations!)
        {
            sb.AppendLine($"- {rec}");
        }
        sb.AppendLine();
    }

    private static string FormatStatus(ExperimentStatus status) => status switch
    {
        ExperimentStatus.Running => "🔄 Running",
        ExperimentStatus.Completed => "✅ Completed",
        ExperimentStatus.Stopped => "⏹️ Stopped",
        ExperimentStatus.Failed => "❌ Failed",
        _ => status.ToString()
    };

    private static string FormatConclusion(ExperimentConclusion conclusion) => conclusion switch
    {
        ExperimentConclusion.Inconclusive => "⏳ Inconclusive",
        ExperimentConclusion.TreatmentWins => "🏆 Treatment wins",
        ExperimentConclusion.ControlWins => "🛡️ Control wins",
        ExperimentConclusion.NoSignificantDifference => "➖ No significant difference",
        ExperimentConclusion.TreatmentNonInferior => "✓ Treatment non-inferior",
        ExperimentConclusion.TreatmentEquivalent => "⚖️ Treatment equivalent",
        _ => conclusion.ToString()
    };

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{duration.TotalDays:F1} days";
        if (duration.TotalHours >= 1)
            return $"{duration.TotalHours:F1} hours";
        if (duration.TotalMinutes >= 1)
            return $"{duration.TotalMinutes:F1} minutes";
        return $"{duration.TotalSeconds:F1} seconds";
    }

    private static string FormatPValue(double p) => p switch
    {
        < 0.001 => "< 0.001",
        < 0.01 => $"{p:F3}",
        _ => $"{p:F4}"
    };

    private string FormatNumber(double? value) =>
        value.HasValue
            ? double.IsInfinity(value.Value)
                ? (double.IsPositiveInfinity(value.Value) ? "+∞" : "-∞")
                : value.Value.ToString($"F{_options.DecimalPlaces}")
            : "-";
}
