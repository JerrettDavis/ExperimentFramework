namespace ExperimentFramework.Science.Reporting;

/// <summary>
/// Defines the contract for generating experiment reports.
/// </summary>
public interface IExperimentReporter
{
    /// <summary>
    /// Generates a report in a specific format.
    /// </summary>
    /// <param name="report">The experiment report data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The formatted report as a string.</returns>
    Task<string> GenerateAsync(ExperimentReport report, CancellationToken cancellationToken = default);
}

/// <summary>
/// Reporter options.
/// </summary>
public sealed class ReporterOptions
{
    /// <summary>
    /// Gets or sets whether to include detailed statistics.
    /// </summary>
    public bool IncludeDetailedStatistics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include effect size information.
    /// </summary>
    public bool IncludeEffectSize { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include power analysis.
    /// </summary>
    public bool IncludePowerAnalysis { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include warnings.
    /// </summary>
    public bool IncludeWarnings { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include recommendations.
    /// </summary>
    public bool IncludeRecommendations { get; set; } = true;

    /// <summary>
    /// Gets or sets the date/time format string.
    /// </summary>
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss UTC";

    /// <summary>
    /// Gets or sets the number of decimal places for statistics.
    /// </summary>
    public int DecimalPlaces { get; set; } = 4;
}
