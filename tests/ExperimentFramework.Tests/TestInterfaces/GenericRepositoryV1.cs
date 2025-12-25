using System.Collections.Concurrent;

namespace ExperimentFramework.Tests.TestInterfaces;

public class GenericRepositoryV1<T> : IGenericRepository<T> where T : class
{
    // Shared data store across all instances to support scope-per-invocation pattern
    private static readonly ConcurrentBag<T> _sharedData = [];

    public Task<T?> GetByIdAsync(int id) => Task.FromResult<T?>(default);
    public Task<List<T>> GetAllAsync() => Task.FromResult(_sharedData.ToList());
    public Task<bool> SaveAsync(T entity)
    {
        _sharedData.Add(entity);
        return Task.FromResult(true);
    }
    public ValueTask<T?> FindAsync(int id) => ValueTask.FromResult<T?>(default);
}
