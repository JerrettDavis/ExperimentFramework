using ExperimentFramework.Science.Models.Results;

namespace ExperimentFramework.Science.Statistics;

/// <summary>
/// Defines the contract for statistical hypothesis tests.
/// </summary>
public interface IStatisticalTest
{
    /// <summary>
    /// Gets the name of this statistical test.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Performs the statistical test on the provided data.
    /// </summary>
    /// <param name="controlData">The control group data.</param>
    /// <param name="treatmentData">The treatment group data.</param>
    /// <param name="alpha">The significance level (default 0.05).</param>
    /// <param name="alternativeType">The type of alternative hypothesis (default two-sided).</param>
    /// <returns>The test result including p-value, confidence interval, etc.</returns>
    StatisticalTestResult Perform(
        IReadOnlyList<double> controlData,
        IReadOnlyList<double> treatmentData,
        double alpha = 0.05,
        AlternativeHypothesisType alternativeType = AlternativeHypothesisType.TwoSided);
}

/// <summary>
/// Defines the contract for statistical tests that compare paired samples.
/// </summary>
public interface IPairedStatisticalTest
{
    /// <summary>
    /// Gets the name of this statistical test.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Performs the paired statistical test on the provided data.
    /// </summary>
    /// <param name="before">The before/control measurements.</param>
    /// <param name="after">The after/treatment measurements (must have same length as before).</param>
    /// <param name="alpha">The significance level (default 0.05).</param>
    /// <param name="alternativeType">The type of alternative hypothesis (default two-sided).</param>
    /// <returns>The test result including p-value, confidence interval, etc.</returns>
    StatisticalTestResult Perform(
        IReadOnlyList<double> before,
        IReadOnlyList<double> after,
        double alpha = 0.05,
        AlternativeHypothesisType alternativeType = AlternativeHypothesisType.TwoSided);
}

/// <summary>
/// Defines the contract for statistical tests that compare multiple groups.
/// </summary>
public interface IMultiGroupStatisticalTest
{
    /// <summary>
    /// Gets the name of this statistical test.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Performs the statistical test comparing multiple groups.
    /// </summary>
    /// <param name="groups">Dictionary mapping group names to their data.</param>
    /// <param name="alpha">The significance level (default 0.05).</param>
    /// <returns>The test result including F-statistic, p-value, etc.</returns>
    StatisticalTestResult Perform(
        IReadOnlyDictionary<string, IReadOnlyList<double>> groups,
        double alpha = 0.05);
}
