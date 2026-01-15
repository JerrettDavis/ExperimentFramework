using System.Text.Json;

namespace ExperimentFramework.Simulation.Comparators;

/// <summary>
/// A comparator that serializes objects to JSON and compares their structure.
/// </summary>
/// <typeparam name="TResult">The result type to compare.</typeparam>
public sealed class JsonComparator<TResult> : ISimulationComparator<TResult>
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonComparator{TResult}"/> class.
    /// </summary>
    /// <param name="options">Optional JSON serializer options.</param>
    public JsonComparator(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };
    }

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

        try
        {
            var controlJson = JsonSerializer.Serialize(control, _options);
            var conditionJson = JsonSerializer.Serialize(condition, _options);

            if (controlJson != conditionJson)
            {
                differences.Add($"{conditionName}: JSON structures differ");
                differences.Add($"  Control JSON: {controlJson}");
                differences.Add($"  Condition JSON: {conditionJson}");
            }
        }
        catch (Exception ex)
        {
            differences.Add($"{conditionName}: Failed to compare JSON - {ex.Message}");
        }

        return differences;
    }
}
