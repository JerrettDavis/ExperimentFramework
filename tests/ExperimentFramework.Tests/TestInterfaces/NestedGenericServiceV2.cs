namespace ExperimentFramework.Tests.TestInterfaces;

public class NestedGenericServiceV2 : INestedGenericService
{
    public Task<Dictionary<string, List<int>>> GetComplexDataAsync() =>
        Task.FromResult(new Dictionary<string, List<int>>
        {
            ["v2"] = [4, 5, 6]
        });

    public Task<Tuple<string, int, bool>> GetTupleAsync() =>
        Task.FromResult(Tuple.Create("v2", 2, false));

    public ValueTask<KeyValuePair<string, int>> GetKeyValuePairAsync() =>
        ValueTask.FromResult(new KeyValuePair<string, int>("v2", 200));
}
