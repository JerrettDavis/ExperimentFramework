using System.Collections.Concurrent;
using ExperimentFramework.Decorators;

namespace ExperimentFramework.ComprehensiveSample.Decorators;

/// <summary>
/// Custom decorator that caches experiment results based on method arguments
/// </summary>
public class CachingDecorator : IExperimentDecorator
{
    private readonly ConcurrentDictionary<string, object?> _cache = new();

    public int Order => 2; // Execute after timing decorator

    public async ValueTask<object?> InvokeAsync(
        InvocationContext context,
        Func<ValueTask<object?>> next)
    {
        // Create cache key from method name + arguments
        var cacheKey = CreateCacheKey(context);

        if (_cache.TryGetValue(cacheKey, out var cachedResult))
        {
            Console.WriteLine($"    [CachingDecorator] Cache HIT for '{cacheKey}'");
            return cachedResult;
        }

        Console.WriteLine($"    [CachingDecorator] Cache MISS for '{cacheKey}'");
        var result = await next();

        // Cache the result
        _cache[cacheKey] = result;
        return result;
    }

    private static string CreateCacheKey(InvocationContext context)
    {
        var argsKey = context.Arguments.Length > 0
            ? string.Join(":", context.Arguments.Select(a => a?.ToString() ?? "null"))
            : "no-args";
        return $"{context.ServiceType.Name}.{context.MethodName}({argsKey})";
    }
}

/// <summary>
/// Factory for creating caching decorators (singleton instance shared across experiments)
/// </summary>
public class CachingDecoratorFactory : IExperimentDecoratorFactory
{
    private static readonly CachingDecorator _instance = new();

    public IExperimentDecorator Create(IServiceProvider services)
        => _instance; // Share cache across all experiments
}
