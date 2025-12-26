using ExperimentFramework.Science.Reporting;
using MathNet.Numerics.Distributions;

namespace ExperimentFramework.Science.EffectSize;

/// <summary>
/// Calculates relative risk (risk ratio) for binary outcomes.
/// </summary>
/// <remarks>
/// <para>
/// The relative risk compares the probability of an event in the treatment group
/// to the probability in the control group.
/// </para>
/// <para>
/// Formula: RR = (a/(a+b)) / (c/(c+d)) = P(event|treatment) / P(event|control)
/// </para>
/// <para>
/// Interpretation:
/// <list type="bullet">
/// <item><description>RR = 1: No difference in risk</description></item>
/// <item><description>RR &gt; 1: Treatment increases risk</description></item>
/// <item><description>RR &lt; 1: Treatment decreases risk (protective)</description></item>
/// </list>
/// </para>
/// <para>
/// Unlike odds ratio, relative risk has an intuitive interpretation as the
/// factor by which treatment changes the probability of the outcome.
/// </para>
/// </remarks>
public sealed class RelativeRisk : IBinaryEffectSizeCalculator
{
    /// <summary>
    /// The singleton instance.
    /// </summary>
    public static RelativeRisk Instance { get; } = new();

    /// <inheritdoc />
    public string Name => "Relative Risk";

    /// <inheritdoc />
    public EffectSizeResult Calculate(
        int controlSuccesses,
        int controlTotal,
        int treatmentSuccesses,
        int treatmentTotal)
    {
        ValidateInputs(controlSuccesses, controlTotal, treatmentSuccesses, treatmentTotal);

        var pControl = (double)controlSuccesses / controlTotal;
        var pTreatment = (double)treatmentSuccesses / treatmentTotal;

        double rr;
        double logRrSe;

        if (controlSuccesses == 0)
        {
            // Control has no events - RR is undefined/infinite
            rr = treatmentSuccesses > 0 ? double.PositiveInfinity : double.NaN;
            return new EffectSizeResult
            {
                MeasureName = Name,
                Value = rr,
                Magnitude = EffectSizeMagnitude.Large,
                ConfidenceIntervalLower = null,
                ConfidenceIntervalUpper = null
            };
        }

        if (treatmentSuccesses == 0)
        {
            // Treatment has no events
            rr = 0;
            return new EffectSizeResult
            {
                MeasureName = Name,
                Value = rr,
                Magnitude = EffectSizeMagnitude.Large,
                ConfidenceIntervalLower = 0,
                ConfidenceIntervalUpper = null
            };
        }

        rr = pTreatment / pControl;

        // Standard error of log(RR)
        logRrSe = Math.Sqrt(
            (1.0 - pTreatment) / (treatmentSuccesses) +
            (1.0 - pControl) / (controlSuccesses));

        // 95% confidence interval on log scale, then transform
        var normal = new Normal(0, 1);
        var z = normal.InverseCumulativeDistribution(0.975);
        var logRr = Math.Log(rr);
        var ciLower = Math.Exp(logRr - z * logRrSe);
        var ciUpper = Math.Exp(logRr + z * logRrSe);

        return new EffectSizeResult
        {
            MeasureName = Name,
            Value = rr,
            Magnitude = EffectSizeExtensions.InterpretRelativeRisk(rr),
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
