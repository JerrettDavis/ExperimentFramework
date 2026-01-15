namespace ExperimentFramework.Simulation.Comparators;

/// <summary>
/// A comparator that uses default equality comparison.
/// </summary>
/// <typeparam name="TResult">The result type to compare.</typeparam>
public sealed class EqualityComparator<TResult> : ISimulationComparator<TResult>
{
    /// <inheritdoc/>
    public IReadOnlyList<string> Compare(TResult? control, TResult? condition, string conditionName)
    {
        var differences = new List<string>();

        if (control == null && condition == null)
        {
            return differences;
        }

        if (control == null)
        {
            differences.Add($"{conditionName}: Control is null but condition is not null");
            return differences;
        }

        if (condition == null)
        {
            differences.Add($"{conditionName}: Condition is null but control is not null");
            return differences;
        }

        if (!EqualityComparer<TResult>.Default.Equals(control, condition))
        {
            differences.Add($"{conditionName}: Values differ - Control: {control}, Condition: {condition}");
        }

        return differences;
    }
}
