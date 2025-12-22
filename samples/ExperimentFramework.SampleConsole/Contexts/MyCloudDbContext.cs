namespace ExperimentFramework.SampleConsole.Contexts;

public sealed class MyCloudDbContext : IMyDatabase
{
    public Task<string> GetDatabaseNameAsync(CancellationToken ct)
        => Task.FromResult("CloudDb");
}