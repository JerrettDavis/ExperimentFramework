namespace ExperimentFramework.SampleConsole.Contexts;

public interface IMyDatabase
{
    Task<string> GetDatabaseNameAsync(CancellationToken ct);
}