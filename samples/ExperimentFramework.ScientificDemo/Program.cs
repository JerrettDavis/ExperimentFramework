using ExperimentFramework.AutoStop;
using ExperimentFramework.AutoStop.Rules;

Console.WriteLine("""
    ╔══════════════════════════════════════════════════════════════════════════════╗
    ║                                                                              ║
    ║               ExperimentFramework - Scientific Demo                          ║
    ║                                                                              ║
    ║  Demonstrates scientific experimentation and auto-stopping:                  ║
    ║    • Statistical significance testing (p-value based)                        ║
    ║    • Minimum sample size requirements                                        ║
    ║    • Automatic experiment conclusion when significance is reached            ║
    ║                                                                              ║
    ╚══════════════════════════════════════════════════════════════════════════════╝
    """);

// Simulate an A/B test comparing two checkout flows
var random = new Random(42);

// True conversion rates (unknown to the experimenter)
const double controlConversionRate = 0.10;    // 10% conversion
const double treatmentConversionRate = 0.12;  // 12% conversion (20% lift)

Console.WriteLine("\n📊 Simulating A/B test for checkout flow optimization...\n");
Console.WriteLine("Hypothesis: New checkout flow increases conversion rate");
Console.WriteLine($"True control rate: {controlConversionRate:P0} (unknown)");
Console.WriteLine($"True treatment rate: {treatmentConversionRate:P0} (unknown)");
Console.WriteLine();

// Configure stopping rules
var minimumSampleRule = new MinimumSampleSizeRule(minimumSamples: 100);
var significanceRule = new StatisticalSignificanceRule();

// Track experiment data using the framework's types
var controlVariant = new VariantData { Key = "control", IsControl = true };
var treatmentVariant = new VariantData { Key = "treatment", IsControl = false };

const int maxIterations = 5000;
const int reportInterval = 500;

Console.WriteLine("Running experiment...\n");

for (var i = 0; i < maxIterations; i++)
{
    // Simulate user allocation (50/50 split)
    var isControl = random.NextDouble() < 0.5;

    if (isControl)
    {
        controlVariant.SampleSize++;
        if (random.NextDouble() < controlConversionRate)
            controlVariant.Successes++;
    }
    else
    {
        treatmentVariant.SampleSize++;
        if (random.NextDouble() < treatmentConversionRate)
            treatmentVariant.Successes++;
    }

    // Build experiment data for evaluation
    var experimentData = new ExperimentData
    {
        ExperimentName = "checkout-flow-test",
        StartedAt = DateTimeOffset.UtcNow.AddMinutes(-i / 100.0),
        Variants = [controlVariant, treatmentVariant]
    };

    // Report progress
    if ((i + 1) % reportInterval == 0)
    {
        var lift = (treatmentVariant.ConversionRate - controlVariant.ConversionRate)
            / controlVariant.ConversionRate;

        Console.WriteLine($"Iteration {i + 1,5:N0}: " +
            $"Control={controlVariant.ConversionRate:P1} ({controlVariant.SampleSize}), " +
            $"Treatment={treatmentVariant.ConversionRate:P1} ({treatmentVariant.SampleSize}), " +
            $"Lift={lift:+0.0%;-0.0%}");

        // Check minimum sample rule
        var minSampleResult = minimumSampleRule.Evaluate(experimentData);
        if (!minSampleResult.ShouldStop && minSampleResult.Reason != null)
        {
            Console.WriteLine($"   ℹ️ {minSampleResult.Reason}");
        }

        // Check significance rule (only after min samples reached)
        if (minSampleResult.ShouldStop)
        {
            var sigResult = significanceRule.Evaluate(experimentData);
            if (sigResult.ShouldStop)
            {
                Console.WriteLine($"\n   🎯 STOPPING: {sigResult.Reason}");
                if (sigResult.WinningVariant != null)
                    Console.WriteLine($"   Winner: {sigResult.WinningVariant}");
                break;
            }
        }
    }
}

// Final results
var finalLift = (treatmentVariant.ConversionRate - controlVariant.ConversionRate)
    / controlVariant.ConversionRate;

Console.WriteLine("\n" + new string('═', 80));
Console.WriteLine("EXPERIMENT RESULTS");
Console.WriteLine(new string('═', 80));
Console.WriteLine($"""

    Control:   {controlVariant.ConversionRate:P2} ({controlVariant.Successes}/{controlVariant.SampleSize} conversions)
    Treatment: {treatmentVariant.ConversionRate:P2} ({treatmentVariant.Successes}/{treatmentVariant.SampleSize} conversions)

    Relative Lift: {finalLift:+0.0%;-0.0%}

    Key Takeaways:

    • MinimumSampleSizeRule ensures statistical validity
    • StatisticalSignificanceRule detects when results are conclusive
    • Auto-stopping saves resources by concluding experiments early
    • Always verify with your statistics team for production experiments

    """);

Console.WriteLine("✅ Demo completed successfully!");
