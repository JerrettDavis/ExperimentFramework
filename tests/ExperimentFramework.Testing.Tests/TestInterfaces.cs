namespace ExperimentFramework.Testing.Tests;

// Test interfaces and implementations
public interface IMyDatabase
{
    Task<string> GetConnectionStringAsync();
    int GetValue();
}

public class MyDatabase : IMyDatabase
{
    public Task<string> GetConnectionStringAsync()
    {
        return Task.FromResult("localhost");
    }

    public int GetValue()
    {
        return 1;
    }
}

public class CloudDatabase : IMyDatabase
{
    public Task<string> GetConnectionStringAsync()
    {
        return Task.FromResult("cloud.example.com");
    }

    public int GetValue()
    {
        return 2;
    }
}
