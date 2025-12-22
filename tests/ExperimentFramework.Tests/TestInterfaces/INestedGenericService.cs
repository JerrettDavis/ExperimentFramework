namespace ExperimentFramework.Tests.TestInterfaces;

/// <summary>
/// Test interface with nested generic return types.
/// </summary>
public interface INestedGenericService
{
    Task<Dictionary<string, List<int>>> GetComplexDataAsync();
    Task<Tuple<string, int, bool>> GetTupleAsync();
    ValueTask<KeyValuePair<string, int>> GetKeyValuePairAsync();
}
