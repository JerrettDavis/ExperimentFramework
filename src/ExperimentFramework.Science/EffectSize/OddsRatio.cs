using ExperimentFramework.Science.Reporting;
using MathNet.Numerics.Distributions;

namespace ExperimentFramework.Science.EffectSize;

/// <summary>
/// Calculates odds ratio for binary outcomes.
/// </summary>
/// <remarks>
/// <para>
/// The odds ratio compares the odds of an event in the treatment group
/// to the odds in the control group.
/// </para>
/// <para>
/// Formula: OR = (a/c) / (b/d) = (a*d) / (b*c)
/// where:
/// - a = treatment successes, b = treatment failures
/// - c = control successes, d = control failures
/// </para>
/// <para>
/// Interpretation:
/// <list type="bullet">
/// <item><description>OR = 1: No difference</description></item>
/// <item><description>OR &gt; 1: Treatment has higher odds of success</description></item>
/// <item><description>OR &lt; 1: Treatment has lower odds of success</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class OddsRatio : IBinaryEffectSizeCalculator
{
    /// <summary>
    /// The singleton instance.
    /// </summary>
    public static OddsRatio Instance { get; } = new();

    /// <inheritdoc />
    public string Name => "Odds Ratio";

    /// <inheritdoc />
    public EffectSizeResult Calculate(
        int controlSuccesses,
        int controlTotal,
        int treatmentSuccesses,
        int treatmentTotal)
    {
        ValidateInputs(controlSuccesses, controlTotal, treatmentSuccesses, treatmentTotal);

        // Add 0.5 correction for zero cells (Haldane-Anscombe correction)
        var a = treatmentSuccesses;
        var b = treatmentTotal - treatmentSuccesses;
        var c = controlSuccesses;
        var d = controlTotal - controlSuccesses;

        double or;
        double logOrSe;

        if (a == 0 || b == 0 || c == 0 || d == 0)
        {
            // Apply continuity correction
            var aCorr = a + 0.5;
            var bCorr = b + 0.5;
            var cCorr = c + 0.5;
            var dCorr = d + 0.5;

            or = (aCorr * dCorr) / (bCorr * cCorr);
            logOrSe = Math.Sqrt(1 / aCorr + 1 / bCorr + 1 / cCorr + 1 / dCorr);
        }
        else
        {
            // Cast both operands before multiplication to avoid potential integer overflow
            or = ((double)a * (double)d) / ((double)b * (double)c);
            logOrSe = Math.Sqrt(1.0 / a + 1.0 / b + 1.0 / c + 1.0 / d);
        }

        // 95% confidence interval on log scale, then transform
        var normal = new Normal(0, 1);
        var z = normal.InverseCumulativeDistribution(0.975);
        var logOr = Math.Log(or);
        var ciLower = Math.Exp(logOr - z * logOrSe);
        var ciUpper = Math.Exp(logOr + z * logOrSe);

        return new EffectSizeResult
        {
            MeasureName = Name,
            Value = or,
            Magnitude = EffectSizeExtensions.InterpretOddsRatio(or),
            ConfidenceIntervalLower = ciLower,
            ConfidenceIntervalUpper = ciUpper
        };
    }

    private static void ValidateInputs(int controlSuccesses, int controlTotal, int treatmentSuccesses, int treatmentTotal)
    {
        if (controlSuccesses < 0)
            throw new ArgumentOutOfRangeException(nameof(controlSuccesses), "Cannot be negative.");
        if (treatmentSuccesses < 0)
            throw new ArgumentOutOfRangeException(nameof(treatmentSuccesses), "Cannot be negative.");
        if (controlTotal < 1)
            throw new ArgumentOutOfRangeException(nameof(controlTotal), "Must be at least 1.");
        if (treatmentTotal < 1)
            throw new ArgumentOutOfRangeException(nameof(treatmentTotal), "Must be at least 1.");
        if (controlSuccesses > controlTotal)
            throw new ArgumentException("Control successes cannot exceed control total.");
        if (treatmentSuccesses > treatmentTotal)
            throw new ArgumentException("Treatment successes cannot exceed treatment total.");
    }
}
