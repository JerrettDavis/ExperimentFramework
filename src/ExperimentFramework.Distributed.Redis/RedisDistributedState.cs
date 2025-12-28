using System.Text.Json;
using StackExchange.Redis;

namespace ExperimentFramework.Distributed.Redis;

/// <summary>
/// Redis implementation of distributed experiment state.
/// </summary>
public sealed class RedisDistributedState : IDistributedExperimentState
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _keyPrefix;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new Redis distributed state.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="options">Optional configuration options.</param>
    public RedisDistributedState(IConnectionMultiplexer redis, RedisDistributedStateOptions? options = null)
    {
        _redis = redis;
        _keyPrefix = options?.KeyPrefix ?? "experiment:state:";
        _jsonOptions = options?.JsonSerializerOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(_keyPrefix + key);

        if (value.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>((string)value!, _jsonOptions);
    }

    /// <inheritdoc />
    public async ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await db.StringSetAsync(_keyPrefix + key, json, expiration);
    }

    /// <inheritdoc />
    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(_keyPrefix + key);
    }

    /// <inheritdoc />
    public async ValueTask<long> IncrementAsync(string key, long delta = 1, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        return await db.StringIncrementAsync(_keyPrefix + key, delta);
    }
}

/// <summary>
/// Configuration options for Redis distributed state.
/// </summary>
public sealed class RedisDistributedStateOptions
{
    /// <summary>
    /// Gets or sets the key prefix for all state keys.
    /// </summary>
    public string KeyPrefix { get; set; } = "experiment:state:";

    /// <summary>
    /// Gets or sets the JSON serializer options.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}
