namespace ExperimentFramework.Tests.TestInterfaces;

/// <summary>
/// Test interface with various async method signatures.
/// </summary>
public interface IAsyncService
{
    Task<string> GetStringAsync();
    Task<int> GetIntAsync();
    Task<List<string>> GetListAsync();
    ValueTask<string> GetStringValueTaskAsync();
    ValueTask<int> GetIntValueTaskAsync();
    Task VoidTaskAsync();
    ValueTask VoidValueTaskAsync();
}
