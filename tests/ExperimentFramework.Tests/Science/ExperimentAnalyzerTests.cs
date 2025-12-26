using ExperimentFramework.Data.Models;
using ExperimentFramework.Data.Storage;
using ExperimentFramework.Science.Analysis;
using ExperimentFramework.Science.Builders;
using ExperimentFramework.Science.Power;
using ExperimentFramework.Science.Reporting;

namespace ExperimentFramework.Tests.Science;

public class ExperimentAnalyzerTests
{
    private readonly InMemoryOutcomeStore _store = new();

    private async Task SeedContinuousData(
        string experimentName = "test-exp",
        string metricName = "metric",
        double[] controlValues = null!,
        double[] treatmentValues = null!)
    {
        controlValues ??= [10, 12, 11, 13, 14, 10, 11, 12, 13, 12];
        treatmentValues ??= [15, 16, 17, 14, 18, 16, 17, 15, 16, 17];

        for (var i = 0; i < controlValues.Length; i++)
        {
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"control-{i}",
                ExperimentName = experimentName,
                TrialKey = "control",
                SubjectId = $"user-{i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = metricName,
                Value = controlValues[i],
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        for (var i = 0; i < treatmentValues.Length; i++)
        {
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"treatment-{i}",
                ExperimentName = experimentName,
                TrialKey = "treatment",
                SubjectId = $"user-{100 + i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = metricName,
                Value = treatmentValues[i],
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    private async Task SeedBinaryData(
        string experimentName = "test-exp",
        string metricName = "conversion",
        int controlSuccess = 20,
        int controlTotal = 100,
        int treatmentSuccess = 35,
        int treatmentTotal = 100)
    {
        for (var i = 0; i < controlTotal; i++)
        {
            var success = i < controlSuccess;
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"control-{i}",
                ExperimentName = experimentName,
                TrialKey = "control",
                SubjectId = $"user-{i}",
                OutcomeType = OutcomeType.Binary,
                MetricName = metricName,
                Value = success ? 1 : 0,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        for (var i = 0; i < treatmentTotal; i++)
        {
            var success = i < treatmentSuccess;
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"treatment-{i}",
                ExperimentName = experimentName,
                TrialKey = "treatment",
                SubjectId = $"user-{1000 + i}",
                OutcomeType = OutcomeType.Binary,
                MetricName = metricName,
                Value = success ? 1 : 0,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    [Fact]
    public void Constructor_ThrowsOnNullStore()
    {
        Assert.Throws<ArgumentNullException>(() => new ExperimentAnalyzer(null!));
    }

    [Fact]
    public void Constructor_AcceptsValidStore()
    {
        var analyzer = new ExperimentAnalyzer(_store);
        Assert.NotNull(analyzer);
    }

    [Fact]
    public void Constructor_AcceptsOptionalPowerAnalyzer()
    {
        var analyzer = new ExperimentAnalyzer(_store, PowerAnalyzer.Instance);
        Assert.NotNull(analyzer);
    }

    [Fact]
    public async Task AnalyzeAsync_ThrowsOnNullExperimentName()
    {
        var analyzer = new ExperimentAnalyzer(_store);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            analyzer.AnalyzeAsync(null!));
    }

    [Fact]
    public async Task AnalyzeAsync_ThrowsOnEmptyExperimentName()
    {
        var analyzer = new ExperimentAnalyzer(_store);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            analyzer.AnalyzeAsync(""));
    }

    [Fact]
    public async Task AnalyzeAsync_ThrowsOnWhitespaceExperimentName()
    {
        var analyzer = new ExperimentAnalyzer(_store);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            analyzer.AnalyzeAsync("   "));
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsEmptyReportForNoData()
    {
        var analyzer = new ExperimentAnalyzer(_store);

        var report = await analyzer.AnalyzeAsync("nonexistent");

        Assert.NotNull(report);
        Assert.Equal("nonexistent", report.ExperimentName);
        Assert.Equal(ExperimentStatus.Running, report.Status);
        Assert.Equal(ExperimentConclusion.Inconclusive, report.Conclusion);
        Assert.NotNull(report.Warnings);
        Assert.Contains(report.Warnings, w => w.Contains("No data"));
    }

    [Fact]
    public async Task AnalyzeAsync_AnalyzesContinuousData()
    {
        await SeedContinuousData();
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions { MetricName = "metric" };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report);
        Assert.Equal("test-exp", report.ExperimentName);
        Assert.NotNull(report.PrimaryResult);
        Assert.Equal("Welch's Two-Sample t-Test", report.PrimaryResult.TestName);
    }

    [Fact]
    public async Task AnalyzeAsync_AnalyzesBinaryData()
    {
        await SeedBinaryData();
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions { MetricName = "conversion" };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report);
        Assert.NotNull(report.PrimaryResult);
        Assert.Equal("Chi-Square Test for Independence", report.PrimaryResult.TestName);
    }

    [Fact]
    public async Task AnalyzeAsync_CalculatesSampleSizes()
    {
        await SeedContinuousData();
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions { MetricName = "metric" };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report.SampleSizes);
        Assert.Equal(10, report.SampleSizes["control"]);
        Assert.Equal(10, report.SampleSizes["treatment"]);
    }

    [Fact]
    public async Task AnalyzeAsync_BuildsConditionSummaries()
    {
        await SeedContinuousData();
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions { MetricName = "metric" };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report.ConditionSummaries);
        Assert.True(report.ConditionSummaries.ContainsKey("control"));
        Assert.True(report.ConditionSummaries.ContainsKey("treatment"));

        var controlSummary = report.ConditionSummaries["control"];
        Assert.Equal("control", controlSummary.Condition);
        Assert.Equal(10, controlSummary.SampleSize);
        Assert.True(controlSummary.Mean > 0);
    }

    [Fact]
    public async Task AnalyzeAsync_CalculatesEffectSize()
    {
        await SeedContinuousData();
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "metric",
            CalculateEffectSize = true
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report.EffectSize);
        Assert.Equal("Cohen's d", report.EffectSize.MeasureName);
    }

    [Fact]
    public async Task AnalyzeAsync_SkipsEffectSizeWhenDisabled()
    {
        await SeedContinuousData();
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "metric",
            CalculateEffectSize = false
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.Null(report.EffectSize);
    }

    [Fact]
    public async Task AnalyzeAsync_PerformsPowerAnalysis()
    {
        await SeedContinuousData();
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "metric",
            PerformPowerAnalysis = true,
            CalculateEffectSize = true
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report.PowerAnalysis);
    }

    [Fact]
    public async Task AnalyzeAsync_SkipsPowerAnalysisWhenDisabled()
    {
        await SeedContinuousData();
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "metric",
            PerformPowerAnalysis = false
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.Null(report.PowerAnalysis);
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsSignificantDifference()
    {
        // Seed data with clearly different means
        await SeedContinuousData(
            controlValues: [1, 2, 3, 4, 5, 1, 2, 3, 4, 5],
            treatmentValues: [20, 21, 22, 23, 24, 20, 21, 22, 23, 24]);
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions { MetricName = "metric" };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report.PrimaryResult);
        Assert.True(report.PrimaryResult.IsSignificant);
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsNoSignificantDifference()
    {
        // Seed data with similar means
        await SeedContinuousData(
            controlValues: [10, 11, 12, 10, 11, 12, 10, 11, 12, 10],
            treatmentValues: [10, 11, 12, 10, 11, 12, 10, 11, 12, 11]);
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions { MetricName = "metric" };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report.PrimaryResult);
        Assert.False(report.PrimaryResult.IsSignificant);
        Assert.Equal(ExperimentConclusion.NoSignificantDifference, report.Conclusion);
    }

    [Fact]
    public async Task AnalyzeAsync_GeneratesWarningsForSmallSampleSize()
    {
        // Seed minimal data
        for (var i = 0; i < 3; i++)
        {
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"c-{i}",
                ExperimentName = "small-exp",
                TrialKey = "control",
                SubjectId = $"u-{i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "m",
                Value = i,
                Timestamp = DateTimeOffset.UtcNow
            });
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"t-{i}",
                ExperimentName = "small-exp",
                TrialKey = "treatment",
                SubjectId = $"u-{10 + i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "m",
                Value = i + 1,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "m",
            MinimumSampleSize = 10
        };

        var report = await analyzer.AnalyzeAsync("small-exp", options);

        Assert.NotNull(report.Warnings);
        Assert.Contains(report.Warnings, w => w.Contains("samples"));
    }

    [Fact]
    public async Task AnalyzeAsync_DeterminesRunningStatus()
    {
        for (var i = 0; i < 5; i++)
        {
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"c-{i}",
                ExperimentName = "running-exp",
                TrialKey = "control",
                SubjectId = $"u-{i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "m",
                Value = i,
                Timestamp = DateTimeOffset.UtcNow
            });
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"t-{i}",
                ExperimentName = "running-exp",
                TrialKey = "treatment",
                SubjectId = $"u-{10 + i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "m",
                Value = i + 1,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "m",
            MinimumSampleSize = 10
        };

        var report = await analyzer.AnalyzeAsync("running-exp", options);

        Assert.Equal(ExperimentStatus.Running, report.Status);
    }

    [Fact]
    public async Task AnalyzeAsync_DeterminesCompletedStatus()
    {
        await SeedContinuousData();
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "metric",
            MinimumSampleSize = 5
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.Equal(ExperimentStatus.Completed, report.Status);
    }

    [Fact]
    public async Task AnalyzeAsync_WithHypothesis_UsesHypothesisMetric()
    {
        await SeedContinuousData(metricName: "primary_metric");
        var analyzer = new ExperimentAnalyzer(_store);
        var hypothesis = new HypothesisBuilder("test")
            .Superiority()
            .NullHypothesis("No difference")
            .AlternativeHypothesis("Treatment is better")
            .PrimaryEndpoint("primary_metric", OutcomeType.Continuous)
            .Build();

        var report = await analyzer.AnalyzeAsync("test-exp", hypothesis);

        Assert.NotNull(report.Hypothesis);
        Assert.NotNull(report.PrimaryResult);
    }

    [Fact]
    public async Task AnalyzeAsync_WithSuperiorityHypothesis_ReturnsAppropriateConclusion()
    {
        await SeedContinuousData(
            controlValues: [1, 2, 3, 4, 5, 1, 2, 3, 4, 5],
            treatmentValues: [20, 21, 22, 23, 24, 20, 21, 22, 23, 24]);
        var analyzer = new ExperimentAnalyzer(_store);
        var hypothesis = new HypothesisBuilder("test")
            .Superiority()
            .NullHypothesis("No difference")
            .AlternativeHypothesis("Treatment is better")
            .PrimaryEndpoint("metric", OutcomeType.Continuous)
            .Build();

        var report = await analyzer.AnalyzeAsync("test-exp", hypothesis);

        Assert.True(report.PrimaryResult!.IsSignificant);
        Assert.Equal(ExperimentConclusion.TreatmentWins, report.Conclusion);
    }

    [Fact]
    public async Task AnalyzeAsync_GeneratesRecommendations()
    {
        await SeedContinuousData(
            controlValues: [1, 2, 3, 4, 5, 1, 2, 3, 4, 5],
            treatmentValues: [20, 21, 22, 23, 24, 20, 21, 22, 23, 24]);
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "metric",
            GenerateRecommendations = true
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report.Recommendations);
        Assert.True(report.Recommendations.Count > 0);
    }

    [Fact]
    public async Task AnalyzeAsync_SkipsRecommendationsWhenDisabled()
    {
        await SeedContinuousData();
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "metric",
            GenerateRecommendations = false
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.Null(report.Recommendations);
    }

    [Fact]
    public async Task AnalyzeAsync_WithMultipleTreatments_PerformsSecondaryAnalyses()
    {
        await SeedContinuousData();

        // Add a third condition
        for (var i = 0; i < 10; i++)
        {
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"variant-b-{i}",
                ExperimentName = "test-exp",
                TrialKey = "variant-b",
                SubjectId = $"user-{200 + i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "metric",
                Value = 18 + i % 3,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "metric",
            ApplyMultipleComparisonCorrection = true
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report.SecondaryResults);
        Assert.True(report.SecondaryResults.Count > 0);
    }

    [Fact]
    public async Task AnalyzeAsync_AppliesBonferroniCorrection()
    {
        await SeedContinuousData();
        for (var i = 0; i < 10; i++)
        {
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"variant-{i}",
                ExperimentName = "test-exp",
                TrialKey = "variant-b",
                SubjectId = $"user-{200 + i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "metric",
                Value = 20 + i,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "metric",
            ApplyMultipleComparisonCorrection = true,
            CorrectionMethod = MultipleComparisonMethod.Bonferroni
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report);
    }

    [Fact]
    public async Task AnalyzeAsync_AppliesHolmBonferroniCorrection()
    {
        await SeedContinuousData();
        for (var i = 0; i < 10; i++)
        {
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"variant-{i}",
                ExperimentName = "test-exp",
                TrialKey = "variant-b",
                SubjectId = $"user-{200 + i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "metric",
                Value = 20 + i,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "metric",
            ApplyMultipleComparisonCorrection = true,
            CorrectionMethod = MultipleComparisonMethod.HolmBonferroni
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report);
    }

    [Fact]
    public async Task AnalyzeAsync_FindsControlConditionAutomatically()
    {
        // Use non-standard naming
        for (var i = 0; i < 10; i++)
        {
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"base-{i}",
                ExperimentName = "auto-exp",
                TrialKey = "baseline-control-v1",
                SubjectId = $"u-{i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "m",
                Value = 10 + i,
                Timestamp = DateTimeOffset.UtcNow
            });
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"exp-{i}",
                ExperimentName = "auto-exp",
                TrialKey = "experimental",
                SubjectId = $"u-{100 + i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "m",
                Value = 20 + i,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "m",
            ControlCondition = "nonexistent"  // Force auto-detection
        };

        var report = await analyzer.AnalyzeAsync("auto-exp", options);

        Assert.NotNull(report.PrimaryResult);
    }

    [Fact]
    public async Task AnalyzeAsync_WithSpecificTreatmentConditions()
    {
        await SeedContinuousData();
        for (var i = 0; i < 10; i++)
        {
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"ignored-{i}",
                ExperimentName = "test-exp",
                TrialKey = "ignored-variant",
                SubjectId = $"user-{300 + i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "metric",
                Value = 50 + i,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "metric",
            TreatmentConditions = ["treatment"]
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.Null(report.SecondaryResults);
    }

    [Fact]
    public async Task AnalyzeAsync_WarnsAboutUnderpoweredExperiment()
    {
        // Small sample with small effect
        for (var i = 0; i < 5; i++)
        {
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"c-{i}",
                ExperimentName = "underpowered",
                TrialKey = "control",
                SubjectId = $"u-{i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "m",
                Value = 10 + i * 0.1,
                Timestamp = DateTimeOffset.UtcNow
            });
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"t-{i}",
                ExperimentName = "underpowered",
                TrialKey = "treatment",
                SubjectId = $"u-{10 + i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "m",
                Value = 10.5 + i * 0.1,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "m",
            PerformPowerAnalysis = true,
            CalculateEffectSize = true,
            MinimumSampleSize = 2
        };

        var report = await analyzer.AnalyzeAsync("underpowered", options);

        Assert.NotNull(report.PowerAnalysis);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNoTreatments_ReturnsEmptyReport()
    {
        // Only control data
        for (var i = 0; i < 10; i++)
        {
            await _store.RecordAsync(new ExperimentOutcome
            {
                Id = $"c-{i}",
                ExperimentName = "control-only",
                TrialKey = "control",
                SubjectId = $"u-{i}",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "m",
                Value = 10 + i,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions { MetricName = "m" };

        var report = await analyzer.AnalyzeAsync("control-only", options);

        Assert.NotNull(report.Warnings);
        Assert.Contains(report.Warnings, w => w.Contains("No treatment"));
    }

    [Fact]
    public async Task AnalyzeAsync_ControlWinsConclusion()
    {
        await SeedContinuousData(
            controlValues: [20, 21, 22, 23, 24, 20, 21, 22, 23, 24],
            treatmentValues: [1, 2, 3, 4, 5, 1, 2, 3, 4, 5]);
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions { MetricName = "metric" };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.True(report.PrimaryResult!.IsSignificant);
        Assert.Equal(ExperimentConclusion.ControlWins, report.Conclusion);
    }

    [Fact]
    public async Task AnalyzeAsync_RespectsCancellationToken()
    {
        await SeedContinuousData();
        var analyzer = new ExperimentAnalyzer(_store);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            analyzer.AnalyzeAsync("test-exp", null, cts.Token));
    }

    [Fact]
    public async Task AnalyzeAsync_BinaryData_CalculatesRelativeRisk()
    {
        await SeedBinaryData();
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "conversion",
            CalculateEffectSize = true
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report.EffectSize);
        Assert.Equal("Relative Risk", report.EffectSize.MeasureName);
    }

    [Fact]
    public async Task AnalyzeAsync_NonInferiorityHypothesis()
    {
        // Use slightly different data to produce non-zero effect size
        await SeedContinuousData(
            controlValues: [10, 11, 12, 13, 14, 10, 11, 12, 13, 14],
            treatmentValues: [11, 12, 13, 14, 15, 11, 12, 13, 14, 15]);
        var analyzer = new ExperimentAnalyzer(_store);
        var hypothesis = new HypothesisBuilder("test")
            .NonInferiority()
            .NullHypothesis("Treatment is inferior")
            .AlternativeHypothesis("Treatment is not inferior")
            .PrimaryEndpoint("metric", OutcomeType.Continuous)
            .Build();

        var report = await analyzer.AnalyzeAsync("test-exp", hypothesis);

        Assert.NotNull(report);
    }

    [Fact]
    public async Task AnalyzeAsync_EquivalenceHypothesis()
    {
        // Use slightly different data to produce non-zero effect size
        await SeedContinuousData(
            controlValues: [10, 11, 12, 13, 14, 10, 11, 12, 13, 14],
            treatmentValues: [10.1, 11.1, 12.1, 13.1, 14.1, 10.1, 11.1, 12.1, 13.1, 14.1]);
        var analyzer = new ExperimentAnalyzer(_store);
        var hypothesis = new HypothesisBuilder("test")
            .Equivalence()
            .NullHypothesis("Treatment is different")
            .AlternativeHypothesis("Treatment is equivalent")
            .PrimaryEndpoint("metric", OutcomeType.Continuous)
            .Build();

        var report = await analyzer.AnalyzeAsync("test-exp", hypothesis);

        Assert.NotNull(report);
    }

    [Fact]
    public async Task AnalyzeAsync_WithCustomAlpha()
    {
        await SeedContinuousData();
        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions
        {
            MetricName = "metric",
            Alpha = 0.01
        };

        var report = await analyzer.AnalyzeAsync("test-exp", options);

        Assert.NotNull(report.PrimaryResult);
        Assert.Equal(0.01, report.PrimaryResult.Alpha);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsInconclusiveWhenNoPrimaryResult()
    {
        // Seed only 1 data point per condition (too few for analysis)
        await _store.RecordAsync(new ExperimentOutcome
        {
            Id = "c-0",
            ExperimentName = "tiny",
            TrialKey = "control",
            SubjectId = "u-0",
            OutcomeType = OutcomeType.Continuous,
            MetricName = "m",
            Value = 10,
            Timestamp = DateTimeOffset.UtcNow
        });
        await _store.RecordAsync(new ExperimentOutcome
        {
            Id = "t-0",
            ExperimentName = "tiny",
            TrialKey = "treatment",
            SubjectId = "u-1",
            OutcomeType = OutcomeType.Continuous,
            MetricName = "m",
            Value = 20,
            Timestamp = DateTimeOffset.UtcNow
        });

        var analyzer = new ExperimentAnalyzer(_store);
        var options = new AnalysisOptions { MetricName = "m" };

        var report = await analyzer.AnalyzeAsync("tiny", options);

        Assert.Null(report.PrimaryResult);
        Assert.Equal(ExperimentConclusion.Inconclusive, report.Conclusion);
    }
}
