namespace ExperimentFramework.Tests.TestInterfaces;

public class AsyncServiceV2 : IAsyncService
{
    public Task<string> GetStringAsync() => Task.FromResult("v2-string");
    public Task<int> GetIntAsync() => Task.FromResult(2);
    public Task<List<string>> GetListAsync() => Task.FromResult(new List<string> { "v2-a", "v2-b" });
    public ValueTask<string> GetStringValueTaskAsync() => ValueTask.FromResult("v2-valuestring");
    public ValueTask<int> GetIntValueTaskAsync() => ValueTask.FromResult(20);
    public Task VoidTaskAsync() => Task.CompletedTask;
    public ValueTask VoidValueTaskAsync() => ValueTask.CompletedTask;
}
