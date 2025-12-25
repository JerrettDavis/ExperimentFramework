namespace ExperimentFramework.Tests.TestInterfaces;

public class NestedGenericServiceV1 : INestedGenericService
{
    public Task<Dictionary<string, List<int>>> GetComplexDataAsync() =>
        Task.FromResult(new Dictionary<string, List<int>>
        {
            ["v1"] = [1, 2, 3]
        });

    public Task<Tuple<string, int, bool>> GetTupleAsync() =>
        Task.FromResult(Tuple.Create("v1", 1, true));

    public ValueTask<KeyValuePair<string, int>> GetKeyValuePairAsync() =>
        ValueTask.FromResult(new KeyValuePair<string, int>("v1", 100));
}
