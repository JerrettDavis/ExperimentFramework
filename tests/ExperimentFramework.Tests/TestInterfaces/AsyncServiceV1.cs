namespace ExperimentFramework.Tests.TestInterfaces;

public class AsyncServiceV1 : IAsyncService
{
    public Task<string> GetStringAsync() => Task.FromResult("v1-string");
    public Task<int> GetIntAsync() => Task.FromResult(1);
    public Task<List<string>> GetListAsync() => Task.FromResult(new List<string> { "v1-a", "v1-b" });
    public ValueTask<string> GetStringValueTaskAsync() => ValueTask.FromResult("v1-valuestring");
    public ValueTask<int> GetIntValueTaskAsync() => ValueTask.FromResult(10);
    public Task VoidTaskAsync() => Task.CompletedTask;
    public ValueTask VoidValueTaskAsync() => ValueTask.CompletedTask;
}
