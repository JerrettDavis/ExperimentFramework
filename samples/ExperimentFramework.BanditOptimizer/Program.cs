using ExperimentFramework.Bandit;
using ExperimentFramework.Bandit.Algorithms;

Console.WriteLine("""
    ╔══════════════════════════════════════════════════════════════════════════════╗
    ║                                                                              ║
    ║               ExperimentFramework - Bandit Optimizer Sample                  ║
    ║                                                                              ║
    ║  Demonstrates multi-armed bandit algorithms for adaptive experimentation:   ║
    ║    • Epsilon-Greedy: Simple exploration/exploitation tradeoff               ║
    ║    • Thompson Sampling: Bayesian approach with probability matching         ║
    ║    • Upper Confidence Bound (UCB1): Optimism in face of uncertainty         ║
    ║                                                                              ║
    ╚══════════════════════════════════════════════════════════════════════════════╝
    """);

// Simulate a recommendation system with 4 content variants
// True conversion rates (unknown to the algorithm)
var trueConversionRates = new Dictionary<string, double>
{
    ["variant-a"] = 0.05,  // 5% conversion
    ["variant-b"] = 0.12,  // 12% conversion (best)
    ["variant-c"] = 0.08,  // 8% conversion
    ["variant-d"] = 0.03   // 3% conversion
};

var armNames = trueConversionRates.Keys.ToArray();
var random = new Random(42); // Fixed seed for reproducibility

Console.WriteLine("\n📊 True Conversion Rates (unknown to algorithms):");
foreach (var (arm, rate) in trueConversionRates)
{
    Console.WriteLine($"   {arm}: {rate:P1}");
}

// Run comparison of all three algorithms
const int totalIterations = 10000;
const int reportInterval = 2000;

Console.WriteLine($"\n🎯 Running {totalIterations:N0} iterations for each algorithm...\n");

// Test each algorithm
RunAlgorithmComparison("Epsilon-Greedy (ε=0.1)",
    new EpsilonGreedy(epsilon: 0.1, seed: 42), armNames, trueConversionRates, random, totalIterations, reportInterval);

RunAlgorithmComparison("Thompson Sampling",
    new ThompsonSampling(seed: 42), armNames, trueConversionRates, random, totalIterations, reportInterval);

RunAlgorithmComparison("UCB1",
    new UpperConfidenceBound(), armNames, trueConversionRates, random, totalIterations, reportInterval);

// Summary
Console.WriteLine("\n" + new string('═', 80));
Console.WriteLine("SUMMARY");
Console.WriteLine(new string('═', 80));
Console.WriteLine("""

    Key Observations:

    • Epsilon-Greedy: Simple but effective. The ε parameter controls exploration.
      Converges quickly but may not fully exploit the best arm.

    • Thompson Sampling: Bayesian approach that naturally balances exploration
      and exploitation. Often achieves best cumulative reward.

    • UCB1: Deterministic selection based on confidence bounds. Good theoretical
      guarantees but can be slower to converge than Thompson Sampling.

    Best Practices:

    1. Use Thompson Sampling for most scenarios - it's robust and effective
    2. Use UCB1 when you need deterministic behavior
    3. Use Epsilon-Greedy for simple scenarios or when interpretability matters
    4. Always integrate with AutoStop to detect when experiments can conclude

    """);

Console.WriteLine("✅ Demo completed successfully!");

static void RunAlgorithmComparison(
    string algorithmName,
    IBanditAlgorithm algorithm,
    string[] armNames,
    Dictionary<string, double> trueRates,
    Random random,
    int iterations,
    int reportInterval)
{
    Console.WriteLine($"─── {algorithmName} {"─".PadRight(50, '─')}");

    // Create arm statistics
    var arms = armNames.Select(name => new ArmStatistics { Key = name }).ToList();

    var totalReward = 0.0;
    var optimalArm = trueRates.MaxBy(kvp => kvp.Value).Key;
    var optimalRate = trueRates[optimalArm];
    var optimalPulls = 0;

    for (var i = 0; i < iterations; i++)
    {
        // Select arm using the algorithm
        var selectedIndex = algorithm.SelectArm(arms);
        var selectedArm = arms[selectedIndex];

        // Simulate reward based on true conversion rate
        var reward = random.NextDouble() < trueRates[selectedArm.Key] ? 1.0 : 0.0;
        totalReward += reward;

        if (selectedArm.Key == optimalArm)
            optimalPulls++;

        // Update the algorithm with the observed reward
        algorithm.UpdateArm(selectedArm, reward);

        // Report progress
        if ((i + 1) % reportInterval == 0)
        {
            var regret = (i + 1) * optimalRate - totalReward;

            Console.WriteLine($"   Iteration {i + 1,6:N0}: " +
                $"Reward={totalReward,7:N1} | " +
                $"Regret={regret,7:N1} | " +
                $"Optimal%={100.0 * optimalPulls / (i + 1),5:N1}%");
        }
    }

    // Final statistics
    Console.WriteLine($"\n   Final Arm Statistics:");
    foreach (var arm in arms.OrderByDescending(a => a.AverageReward))
    {
        var actualRate = trueRates[arm.Key];
        var indicator = arm.Key == optimalArm ? " ← BEST" : "";
        Console.WriteLine($"      {arm.Key}: Mean={arm.AverageReward:P1}, Pulls={arm.Pulls,5:N0}, " +
            $"Actual={actualRate:P1}{indicator}");
    }

    var totalRegret = iterations * optimalRate - totalReward;
    Console.WriteLine($"\n   Total Regret: {totalRegret:N1} " +
        $"(lower is better, optimal would select best arm every time)");
    Console.WriteLine($"   Optimal Arm Selection Rate: {100.0 * optimalPulls / iterations:N1}%\n");
}
