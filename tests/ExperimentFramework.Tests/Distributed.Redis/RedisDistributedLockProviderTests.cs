using ExperimentFramework.Distributed.Redis;
using StackExchange.Redis;
using Testcontainers.Redis;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Distributed.Redis;

[Feature("RedisDistributedLockProvider provides Redis-backed distributed locking")]
public sealed class RedisDistributedLockProviderTests : TinyBddXunitBase, IAsyncLifetime
{
    private readonly RedisContainer _redis;
    private IConnectionMultiplexer? _connection;

    public RedisDistributedLockProviderTests(ITestOutputHelper output) : base(output)
    {
        _redis = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        _connection = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
        await _redis.DisposeAsync();
    }

    [Scenario("TryAcquire succeeds when lock is available")]
    [Fact]
    public async Task TryAcquire_succeeds_when_available()
    {
        var provider = new RedisDistributedLockProvider(_connection!);

        await using var handle = await provider.TryAcquireAsync("test-lock", TimeSpan.FromSeconds(10));

        Assert.NotNull(handle);
        Assert.True(handle.IsAcquired);
    }

    [Scenario("TryAcquire fails when lock is held")]
    [Fact]
    public async Task TryAcquire_fails_when_held()
    {
        var provider = new RedisDistributedLockProvider(_connection!);

        await using var handle1 = await provider.TryAcquireAsync("contested-lock", TimeSpan.FromSeconds(10));
        var handle2 = await provider.TryAcquireAsync("contested-lock", TimeSpan.FromSeconds(10));

        Assert.NotNull(handle1);
        Assert.Null(handle2);
    }

    [Scenario("Lock is released after dispose")]
    [Fact]
    public async Task Lock_is_released_after_dispose()
    {
        var provider = new RedisDistributedLockProvider(_connection!);

        var handle1 = await provider.TryAcquireAsync("release-test", TimeSpan.FromSeconds(10));
        Assert.NotNull(handle1);
        await handle1.DisposeAsync();

        await using var handle2 = await provider.TryAcquireAsync("release-test", TimeSpan.FromSeconds(10));
        Assert.NotNull(handle2);
    }

    [Scenario("Acquire waits for lock to become available")]
    [Fact]
    public async Task Acquire_waits_for_availability()
    {
        var provider = new RedisDistributedLockProvider(_connection!);

        var handle1 = await provider.TryAcquireAsync("wait-lock", TimeSpan.FromMilliseconds(200));
        Assert.NotNull(handle1);

        // Start acquiring in background
        var acquireTask = provider.AcquireAsync(
            "wait-lock",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(5));

        // Release the first lock after a short delay
        await Task.Delay(100);
        await handle1.DisposeAsync();

        await using var handle2 = await acquireTask;
        Assert.NotNull(handle2);
        Assert.True(handle2.IsAcquired);
    }

    [Scenario("Acquire times out when lock not available")]
    [Fact]
    public async Task Acquire_times_out()
    {
        var provider = new RedisDistributedLockProvider(_connection!);

        await using var handle1 = await provider.TryAcquireAsync("timeout-lock", TimeSpan.FromSeconds(30));
        Assert.NotNull(handle1);

        var handle2 = await provider.AcquireAsync(
            "timeout-lock",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(200));

        Assert.Null(handle2);
    }

    [Scenario("Lock expires after expiration time")]
    [Fact]
    public async Task Lock_expires()
    {
        var provider = new RedisDistributedLockProvider(_connection!);

        var handle1 = await provider.TryAcquireAsync("expiring-lock", TimeSpan.FromMilliseconds(100));
        Assert.NotNull(handle1);

        await Task.Delay(150);

        await using var handle2 = await provider.TryAcquireAsync("expiring-lock", TimeSpan.FromSeconds(10));
        Assert.NotNull(handle2);
    }

    [Scenario("Extend prolongs lock expiration")]
    [Fact]
    public async Task Extend_prolongs_expiration()
    {
        var provider = new RedisDistributedLockProvider(_connection!);

        await using var handle = await provider.TryAcquireAsync("extend-lock", TimeSpan.FromMilliseconds(200));
        Assert.NotNull(handle);

        // Extend before expiration
        var extended = await handle.ExtendAsync(TimeSpan.FromSeconds(5));
        Assert.True(extended);

        // Wait past original expiration
        await Task.Delay(250);

        // Lock should still be held
        Assert.True(handle.IsAcquired);

        // Another client should not be able to acquire
        var handle2 = await provider.TryAcquireAsync("extend-lock", TimeSpan.FromSeconds(10));
        Assert.Null(handle2);
    }

    [Scenario("Extend fails after lock released")]
    [Fact]
    public async Task Extend_fails_after_release()
    {
        var provider = new RedisDistributedLockProvider(_connection!);

        var handle = await provider.TryAcquireAsync("extend-release", TimeSpan.FromSeconds(10));
        Assert.NotNull(handle);

        await handle.DisposeAsync();

        var extended = await handle.ExtendAsync(TimeSpan.FromSeconds(5));
        Assert.False(extended);
    }

    [Scenario("Uses custom key prefix")]
    [Fact]
    public async Task Uses_custom_key_prefix()
    {
        var provider = new RedisDistributedLockProvider(_connection!, "custom:lock:");

        await using var handle = await provider.TryAcquireAsync("prefixed", TimeSpan.FromSeconds(10));
        Assert.NotNull(handle);

        var db = _connection!.GetDatabase();
        var exists = await db.KeyExistsAsync("custom:lock:prefixed");
        Assert.True(exists);
    }

    [Scenario("Multiple dispose calls are safe")]
    [Fact]
    public async Task Multiple_dispose_is_safe()
    {
        var provider = new RedisDistributedLockProvider(_connection!);

        var handle = await provider.TryAcquireAsync("multi-dispose", TimeSpan.FromSeconds(10));
        Assert.NotNull(handle);

        await handle.DisposeAsync();
        await handle.DisposeAsync(); // Should not throw
        await handle.DisposeAsync();
    }

    [Scenario("Lock ID is unique per acquisition")]
    [Fact]
    public async Task LockId_is_unique()
    {
        var provider = new RedisDistributedLockProvider(_connection!);

        await using var handle1 = await provider.TryAcquireAsync("unique-1", TimeSpan.FromSeconds(10));
        await using var handle2 = await provider.TryAcquireAsync("unique-2", TimeSpan.FromSeconds(10));

        Assert.NotNull(handle1);
        Assert.NotNull(handle2);
        Assert.NotEqual(handle1.LockId, handle2.LockId);
    }

    [Scenario("Extend fails for stolen lock")]
    [Fact]
    public async Task Extend_fails_for_stolen_lock()
    {
        var provider = new RedisDistributedLockProvider(_connection!);

        var handle = await provider.TryAcquireAsync("stolen-lock", TimeSpan.FromMilliseconds(100));
        Assert.NotNull(handle);

        // Wait for lock to expire
        await Task.Delay(150);

        // Another client acquires the same lock
        await using var handle2 = await provider.TryAcquireAsync("stolen-lock", TimeSpan.FromSeconds(10));
        Assert.NotNull(handle2);

        // Original handle should fail to extend
        var extended = await handle.ExtendAsync(TimeSpan.FromSeconds(5));
        Assert.False(extended);
    }

    [Scenario("IsAcquired returns false after dispose")]
    [Fact]
    public async Task IsAcquired_false_after_dispose()
    {
        var provider = new RedisDistributedLockProvider(_connection!);

        var handle = await provider.TryAcquireAsync("acquired-check", TimeSpan.FromSeconds(10));
        Assert.NotNull(handle);
        Assert.True(handle.IsAcquired);

        await handle.DisposeAsync();

        Assert.False(handle.IsAcquired);
    }
}
