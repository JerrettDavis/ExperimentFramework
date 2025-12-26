namespace ExperimentFramework.Science.Models.Hypothesis;

/// <summary>
/// Defines the type of statistical hypothesis being tested.
/// </summary>
public enum HypothesisType
{
    /// <summary>
    /// Superiority test: treatment is better than control.
    /// </summary>
    /// <remarks>
    /// H0: μT ≤ μC (treatment is not better)
    /// H1: μT > μC (treatment is better)
    /// </remarks>
    Superiority,

    /// <summary>
    /// Non-inferiority test: treatment is not worse than control by more than a margin.
    /// </summary>
    /// <remarks>
    /// H0: μT ≤ μC - Δ (treatment is inferior by more than margin)
    /// H1: μT > μC - Δ (treatment is not inferior by more than margin)
    /// </remarks>
    NonInferiority,

    /// <summary>
    /// Equivalence test: treatment is within an acceptable range of control.
    /// </summary>
    /// <remarks>
    /// H0: |μT - μC| ≥ Δ (treatment differs by more than margin)
    /// H1: |μT - μC| < Δ (treatment is equivalent within margin)
    /// Uses two one-sided tests (TOST) procedure.
    /// </remarks>
    Equivalence,

    /// <summary>
    /// Two-sided test: treatment differs from control in either direction.
    /// </summary>
    /// <remarks>
    /// H0: μT = μC (no difference)
    /// H1: μT ≠ μC (difference exists)
    /// </remarks>
    TwoSided
}
