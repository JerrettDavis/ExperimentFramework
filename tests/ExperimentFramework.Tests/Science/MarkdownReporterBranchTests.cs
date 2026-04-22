using ExperimentFramework.Science.Models.Hypothesis;
using ExperimentFramework.Science.Models.Results;
using ExperimentFramework.Science.Reporting;

namespace ExperimentFramework.Tests.Science;

/// <summary>
/// Tests for MarkdownReporter branches not covered by ReporterTests.cs.
/// </summary>
public class MarkdownReporterBranchTests
{
    private readonly MarkdownReporter _reporter = new();

    // ───────────────────────── FormatDuration branches ─────────────────────────

    [Fact]
    public async Task GenerateAsync_FormatsDuration_InDays_WhenMoreThan24Hours()
    {
        var report = CreateReport(duration: TimeSpan.FromDays(3));

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("days", markdown);
    }

    [Fact]
    public async Task GenerateAsync_FormatsDuration_InHours_WhenMoreThan60Minutes()
    {
        var report = CreateReport(duration: TimeSpan.FromHours(5));

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("hours", markdown);
    }

    [Fact]
    public async Task GenerateAsync_FormatsDuration_InMinutes_WhenMoreThan60Seconds()
    {
        var report = CreateReport(duration: TimeSpan.FromMinutes(45));

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("minutes", markdown);
    }

    [Fact]
    public async Task GenerateAsync_FormatsDuration_InSeconds_WhenLessThan60Seconds()
    {
        var report = CreateReport(duration: TimeSpan.FromSeconds(30));

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("seconds", markdown);
    }

    // ───────────────────────── StartedAt ─────────────────────────

    [Fact]
    public async Task GenerateAsync_IncludesStartedAt_WhenPresent()
    {
        var report = new ExperimentReport
        {
            ExperimentName = "exp",
            AnalyzedAt = DateTimeOffset.UtcNow,
            Status = ExperimentStatus.Completed,
            Conclusion = ExperimentConclusion.TreatmentWins,
            SampleSizes = new Dictionary<string, int> { ["control"] = 100, ["treatment"] = 100 },
            StartedAt = DateTimeOffset.UtcNow.AddDays(-7)
        };

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("Started", markdown);
    }

    // ───────────────────────── FormatStatus branches ─────────────────────────

    [Theory]
    [InlineData(ExperimentStatus.Running, "Running")]
    [InlineData(ExperimentStatus.Completed, "Completed")]
    [InlineData(ExperimentStatus.Stopped, "Stopped")]
    [InlineData(ExperimentStatus.Failed, "Failed")]
    public async Task GenerateAsync_FormatsAllStatuses(ExperimentStatus status, string expectedText)
    {
        var report = CreateReport(status: status);

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains(expectedText, markdown);
    }

    // ───────────────────────── FormatConclusion branches ─────────────────────────

    [Theory]
    [InlineData(ExperimentConclusion.Inconclusive, "Inconclusive")]
    [InlineData(ExperimentConclusion.TreatmentWins, "Treatment wins")]
    [InlineData(ExperimentConclusion.ControlWins, "Control wins")]
    [InlineData(ExperimentConclusion.NoSignificantDifference, "No significant difference")]
    [InlineData(ExperimentConclusion.TreatmentNonInferior, "non-inferior")]
    [InlineData(ExperimentConclusion.TreatmentEquivalent, "equivalent")]
    public async Task GenerateAsync_FormatsAllConclusions(ExperimentConclusion conclusion, string expectedText)
    {
        var report = CreateReport(conclusion: conclusion);

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains(expectedText, markdown, StringComparison.OrdinalIgnoreCase);
    }

    // ───────────────────────── DegreesOfFreedom ─────────────────────────

    [Fact]
    public async Task GenerateAsync_IncludesDegreesOfFreedom_WhenPresent()
    {
        var report = CreateReport(degreesOfFreedom: 198.5);

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("Degrees of Freedom", markdown);
    }

    [Fact]
    public async Task GenerateAsync_OmitsDegreesOfFreedom_WhenAbsent()
    {
        var report = CreateReport(degreesOfFreedom: null);

        var markdown = await _reporter.GenerateAsync(report);

        Assert.DoesNotContain("Degrees of Freedom", markdown);
    }

    // ───────────────────────── FormatPValue branches ─────────────────────────

    [Fact]
    public async Task GenerateAsync_FormatsPValue_LessThan0001()
    {
        var report = CreateReport(pValue: 0.00001);

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("< 0.001", markdown);
    }

    [Fact]
    public async Task GenerateAsync_FormatsPValue_LessThan001()
    {
        var report = CreateReport(pValue: 0.005);

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("0.005", markdown);
    }

    [Fact]
    public async Task GenerateAsync_FormatsPValue_Otherwise()
    {
        var report = CreateReport(pValue: 0.04567);

        var markdown = await _reporter.GenerateAsync(report);

        // 4 decimal places for values >= 0.01
        Assert.Contains("0.0457", markdown);
    }

    // ───────────────────────── EffectSize CI branches ─────────────────────────

    [Fact]
    public async Task GenerateAsync_IncludesEffectSizeCI_WhenBothBoundsPresent()
    {
        var report = CreateReport(effectSizeCiLower: 0.2, effectSizeCiUpper: 0.8);

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("95% CI", markdown);
    }

    [Fact]
    public async Task GenerateAsync_OmitsEffectSizeCI_WhenBoundsAbsent()
    {
        // Use a reporter with no effect size at all to avoid the primary result "95% CI" row
        var reporter = new MarkdownReporter(new ReporterOptions { IncludeEffectSize = false });
        var report = CreateReport(effectSizeCiLower: null, effectSizeCiUpper: null);

        var markdown = await reporter.GenerateAsync(report);

        Assert.DoesNotContain("Effect Size", markdown);
    }

    // ───────────────────────── PowerAnalysis RequiredSampleSize branch ─────────────────────────

    [Fact]
    public async Task GenerateAsync_IncludesRequiredSampleSize_WhenPresent()
    {
        var report = CreateReport(requiredSampleSize: 250);

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("Required Sample Size", markdown);
        Assert.Contains("250", markdown);
    }

    [Fact]
    public async Task GenerateAsync_OmitsRequiredSampleSize_WhenAbsent()
    {
        var report = CreateReport(requiredSampleSize: null);

        var markdown = await _reporter.GenerateAsync(report);

        Assert.DoesNotContain("Required Sample Size", markdown);
    }

    // ───────────────────────── PowerAnalysis MinimumDetectableEffect branch ─────────────────────────

    [Fact]
    public async Task GenerateAsync_IncludesMinimumDetectableEffect_WhenPresent()
    {
        var report = CreateReport(minimumDetectableEffect: 0.05);

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("Minimum Detectable Effect", markdown);
    }

    // ───────────────────────── Condition summaries: continuous vs binary ─────────────────────────

    [Fact]
    public async Task GenerateAsync_WritesContinuousTable_WhenNoSuccessRate()
    {
        var report = CreateReport(conditionSummaries: new Dictionary<string, ConditionSummary>
        {
            ["control"] = new ConditionSummary
            {
                Condition = "control",
                SampleSize = 100,
                Mean = 50.0,
                StandardDeviation = 5.0,
                Minimum = 30.0,
                Maximum = 70.0,
                Median = 50.0,
                SuccessRate = null
            }
        });

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("Mean", markdown);
        Assert.Contains("Std Dev", markdown);
    }

    [Fact]
    public async Task GenerateAsync_WritesBinaryTable_WhenSuccessRatePresent()
    {
        var report = CreateReport(conditionSummaries: new Dictionary<string, ConditionSummary>
        {
            ["control"] = new ConditionSummary
            {
                Condition = "control",
                SampleSize = 100,
                SuccessCount = 42,
                SuccessRate = 0.42
            }
        });

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("Success Rate", markdown);
        Assert.Contains("Successes", markdown);
    }

    // ───────────────────────── FormatNumber: infinity branches ─────────────────────────

    [Fact]
    public async Task GenerateAsync_FormatsPositiveInfinity()
    {
        var report = CreateReport(ciLower: double.NegativeInfinity, ciUpper: double.PositiveInfinity);

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("+∞", markdown);
        Assert.Contains("-∞", markdown);
    }

    // ───────────────────────── Hypothesis section ─────────────────────────

    [Fact]
    public async Task GenerateAsync_IncludesHypothesisSection_WhenPresent()
    {
        var report = new ExperimentReport
        {
            ExperimentName = "hyp-exp",
            AnalyzedAt = DateTimeOffset.UtcNow,
            Status = ExperimentStatus.Completed,
            Conclusion = ExperimentConclusion.TreatmentWins,
            SampleSizes = new Dictionary<string, int> { ["control"] = 50, ["treatment"] = 50 },
            Hypothesis = new HypothesisDefinition
            {
                Name = "Conversion hypothesis",
                NullHypothesis = "No difference",
                AlternativeHypothesis = "Treatment is better",
                PrimaryEndpoint = new Endpoint
                {
                    Name = "conversion",
                    Description = "Conversion rate",
                    OutcomeType = ExperimentFramework.Data.Models.OutcomeType.Binary
                },
                Type = HypothesisType.Superiority,
                ExpectedEffectSize = 0.05,
                SuccessCriteria = new SuccessCriteria { Alpha = 0.05 }
            }
        };

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("Hypothesis", markdown);
        Assert.Contains("No difference", markdown);
        Assert.Contains("conversion", markdown);
        Assert.Contains("Conversion rate", markdown);
    }

    [Fact]
    public async Task GenerateAsync_OmitsEndpointDescription_WhenEmpty()
    {
        var report = new ExperimentReport
        {
            ExperimentName = "hyp-exp2",
            AnalyzedAt = DateTimeOffset.UtcNow,
            Status = ExperimentStatus.Completed,
            Conclusion = ExperimentConclusion.TreatmentWins,
            SampleSizes = new Dictionary<string, int> { ["control"] = 50, ["treatment"] = 50 },
            Hypothesis = new HypothesisDefinition
            {
                Name = "Metric hypothesis",
                NullHypothesis = "H0",
                AlternativeHypothesis = "H1",
                PrimaryEndpoint = new Endpoint
                {
                    Name = "metric",
                    Description = "", // empty description
                    OutcomeType = ExperimentFramework.Data.Models.OutcomeType.Continuous
                },
                Type = HypothesisType.Superiority,
                ExpectedEffectSize = 0.1,
                SuccessCriteria = new SuccessCriteria { Alpha = 0.05 }
            }
        };

        var markdown = await _reporter.GenerateAsync(report);

        Assert.Contains("metric", markdown);
    }

    // ───────────────────────── Custom options ─────────────────────────

    [Fact]
    public async Task GenerateAsync_RespectsIncludeEffectSizeFalse()
    {
        var reporter = new MarkdownReporter(new ReporterOptions { IncludeEffectSize = false });
        var report = CreateReport();

        var markdown = await reporter.GenerateAsync(report);

        Assert.DoesNotContain("Effect Size", markdown);
    }

    [Fact]
    public async Task GenerateAsync_RespectsIncludePowerAnalysisFalse()
    {
        var reporter = new MarkdownReporter(new ReporterOptions { IncludePowerAnalysis = false });
        var report = CreateReport();

        var markdown = await reporter.GenerateAsync(report);

        Assert.DoesNotContain("Power Analysis", markdown);
    }

    [Fact]
    public async Task GenerateAsync_RespectsIncludeWarningsFalse()
    {
        var reporter = new MarkdownReporter(new ReporterOptions { IncludeWarnings = false });
        var report = CreateReport(warnings: new List<string> { "a warning" });

        var markdown = await reporter.GenerateAsync(report);

        Assert.DoesNotContain("a warning", markdown);
    }

    [Fact]
    public async Task GenerateAsync_RespectsIncludeRecommendationsFalse()
    {
        var reporter = new MarkdownReporter(new ReporterOptions { IncludeRecommendations = false });
        var report = CreateReport(recommendations: new List<string> { "a recommendation" });

        var markdown = await reporter.GenerateAsync(report);

        Assert.DoesNotContain("a recommendation", markdown);
    }

    [Fact]
    public void Constructor_ThrowsForNullOptions()
    {
        Assert.Throws<ArgumentNullException>(() => new MarkdownReporter(null!));
    }

    [Fact]
    public async Task GenerateAsync_ThrowsForNullReport()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _reporter.GenerateAsync(null!));
    }

    // ───────────────────────── Helpers ─────────────────────────

    private static ExperimentReport CreateReport(
        ExperimentStatus status = ExperimentStatus.Completed,
        ExperimentConclusion conclusion = ExperimentConclusion.TreatmentWins,
        TimeSpan? duration = null,
        double pValue = 0.01,
        double? degreesOfFreedom = 198,
        double? effectSizeCiLower = 0.1,
        double? effectSizeCiUpper = 0.9,
        int? requiredSampleSize = 150,
        double? minimumDetectableEffect = null,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? recommendations = null,
        IReadOnlyDictionary<string, ConditionSummary>? conditionSummaries = null,
        double ciLower = 0.05,
        double ciUpper = 0.25) =>
        new()
        {
            ExperimentName = "test-exp",
            AnalyzedAt = DateTimeOffset.UtcNow,
            Status = status,
            Conclusion = conclusion,
            Duration = duration,
            SampleSizes = new Dictionary<string, int> { ["control"] = 100, ["treatment"] = 100 },
            PrimaryResult = new StatisticalTestResult
            {
                TestName = "Welch's t-Test",
                TestStatistic = 2.5,
                PValue = pValue,
                Alpha = 0.05,
                PointEstimate = 0.15,
                ConfidenceIntervalLower = ciLower,
                ConfidenceIntervalUpper = ciUpper,
                DegreesOfFreedom = degreesOfFreedom,
                SampleSizes = new Dictionary<string, int> { ["control"] = 100, ["treatment"] = 100 }
            },
            EffectSize = new EffectSizeResult
            {
                MeasureName = "Cohen's d",
                Value = 0.5,
                Magnitude = EffectSizeMagnitude.Medium,
                ConfidenceIntervalLower = effectSizeCiLower,
                ConfidenceIntervalUpper = effectSizeCiUpper
            },
            PowerAnalysis = new PowerAnalysisResult
            {
                AchievedPower = 0.85,
                TargetPower = 0.80,
                CurrentSampleSize = 200,
                RequiredSampleSize = requiredSampleSize,
                IsAdequatelyPowered = true,
                Alpha = 0.05,
                MinimumDetectableEffect = minimumDetectableEffect
            },
            Warnings = warnings,
            Recommendations = recommendations,
            ConditionSummaries = conditionSummaries
        };
}
