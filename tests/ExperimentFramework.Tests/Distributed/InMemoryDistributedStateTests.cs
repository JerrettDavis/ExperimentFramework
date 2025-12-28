using ExperimentFramework.Distributed;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Distributed;

[Feature("In-memory distributed state provides simple key-value storage")]
public sealed class InMemoryDistributedStateTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Get returns null for missing key")]
    [Fact]
    public async Task Get_returns_null_for_missing_key()
    {
        var state = new InMemoryDistributedState();
        var result = await state.GetAsync<string>("missing-key");
        Assert.Null(result);
    }

    [Scenario("Set and Get round trip")]
    [Fact]
    public async Task Set_and_get_round_trip()
    {
        var state = new InMemoryDistributedState();
        await state.SetAsync("key", "test-value");
        var result = await state.GetAsync<string>("key");
        Assert.Equal("test-value", result);
    }

    [Scenario("Set overwrites existing value")]
    [Fact]
    public async Task Set_overwrites_existing()
    {
        var state = new InMemoryDistributedState();
        await state.SetAsync("key", "old-value");
        await state.SetAsync("key", "new-value");
        var result = await state.GetAsync<string>("key");
        Assert.Equal("new-value", result);
    }

    [Scenario("Remove deletes value")]
    [Fact]
    public async Task Remove_deletes_value()
    {
        var state = new InMemoryDistributedState();
        await state.SetAsync("key", "test-value");
        await state.RemoveAsync("key");
        var result = await state.GetAsync<string>("key");
        Assert.Null(result);
    }

    [Scenario("Remove non-existent key does not throw")]
    [Fact]
    public async Task Remove_nonexistent_does_not_throw()
    {
        var state = new InMemoryDistributedState();
        await state.RemoveAsync("missing-key");
        // No exception means success
    }

    [Scenario("Increment creates counter from 0")]
    [Fact]
    public async Task Increment_creates_counter()
    {
        var state = new InMemoryDistributedState();
        var result = await state.IncrementAsync("counter");
        Assert.Equal(1, result);
    }

    [Scenario("Increment adds to existing counter")]
    [Fact]
    public async Task Increment_adds_to_existing()
    {
        var state = new InMemoryDistributedState();
        await state.IncrementAsync("counter", 5);
        var result = await state.IncrementAsync("counter", 3);
        Assert.Equal(8, result);
    }

    [Scenario("Increment with negative delta decrements")]
    [Fact]
    public async Task Increment_negative_decrements()
    {
        var state = new InMemoryDistributedState();
        await state.IncrementAsync("counter", 10);
        var result = await state.IncrementAsync("counter", -3);
        Assert.Equal(7, result);
    }

    [Scenario("Expired value returns null")]
    [Fact]
    public async Task Expired_value_returns_null()
    {
        var state = new InMemoryDistributedState();
        await state.SetAsync("key", "value", TimeSpan.FromMilliseconds(1));
        await Task.Delay(50); // Wait for expiration
        var result = await state.GetAsync<string>("key");
        Assert.Null(result);
    }

    [Scenario("Non-expired value is returned")]
    [Fact]
    public async Task Non_expired_value_is_returned()
    {
        var state = new InMemoryDistributedState();
        await state.SetAsync("key", "value", TimeSpan.FromHours(1));
        var result = await state.GetAsync<string>("key");
        Assert.Equal("value", result);
    }

    [Scenario("Complex types round trip")]
    [Fact]
    public async Task Complex_types_round_trip()
    {
        var state = new InMemoryDistributedState();
        var data = new TestData { Id = 42, Name = "Test" };
        await state.SetAsync("complex", data);
        var result = await state.GetAsync<TestData>("complex");
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Scenario("Value with no expiration persists")]
    [Fact]
    public async Task Value_without_expiration_persists()
    {
        var state = new InMemoryDistributedState();
        await state.SetAsync("key", "value");

        // Force cleanup by getting another expired key
        await state.SetAsync("expired", "x", TimeSpan.FromMilliseconds(1));
        await Task.Delay(10);
        await state.GetAsync<string>("expired"); // Triggers cleanup

        var result = await state.GetAsync<string>("key");
        Assert.Equal("value", result);
    }

    private sealed class TestData
    {
        public int Id { get; init; }
        public string? Name { get; init; }
    }
}
