namespace ExperimentFramework.Tests.TestInterfaces;

/// <summary>
/// Test interface with generic type parameter.
/// </summary>
public interface IGenericRepository<T>
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<bool> SaveAsync(T entity);
    ValueTask<T?> FindAsync(int id);
}
