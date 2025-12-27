using ExperimentFramework.Data;
using ExperimentFramework.Data.Models;
using ExperimentFramework.Data.Recording;
using ExperimentFramework.Data.Storage;
using ExperimentFramework.Science;
using ExperimentFramework.Science.Builders;
using ExperimentFramework.Science.Corrections;
using ExperimentFramework.Science.EffectSize;
using ExperimentFramework.Science.Power;
using ExperimentFramework.Science.Reporting;
using ExperimentFramework.Science.Statistics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("""
    ╔══════════════════════════════════════════════════════════════════════════════╗
    ║                                                                              ║
    ║              ExperimentFramework - Scientific Experimentation                ║
    ║                                                                              ║
    ║  Demonstrates scientific experimentation capabilities:                       ║
    ║    • Hypothesis definition and pre-registration                              ║
    ║    • Power analysis and sample size calculation                              ║
    ║    • Outcome data collection and storage                                     ║
    ║    • Statistical testing (t-test, chi-square, ANOVA)                         ║
    ║    • Effect size calculation (Cohen's d, odds ratio)                         ║
    ║    • Multiple comparison corrections (Bonferroni, Benjamini-Hochberg)        ║
    ║    • Publication-ready report generation                                     ║
    ║                                                                              ║
    ╚══════════════════════════════════════════════════════════════════════════════╝
    """);

// ========================================
// 1. Setup Dependency Injection
// ========================================
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddExperimentDataCollection();
builder.Services.AddExperimentScience();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

var recorder = services.GetRequiredService<IOutcomeRecorder>();
var store = services.GetRequiredService<IOutcomeStore>();

// ========================================
// Demo 1: Power Analysis
// ========================================
Console.WriteLine("\n" + new string('=', 80));
Console.WriteLine("DEMO 1: POWER ANALYSIS");
Console.WriteLine(new string('=', 80));

var powerAnalyzer = PowerAnalyzer.Instance;

Console.WriteLine("\n📊 Sample Size Calculation for Different Effect Sizes:");
Console.WriteLine("-".PadRight(60, '-'));

var effectSizes = new[] { 0.2, 0.5, 0.8 };
foreach (var d in effectSizes)
{
    var n = powerAnalyzer.CalculateSampleSize(d, power: 0.80, alpha: 0.05);
    var magnitude = d switch
    {
        <= 0.2 => "small",
        <= 0.5 => "medium",
        _ => "large"
    };
    Console.WriteLine($"  Effect size d = {d:F1} ({magnitude}): {n} subjects per group");
}

Console.WriteLine("\n📊 Power Analysis for Binary Outcomes:");
Console.WriteLine("-".PadRight(60, '-'));

var binaryOptions = new PowerOptions
{
    OutcomeType = PowerOutcomeType.Binary,
    BaselineProportion = 0.25
};

var binarySampleSize = powerAnalyzer.CalculateSampleSize(
    effectSize: 0.05, // 5pp improvement (25% -> 30%)
    power: 0.80,
    alpha: 0.05,
    options: binaryOptions);

Console.WriteLine($"  Baseline conversion: 25%");
Console.WriteLine($"  Expected improvement: +5pp (25% -> 30%)");
Console.WriteLine($"  Required sample size: {binarySampleSize} per group");

var powerResult = powerAnalyzer.Analyze(
    currentSampleSizePerGroup: 200,
    effectSize: 0.05,
    targetPower: 0.80,
    alpha: 0.05,
    options: binaryOptions);

Console.WriteLine($"\n  With 200 subjects per group:");
Console.WriteLine($"    Achieved power: {powerResult.AchievedPower:P1}");
Console.WriteLine($"    Adequately powered: {(powerResult.IsAdequatelyPowered ? "Yes" : "No")}");
Console.WriteLine($"    MDE at this sample: {powerResult.MinimumDetectableEffect:P1}");

// ========================================
// Demo 2: Hypothesis Definition
// ========================================
Console.WriteLine("\n" + new string('=', 80));
Console.WriteLine("DEMO 2: HYPOTHESIS DEFINITION");
Console.WriteLine(new string('=', 80));

var hypothesis = new HypothesisBuilder("checkout-optimization-v2")
    .Description("Testing the streamlined checkout flow")
    .Superiority()
    .NullHypothesis("The streamlined checkout has no effect on conversion rate")
    .AlternativeHypothesis("The streamlined checkout increases conversion by at least 5%")
    .Rationale("""
        Prior user research showed checkout friction as top complaint.
        The new flow reduces steps from 5 to 2.
        Similar changes at competitors showed 5-8% lifts.
        """)
    .Control("legacy")
    .Treatment("streamlined")
    .PrimaryEndpoint("purchase_completed", OutcomeType.Binary, ep => ep
        .Description("User completes purchase")
        .HigherIsBetter()
        .ExpectedBaseline(0.25)
        .MinimumImportantDifference(0.03))
    .SecondaryEndpoint("checkout_duration", OutcomeType.Duration, ep => ep
        .Description("Time to complete checkout")
        .Unit("seconds")
        .LowerIsBetter())
    .SecondaryEndpoint("cart_abandonment", OutcomeType.Binary, ep => ep
        .Description("User abandons checkout")
        .LowerIsBetter())
    .ExpectedEffectSize(0.05)
    .WithSuccessCriteria(c => c
        .Alpha(0.05)
        .Power(0.80)
        .MinimumSampleSize(1000)
        .PrimaryEndpointOnly()
        .WithMultipleComparisonCorrection()
        .MinimumDuration(TimeSpan.FromDays(14))
        .RequirePositiveEffect())
    .DefinedNow()
    .Build();

Console.WriteLine($"\n📋 Hypothesis: {hypothesis.Name}");
Console.WriteLine($"   Type: {hypothesis.Type}");
Console.WriteLine($"   H0: {hypothesis.NullHypothesis}");
Console.WriteLine($"   H1: {hypothesis.AlternativeHypothesis}");
Console.WriteLine($"   Primary endpoint: {hypothesis.PrimaryEndpoint.Name}");
Console.WriteLine($"   Expected effect: {hypothesis.ExpectedEffectSize:P0}");
Console.WriteLine($"   Alpha: {hypothesis.SuccessCriteria.Alpha}");
Console.WriteLine($"   Power: {hypothesis.SuccessCriteria.Power:P0}");
Console.WriteLine($"   Registered at: {hypothesis.DefinedAt:yyyy-MM-dd HH:mm:ss}");

// ========================================
// Demo 3: Data Collection
// ========================================
Console.WriteLine("\n" + new string('=', 80));
Console.WriteLine("DEMO 3: DATA COLLECTION");
Console.WriteLine(new string('=', 80));

Console.WriteLine("\n📝 Simulating experiment outcomes...");

var random = new Random(42); // Fixed seed for reproducibility

// Simulate control group (25% conversion)
for (int i = 0; i < 500; i++)
{
    var userId = $"user-{i:D4}";
    var converted = random.NextDouble() < 0.25;
    var duration = TimeSpan.FromSeconds(120 + random.Next(60));

    await recorder.RecordBinaryAsync(
        hypothesis.Name, "legacy", userId,
        "purchase_completed", converted);

    await recorder.RecordDurationAsync(
        hypothesis.Name, "legacy", userId,
        "checkout_duration", duration);
}

// Simulate treatment group (30% conversion - 5pp improvement)
for (int i = 500; i < 1000; i++)
{
    var userId = $"user-{i:D4}";
    var converted = random.NextDouble() < 0.30;
    var duration = TimeSpan.FromSeconds(90 + random.Next(45)); // Faster checkout

    await recorder.RecordBinaryAsync(
        hypothesis.Name, "streamlined", userId,
        "purchase_completed", converted);

    await recorder.RecordDurationAsync(
        hypothesis.Name, "streamlined", userId,
        "checkout_duration", duration);
}

Console.WriteLine("   Recorded 1000 outcomes (500 per group)");

// Query aggregations
var aggregations = await store.GetAggregationsAsync(hypothesis.Name, "purchase_completed");

Console.WriteLine("\n📊 Aggregated Results:");
foreach (var (trial, agg) in aggregations.OrderBy(a => a.Key))
{
    Console.WriteLine($"   {trial}: {agg.SuccessCount}/{agg.Count} = {agg.ConversionRate:P1}");
}

// ========================================
// Demo 4: Statistical Analysis
// ========================================
Console.WriteLine("\n" + new string('=', 80));
Console.WriteLine("DEMO 4: STATISTICAL ANALYSIS");
Console.WriteLine(new string('=', 80));

// Get raw data for analysis
var query = new OutcomeQuery
{
    ExperimentName = hypothesis.Name,
    MetricName = "purchase_completed"
};

var outcomes = await store.QueryAsync(query);

var controlData = outcomes
    .Where(o => o.TrialKey == "legacy")
    .Select(o => o.Value)
    .ToArray();

var treatmentData = outcomes
    .Where(o => o.TrialKey == "streamlined")
    .Select(o => o.Value)
    .ToArray();

Console.WriteLine("\n🧪 Chi-Square Test for Conversion:");
Console.WriteLine("-".PadRight(60, '-'));

var chiSquareResult = ChiSquareTest.Instance.Perform(controlData, treatmentData, alpha: 0.05);

Console.WriteLine($"   Test: {chiSquareResult.TestName}");
Console.WriteLine($"   Chi-square statistic: {chiSquareResult.TestStatistic:F3}");
Console.WriteLine($"   p-value: {chiSquareResult.PValue:F4}");
Console.WriteLine($"   Significant at alpha=0.05: {(chiSquareResult.IsSignificant ? "Yes" : "No")}");
Console.WriteLine($"   Difference in proportions: {chiSquareResult.PointEstimate:P1}");
Console.WriteLine($"   95% CI: [{chiSquareResult.ConfidenceIntervalLower:P1}, {chiSquareResult.ConfidenceIntervalUpper:P1}]");

// Duration analysis
var durationQuery = new OutcomeQuery
{
    ExperimentName = hypothesis.Name,
    MetricName = "checkout_duration"
};

var durationOutcomes = await store.QueryAsync(durationQuery);

var controlDurations = durationOutcomes
    .Where(o => o.TrialKey == "legacy")
    .Select(o => o.Value)
    .ToArray();

var treatmentDurations = durationOutcomes
    .Where(o => o.TrialKey == "streamlined")
    .Select(o => o.Value)
    .ToArray();

Console.WriteLine("\n🧪 Two-Sample t-Test for Duration:");
Console.WriteLine("-".PadRight(60, '-'));

var tTestResult = TwoSampleTTest.Instance.Perform(controlDurations, treatmentDurations, alpha: 0.05);

Console.WriteLine($"   Test: {tTestResult.TestName}");
Console.WriteLine($"   t-statistic: {tTestResult.TestStatistic:F3}");
Console.WriteLine($"   p-value: {tTestResult.PValue:F4}");
Console.WriteLine($"   Significant at alpha=0.05: {(tTestResult.IsSignificant ? "Yes" : "No")}");
Console.WriteLine($"   Mean difference: {tTestResult.PointEstimate:F1} seconds");
Console.WriteLine($"   95% CI: [{tTestResult.ConfidenceIntervalLower:F1}, {tTestResult.ConfidenceIntervalUpper:F1}]");

// ========================================
// Demo 5: Effect Size
// ========================================
Console.WriteLine("\n" + new string('=', 80));
Console.WriteLine("DEMO 5: EFFECT SIZE CALCULATION");
Console.WriteLine(new string('=', 80));

Console.WriteLine("\n📏 Cohen's d for Duration:");
Console.WriteLine("-".PadRight(60, '-'));

var cohensD = CohensD.Instance.Calculate(controlDurations, treatmentDurations);

Console.WriteLine($"   Cohen's d: {cohensD.Value:F3}");
Console.WriteLine($"   Magnitude: {cohensD.Magnitude}");
Console.WriteLine($"   95% CI: [{cohensD.ConfidenceIntervalLower:F3}, {cohensD.ConfidenceIntervalUpper:F3}]");

Console.WriteLine("\n📏 Odds Ratio for Conversion:");
Console.WriteLine("-".PadRight(60, '-'));

var controlSuccesses = (int)controlData.Sum();
var treatmentSuccesses = (int)treatmentData.Sum();

var oddsRatio = OddsRatio.Instance.Calculate(
    controlSuccesses, controlData.Length,
    treatmentSuccesses, treatmentData.Length);

Console.WriteLine($"   Odds Ratio: {oddsRatio.Value:F2}");
Console.WriteLine($"   95% CI: [{oddsRatio.ConfidenceIntervalLower:F2}, {oddsRatio.ConfidenceIntervalUpper:F2}]");
Console.WriteLine($"   Interpretation: Treatment has {oddsRatio.Value:F1}x the odds of conversion");

var relativeRisk = RelativeRisk.Instance.Calculate(
    controlSuccesses, controlData.Length,
    treatmentSuccesses, treatmentData.Length);

Console.WriteLine($"\n   Relative Risk: {relativeRisk.Value:F2}");
Console.WriteLine($"   95% CI: [{relativeRisk.ConfidenceIntervalLower:F2}, {relativeRisk.ConfidenceIntervalUpper:F2}]");

// ========================================
// Demo 6: Multiple Comparison Corrections
// ========================================
Console.WriteLine("\n" + new string('=', 80));
Console.WriteLine("DEMO 6: MULTIPLE COMPARISON CORRECTIONS");
Console.WriteLine(new string('=', 80));

var pValues = new[] { 0.01, 0.03, 0.04, 0.06, 0.12 };

Console.WriteLine("\n📊 Raw p-values:");
for (int i = 0; i < pValues.Length; i++)
{
    Console.WriteLine($"   Test {i + 1}: p = {pValues[i]:F3}");
}

Console.WriteLine("\n🔬 Bonferroni Correction (most conservative):");
Console.WriteLine("-".PadRight(60, '-'));

var bonferroniAdjusted = BonferroniCorrection.Instance.AdjustPValues(pValues);
var bonferroniSignificant = BonferroniCorrection.Instance.DetermineSignificance(pValues, 0.05);

for (int i = 0; i < pValues.Length; i++)
{
    var sig = bonferroniSignificant[i] ? "**" : "  ";
    Console.WriteLine($"   {sig}Test {i + 1}: p = {pValues[i]:F3} -> adjusted = {bonferroniAdjusted[i]:F3}");
}
Console.WriteLine($"   Controls for: {BonferroniCorrection.Instance.ControlsFor}");

Console.WriteLine("\n🔬 Holm-Bonferroni Correction (step-down):");
Console.WriteLine("-".PadRight(60, '-'));

var holmAdjusted = HolmBonferroniCorrection.Instance.AdjustPValues(pValues);
var holmSignificant = HolmBonferroniCorrection.Instance.DetermineSignificance(pValues, 0.05);

for (int i = 0; i < pValues.Length; i++)
{
    var sig = holmSignificant[i] ? "**" : "  ";
    Console.WriteLine($"   {sig}Test {i + 1}: p = {pValues[i]:F3} -> adjusted = {holmAdjusted[i]:F3}");
}

Console.WriteLine("\n🔬 Benjamini-Hochberg Correction (FDR control):");
Console.WriteLine("-".PadRight(60, '-'));

var bhAdjusted = BenjaminiHochbergCorrection.Instance.AdjustPValues(pValues);
var bhSignificant = BenjaminiHochbergCorrection.Instance.DetermineSignificance(pValues, 0.05);

for (int i = 0; i < pValues.Length; i++)
{
    var sig = bhSignificant[i] ? "**" : "  ";
    Console.WriteLine($"   {sig}Test {i + 1}: p = {pValues[i]:F3} -> adjusted = {bhAdjusted[i]:F3}");
}
Console.WriteLine($"   Controls for: {BenjaminiHochbergCorrection.Instance.ControlsFor}");

Console.WriteLine("\n   ** = significant at corrected alpha = 0.05");

// ========================================
// Demo 7: Report Generation
// ========================================
Console.WriteLine("\n" + new string('=', 80));
Console.WriteLine("DEMO 7: REPORT GENERATION");
Console.WriteLine(new string('=', 80));

var reporterOptions = new ReporterOptions
{
    IncludeDetailedStatistics = true,
    IncludeEffectSize = true,
    IncludePowerAnalysis = true,
    IncludeWarnings = true,
    IncludeRecommendations = true
};

var markdownReporter = new MarkdownReporter(reporterOptions);

var report = new ExperimentReport
{
    ExperimentName = hypothesis.Name,
    Hypothesis = hypothesis,
    AnalyzedAt = DateTimeOffset.UtcNow,
    StartedAt = DateTimeOffset.UtcNow.AddDays(-14),
    Duration = TimeSpan.FromDays(14),
    Status = ExperimentStatus.Completed,
    Conclusion = chiSquareResult is { IsSignificant: true, PointEstimate: > 0 }
        ? ExperimentConclusion.TreatmentWins
        : ExperimentConclusion.NoSignificantDifference,
    SampleSizes = new Dictionary<string, int>
    {
        ["legacy"] = 500,
        ["streamlined"] = 500
    },
    PrimaryResult = chiSquareResult,
    EffectSize = new EffectSizeResult
    {
        MeasureName = oddsRatio.MeasureName,
        Value = oddsRatio.Value,
        Magnitude = oddsRatio.Magnitude,
        ConfidenceIntervalLower = oddsRatio.ConfidenceIntervalLower,
        ConfidenceIntervalUpper = oddsRatio.ConfidenceIntervalUpper
    },
    PowerAnalysis = powerResult,
    ConditionSummaries = new Dictionary<string, ConditionSummary>
    {
        ["legacy"] = new ConditionSummary
        {
            Condition = "legacy",
            SampleSize = controlData.Length,
            Mean = controlData.Average(),
            SuccessRate = controlData.Average(),
            SuccessCount = (int)controlData.Sum()
        },
        ["streamlined"] = new ConditionSummary
        {
            Condition = "streamlined",
            SampleSize = treatmentData.Length,
            Mean = treatmentData.Average(),
            SuccessRate = treatmentData.Average(),
            SuccessCount = (int)treatmentData.Sum()
        }
    },
    Recommendations = chiSquareResult.IsSignificant
        ? new List<string> { "Treatment shows significant improvement. Consider rollout." }
        : new List<string> { "No significant difference. Consider extending experiment or revising hypothesis." }
};

var markdown = await markdownReporter.GenerateAsync(report);

Console.WriteLine("\n📄 Generated Markdown Report (excerpt):");
Console.WriteLine("-".PadRight(60, '-'));

// Show first 40 lines
var lines = markdown.Split('\n');
foreach (var line in lines.Take(40))
{
    Console.WriteLine(line);
}
if (lines.Length > 40)
{
    Console.WriteLine($"\n   ... ({lines.Length - 40} more lines)");
}

// JSON report
var jsonReporter = new JsonReporter();
var json = await jsonReporter.GenerateAsync(report);

Console.WriteLine("\n📄 JSON Report available for API/dashboard integration");
Console.WriteLine($"   JSON size: {json.Length:N0} characters");

// ========================================
// Summary
// ========================================
Console.WriteLine("\n" + new string('=', 80));
Console.WriteLine("DEMO COMPLETE!");
Console.WriteLine(new string('=', 80));

Console.WriteLine("""

    Summary of Scientific Experimentation Features:

    ✅ Power Analysis
       - Calculate required sample sizes
       - Determine achievable power
       - Find minimum detectable effects

    ✅ Hypothesis Definition
       - Pre-register hypotheses with timestamps
       - Define primary and secondary endpoints
       - Specify success criteria

    ✅ Data Collection
       - Record binary, continuous, count, and duration outcomes
       - Thread-safe in-memory storage
       - Query and aggregate results

    ✅ Statistical Analysis
       - Two-sample t-test (Welch's)
       - Chi-square test for proportions
       - Paired t-test
       - Mann-Whitney U test
       - One-way ANOVA

    ✅ Effect Size
       - Cohen's d with confidence intervals
       - Odds ratio for binary outcomes
       - Relative risk

    ✅ Multiple Comparison Corrections
       - Bonferroni (family-wise error)
       - Holm-Bonferroni (step-down)
       - Benjamini-Hochberg (FDR)

    ✅ Report Generation
       - Publication-ready Markdown
       - Structured JSON for integration

    For more information, see:
      - docs/user-guide/data-collection.md
      - docs/user-guide/statistical-analysis.md
      - docs/user-guide/hypothesis-testing.md
      - docs/user-guide/power-analysis.md

    """);

return 0;
