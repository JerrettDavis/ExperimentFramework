namespace ExperimentFramework.SampleConsole.Contexts;

public sealed class MyDbContext : IMyDatabase
{
    public Task<string> GetDatabaseNameAsync(CancellationToken ct)
        => Task.FromResult("LocalDb");
}