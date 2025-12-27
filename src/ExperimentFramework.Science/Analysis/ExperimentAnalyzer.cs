using ExperimentFramework.Data.Models;
using ExperimentFramework.Data.Storage;
using ExperimentFramework.Science.Corrections;
using ExperimentFramework.Science.EffectSize;
using ExperimentFramework.Science.Models.Hypothesis;
using ExperimentFramework.Science.Models.Results;
using ExperimentFramework.Science.Power;
using ExperimentFramework.Science.Reporting;
using ExperimentFramework.Science.Statistics;

namespace ExperimentFramework.Science.Analysis;

/// <summary>
/// Default implementation of the experiment analyzer.
/// </summary>
public sealed class ExperimentAnalyzer : IExperimentAnalyzer
{
    private readonly IOutcomeStore _outcomeStore;
    private readonly IPowerAnalyzer _powerAnalyzer;

    /// <summary>
    /// Creates a new experiment analyzer.
    /// </summary>
    /// <param name="outcomeStore">The outcome store to retrieve data from.</param>
    /// <param name="powerAnalyzer">The power analyzer (optional, defaults to singleton).</param>
    public ExperimentAnalyzer(IOutcomeStore outcomeStore, IPowerAnalyzer? powerAnalyzer = null)
    {
        _outcomeStore = outcomeStore ?? throw new ArgumentNullException(nameof(outcomeStore));
        _powerAnalyzer = powerAnalyzer ?? PowerAnalyzer.Instance;
    }

    /// <inheritdoc />
    public Task<ExperimentReport> AnalyzeAsync(
        string experimentName,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return AnalyzeAsync(experimentName, hypothesis: null, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ExperimentReport> AnalyzeAsync(
        string experimentName,
        HypothesisDefinition? hypothesis,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(experimentName);

        options ??= new AnalysisOptions();
        var warnings = new List<string>();
        var recommendations = new List<string>();

        // Determine metric name
        var metricName = options.MetricName ?? hypothesis?.PrimaryEndpoint.Name ?? "default";

        // Get aggregated data
        var aggregations = await _outcomeStore.GetAggregationsAsync(experimentName, metricName, cancellationToken);

        if (aggregations.Count == 0)
        {
            return CreateEmptyReport(experimentName, hypothesis, warnings);
        }

        // Determine control and treatment conditions
        var controlKey = options.ControlCondition;
        if (!aggregations.ContainsKey(controlKey))
        {
            // Try to find a control
            controlKey = aggregations.Keys.FirstOrDefault(k =>
                k.Contains("control", StringComparison.OrdinalIgnoreCase)) ?? aggregations.Keys.First();
        }

        var treatmentKeys = options.TreatmentConditions?.ToList() ??
            aggregations.Keys.Where(k => k != controlKey).ToList();

        if (treatmentKeys.Count == 0)
        {
            warnings.Add("No treatment conditions found for comparison.");
            return CreateEmptyReport(experimentName, hypothesis, warnings);
        }

        // Get sample sizes
        var sampleSizes = aggregations.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Count);

        // Check minimum sample sizes
        foreach (var kvp in sampleSizes.Where(kvp => kvp.Value < options.MinimumSampleSize))
        {
            warnings.Add($"Condition '{kvp.Key}' has only {kvp.Value} samples (minimum: {options.MinimumSampleSize}).");
        }

        // Determine outcome type (check if any successes recorded, indicating binary metric)
        var isBinary = aggregations.Values.Any(a => a.SuccessCount > 0);

        // Get raw data for primary analysis
        var query = new OutcomeQuery
        {
            ExperimentName = experimentName,
            MetricName = metricName
        };
        var outcomes = await _outcomeStore.QueryAsync(query, cancellationToken);
        var dataByCondition = outcomes
            .GroupBy(o => o.TrialKey)
            .ToDictionary(g => g.Key, g => g.Select(o => o.Value).ToList() as IReadOnlyList<double>);

        // Perform primary analysis
        StatisticalTestResult? primaryResult = null;
        EffectSizeResult? effectSize = null;
        var conditionSummaries = new Dictionary<string, ConditionSummary>();

        // Build condition summaries
        foreach (var kvp in aggregations)
        {
            var condition = kvp.Key;
            var agg = kvp.Value;
            conditionSummaries[condition] = new ConditionSummary
            {
                Condition = condition,
                SampleSize = agg.Count,
                Mean = agg.Mean,
                StandardDeviation = agg.Count > 1 ? Math.Sqrt(agg.Variance) : null,
                SuccessRate = isBinary ? agg.ConversionRate : null,
                SuccessCount = isBinary ? agg.SuccessCount : null,
                Minimum = agg.Min,
                Maximum = agg.Max
            };
        }

        // Primary statistical test (first treatment vs control)
        var primaryTreatmentKey = treatmentKeys.First();
        if (dataByCondition.TryGetValue(controlKey, out var controlData) &&
            dataByCondition.TryGetValue(primaryTreatmentKey, out var treatmentData) &&
            controlData.Count >= 2 && treatmentData.Count >= 2)
        {
            primaryResult = PerformStatisticalTest(controlData, treatmentData, isBinary, options.Alpha, hypothesis);

            // Calculate effect size
            if (options.CalculateEffectSize)
            {
                effectSize = CalculateEffectSize(controlData, treatmentData, isBinary, aggregations, controlKey, primaryTreatmentKey);
            }
        }

        // Secondary analyses for other treatments
        var secondaryResults = new Dictionary<string, StatisticalTestResult>();
        if (treatmentKeys.Count > 1 && dataByCondition.TryGetValue(controlKey, out var ctrlData))
        {
            var validTreatments = treatmentKeys
                .Skip(1)
                .Where(key => dataByCondition.TryGetValue(key, out var data) && data.Count >= 2);

            foreach (var treatmentKey in validTreatments)
            {
                var trtData = dataByCondition[treatmentKey];
                var result = PerformStatisticalTest(ctrlData, trtData, isBinary, options.Alpha, hypothesis);
                secondaryResults[treatmentKey] = result;
            }
        }

        // Apply multiple comparison correction
        if (options.ApplyMultipleComparisonCorrection && secondaryResults.Count > 0)
        {
            ApplyCorrection(primaryResult, secondaryResults, options.CorrectionMethod, options.Alpha, warnings);
        }

        // Power analysis
        PowerAnalysisResult? powerAnalysis = null;
        if (options.PerformPowerAnalysis && effectSize != null)
        {
            var minSampleSize = sampleSizes.Values.Min();
            powerAnalysis = _powerAnalyzer.Analyze(
                minSampleSize,
                Math.Abs(effectSize.Value),
                options.TargetPower,
                options.Alpha);

            if (!powerAnalysis.IsAdequatelyPowered)
            {
                warnings.Add($"Experiment is underpowered ({powerAnalysis.AchievedPower:P0} achieved, {options.TargetPower:P0} target). Consider collecting more data.");
                if (powerAnalysis.RequiredSampleSize.HasValue)
                {
                    recommendations.Add($"Collect at least {powerAnalysis.RequiredSampleSize.Value} samples per group to achieve {options.TargetPower:P0} power.");
                }
            }
        }

        // Determine status and conclusion
        var status = DetermineStatus(sampleSizes, options.MinimumSampleSize, hypothesis);
        var conclusion = DetermineConclusion(primaryResult, effectSize, hypothesis);

        // Generate recommendations
        if (options.GenerateRecommendations)
        {
            GenerateRecommendations(primaryResult, effectSize, conclusion, hypothesis, recommendations);
        }

        return new ExperimentReport
        {
            ExperimentName = experimentName,
            Hypothesis = hypothesis,
            AnalyzedAt = DateTimeOffset.UtcNow,
            Status = status,
            Conclusion = conclusion,
            PrimaryResult = primaryResult,
            SecondaryResults = secondaryResults.Count > 0 ? secondaryResults : null,
            EffectSize = effectSize,
            PowerAnalysis = powerAnalysis,
            SampleSizes = sampleSizes,
            ConditionSummaries = conditionSummaries,
            Warnings = warnings.Count > 0 ? warnings : null,
            Recommendations = recommendations.Count > 0 ? recommendations : null
        };
    }

    private static StatisticalTestResult PerformStatisticalTest(
        IReadOnlyList<double> controlData,
        IReadOnlyList<double> treatmentData,
        bool isBinary,
        double alpha,
        HypothesisDefinition? hypothesis)
    {
        var alternativeType = hypothesis?.Type switch
        {
            HypothesisType.Superiority => AlternativeHypothesisType.Greater,
            _ => AlternativeHypothesisType.TwoSided
        };

        if (isBinary)
        {
            return ChiSquareTest.Instance.Perform(controlData, treatmentData, alpha, alternativeType);
        }

        return TwoSampleTTest.Instance.Perform(controlData, treatmentData, alpha, alternativeType);
    }

    private static EffectSizeResult? CalculateEffectSize(
        IReadOnlyList<double> controlData,
        IReadOnlyList<double> treatmentData,
        bool isBinary,
        IReadOnlyDictionary<string, OutcomeAggregation> aggregations,
        string controlKey,
        string treatmentKey)
    {
        if (isBinary)
        {
            if (aggregations.TryGetValue(controlKey, out var controlAgg) &&
                aggregations.TryGetValue(treatmentKey, out var treatmentAgg))
            {
                return RelativeRisk.Instance.Calculate(
                    controlAgg.SuccessCount,
                    controlAgg.Count,
                    treatmentAgg.SuccessCount,
                    treatmentAgg.Count);
            }
            return null;
        }

        return CohensD.Instance.Calculate(controlData, treatmentData);
    }

    private static void ApplyCorrection(
        StatisticalTestResult? primary,
        Dictionary<string, StatisticalTestResult> secondary,
        MultipleComparisonMethod method,
        double alpha,
        List<string> warnings)
    {
        if (primary == null && secondary.Count == 0)
            return;

        IMultipleComparisonCorrection? correction = method switch
        {
            MultipleComparisonMethod.Bonferroni => BonferroniCorrection.Instance,
            MultipleComparisonMethod.HolmBonferroni => HolmBonferroniCorrection.Instance,
            MultipleComparisonMethod.BenjaminiHochberg => BenjaminiHochbergCorrection.Instance,
            _ => null
        };

        if (correction == null)
            return;

        var allPValues = new List<double>();
        if (primary != null) allPValues.Add(primary.PValue);
        allPValues.AddRange(secondary.Values.Select(r => r.PValue));

        var significant = correction.DetermineSignificance(allPValues, alpha);

        // Log if any significances changed
        var originalSignificant = allPValues.Count(p => p < alpha);
        var adjustedSignificant = significant.Count(s => s);

        if (originalSignificant != adjustedSignificant)
        {
            warnings.Add($"Multiple comparison correction ({correction.Name}) changed {originalSignificant} significant results to {adjustedSignificant}.");
        }
    }

    private static ExperimentStatus DetermineStatus(
        IReadOnlyDictionary<string, int> sampleSizes,
        int minimumSampleSize,
        HypothesisDefinition? hypothesis)
    {
        var minSize = sampleSizes.Values.Min();

        if (minSize < minimumSampleSize)
            return ExperimentStatus.Running;

        if (hypothesis?.SuccessCriteria.MinimumSampleSize.HasValue == true &&
            minSize < hypothesis.SuccessCriteria.MinimumSampleSize.Value)
            return ExperimentStatus.Running;

        return ExperimentStatus.Completed;
    }

    private static ExperimentConclusion DetermineConclusion(
        StatisticalTestResult? primaryResult,
        EffectSizeResult? effectSize,
        HypothesisDefinition? hypothesis)
    {
        if (primaryResult == null)
            return ExperimentConclusion.Inconclusive;

        if (!primaryResult.IsSignificant)
            return ExperimentConclusion.NoSignificantDifference;

        var positiveEffect = primaryResult.PointEstimate > 0;

        if (hypothesis?.Type == HypothesisType.Superiority)
        {
            return positiveEffect ? ExperimentConclusion.TreatmentWins : ExperimentConclusion.ControlWins;
        }

        if (hypothesis?.Type == HypothesisType.NonInferiority)
        {
            return ExperimentConclusion.TreatmentNonInferior;
        }

        if (hypothesis?.Type == HypothesisType.Equivalence)
        {
            return ExperimentConclusion.TreatmentEquivalent;
        }

        return positiveEffect ? ExperimentConclusion.TreatmentWins : ExperimentConclusion.ControlWins;
    }

    private static void GenerateRecommendations(
        StatisticalTestResult? primaryResult,
        EffectSizeResult? effectSize,
        ExperimentConclusion conclusion,
        HypothesisDefinition? hypothesis,
        List<string> recommendations)
    {
        switch (conclusion)
        {
            case ExperimentConclusion.TreatmentWins:
                recommendations.Add("Consider rolling out the treatment to all users.");
                if (effectSize is { Magnitude: EffectSizeMagnitude.Small })
                {
                    recommendations.Add("Effect size is small - ensure the improvement justifies implementation costs.");
                }
                break;

            case ExperimentConclusion.ControlWins:
                recommendations.Add("Consider keeping the current (control) implementation.");
                recommendations.Add("Investigate why the treatment performed worse than expected.");
                break;

            case ExperimentConclusion.NoSignificantDifference:
                recommendations.Add("No significant difference detected between conditions.");
                recommendations.Add("Consider if the test is adequately powered to detect the expected effect size.");
                if (hypothesis != null)
                {
                    recommendations.Add("Review the expected effect size assumption in the hypothesis.");
                }
                break;

            case ExperimentConclusion.Inconclusive:
                recommendations.Add("Continue data collection to reach sufficient sample size.");
                break;
        }
    }

    private static ExperimentReport CreateEmptyReport(
        string experimentName,
        HypothesisDefinition? hypothesis,
        List<string> warnings)
    {
        warnings.Add("No data available for analysis.");

        return new ExperimentReport
        {
            ExperimentName = experimentName,
            Hypothesis = hypothesis,
            AnalyzedAt = DateTimeOffset.UtcNow,
            Status = ExperimentStatus.Running,
            Conclusion = ExperimentConclusion.Inconclusive,
            SampleSizes = new Dictionary<string, int>(),
            Warnings = warnings
        };
    }
}
