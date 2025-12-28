using ExperimentFramework.Distributed;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Distributed;

[Feature("In-memory distributed lock provider provides mutual exclusion")]
public sealed class InMemoryDistributedLockProviderTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("TryAcquire succeeds when lock is free")]
    [Fact]
    public async Task TryAcquire_succeeds_when_free()
    {
        var provider = new InMemoryDistributedLockProvider();
        var handle = await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));
        Assert.NotNull(handle);
        Assert.True(handle.IsAcquired);
    }

    [Scenario("TryAcquire fails when lock is held")]
    [Fact]
    public async Task TryAcquire_fails_when_held()
    {
        var provider = new InMemoryDistributedLockProvider();
        await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));
        var handle = await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));
        Assert.Null(handle);
    }

    [Scenario("TryAcquire succeeds for different locks")]
    [Fact]
    public async Task TryAcquire_different_locks_succeed()
    {
        var provider = new InMemoryDistributedLockProvider();
        var handle1 = await provider.TryAcquireAsync("lock-1", TimeSpan.FromMinutes(5));
        var handle2 = await provider.TryAcquireAsync("lock-2", TimeSpan.FromMinutes(5));
        Assert.NotNull(handle1);
        Assert.True(handle1.IsAcquired);
        Assert.NotNull(handle2);
        Assert.True(handle2.IsAcquired);
    }

    [Scenario("Lock handle has unique ID")]
    [Fact]
    public async Task Lock_handle_has_id()
    {
        var provider = new InMemoryDistributedLockProvider();
        var handle = await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));
        Assert.NotNull(handle);
        Assert.False(string.IsNullOrEmpty(handle.LockId));
    }

    [Scenario("Dispose releases the lock")]
    [Fact]
    public async Task Dispose_releases_lock()
    {
        var provider = new InMemoryDistributedLockProvider();
        var handle = await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));
        await handle!.DisposeAsync();
        var newHandle = await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));
        Assert.NotNull(newHandle);
        Assert.True(newHandle.IsAcquired);
    }

    [Scenario("Extend prolongs lock lifetime")]
    [Fact]
    public async Task Extend_prolongs_lock()
    {
        var provider = new InMemoryDistributedLockProvider();
        var handle = await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));
        var result = await handle!.ExtendAsync(TimeSpan.FromMinutes(10));
        Assert.True(result);
    }

    [Scenario("Extend fails after dispose")]
    [Fact]
    public async Task Extend_fails_after_dispose()
    {
        var provider = new InMemoryDistributedLockProvider();
        var handle = await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));
        await handle!.DisposeAsync();
        var result = await handle.ExtendAsync(TimeSpan.FromMinutes(10));
        Assert.False(result);
    }

    [Scenario("IsAcquired is false after dispose")]
    [Fact]
    public async Task IsAcquired_false_after_dispose()
    {
        var provider = new InMemoryDistributedLockProvider();
        var handle = await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));
        await handle!.DisposeAsync();
        Assert.False(handle.IsAcquired);
    }

    [Scenario("Acquire waits for lock to be released")]
    [Fact]
    public async Task Acquire_waits_for_release()
    {
        var provider = new InMemoryDistributedLockProvider();
        var handle = await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));

        var acquireTask = provider.AcquireAsync(
            "test-lock",
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(2));

        await Task.Delay(100);
        await handle!.DisposeAsync();

        var newHandle = await acquireTask;
        Assert.NotNull(newHandle);
        Assert.True(newHandle.IsAcquired);
    }

    [Scenario("Acquire returns null on timeout")]
    [Fact]
    public async Task Acquire_returns_null_on_timeout()
    {
        var provider = new InMemoryDistributedLockProvider();
        await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));

        var handle = await provider.AcquireAsync(
            "test-lock",
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMilliseconds(100));
        Assert.Null(handle);
    }

    [Scenario("Expired lock can be re-acquired")]
    [Fact]
    public async Task Expired_lock_can_be_reacquired()
    {
        var provider = new InMemoryDistributedLockProvider();
        await provider.TryAcquireAsync("test-lock", TimeSpan.FromMilliseconds(1));
        await Task.Delay(50); // Wait for expiration
        var handle = await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));
        Assert.NotNull(handle);
        Assert.True(handle.IsAcquired);
    }

    [Scenario("Multiple dispose calls are safe")]
    [Fact]
    public async Task Multiple_dispose_calls_safe()
    {
        var provider = new InMemoryDistributedLockProvider();
        var handle = await provider.TryAcquireAsync("test-lock", TimeSpan.FromMinutes(5));
        await handle!.DisposeAsync();
        await handle.DisposeAsync();
        await handle.DisposeAsync();
        // No exception means success
    }
}
