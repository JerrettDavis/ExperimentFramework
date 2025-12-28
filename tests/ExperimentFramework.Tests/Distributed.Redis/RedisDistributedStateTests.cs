using System.Text.Json;
using ExperimentFramework.Distributed.Redis;
using StackExchange.Redis;
using Testcontainers.Redis;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Distributed.Redis;

[Feature("RedisDistributedState provides Redis-backed state storage")]
public sealed class RedisDistributedStateTests : TinyBddXunitBase, IAsyncLifetime
{
    private readonly RedisContainer _redis;
    private IConnectionMultiplexer? _connection;

    public RedisDistributedStateTests(ITestOutputHelper output) : base(output)
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

    [Scenario("State stores and retrieves simple values")]
    [Fact]
    public async Task Stores_and_retrieves_simple_values()
    {
        var state = new RedisDistributedState(_connection!);

        await state.SetAsync("test-key", "test-value");
        var result = await state.GetAsync<string>("test-key");

        Assert.Equal("test-value", result);
    }

    [Scenario("State stores and retrieves complex objects")]
    [Fact]
    public async Task Stores_and_retrieves_complex_objects()
    {
        var state = new RedisDistributedState(_connection!);
        var testObject = new TestData { Id = 42, Name = "Test", Active = true };

        await state.SetAsync("complex-key", testObject);
        var result = await state.GetAsync<TestData>("complex-key");

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.True(result.Active);
    }

    [Scenario("State returns default for missing keys")]
    [Fact]
    public async Task Returns_default_for_missing_keys()
    {
        var state = new RedisDistributedState(_connection!);

        var result = await state.GetAsync<string>("non-existent-key");

        Assert.Null(result);
    }

    [Scenario("State removes values")]
    [Fact]
    public async Task Removes_values()
    {
        var state = new RedisDistributedState(_connection!);
        await state.SetAsync("to-remove", "value");

        await state.RemoveAsync("to-remove");
        var result = await state.GetAsync<string>("to-remove");

        Assert.Null(result);
    }

    [Scenario("State increments counters")]
    [Fact]
    public async Task Increments_counters()
    {
        var state = new RedisDistributedState(_connection!);

        var result1 = await state.IncrementAsync("counter");
        var result2 = await state.IncrementAsync("counter");
        var result3 = await state.IncrementAsync("counter", 5);

        Assert.Equal(1, result1);
        Assert.Equal(2, result2);
        Assert.Equal(7, result3);
    }

    [Scenario("State respects expiration")]
    [Fact]
    public async Task Respects_expiration()
    {
        var state = new RedisDistributedState(_connection!);

        await state.SetAsync("expiring-key", "value", TimeSpan.FromMilliseconds(100));
        var immediate = await state.GetAsync<string>("expiring-key");

        Assert.Equal("value", immediate);

        await Task.Delay(150);
        var afterExpiry = await state.GetAsync<string>("expiring-key");

        Assert.Null(afterExpiry);
    }

    [Scenario("State uses custom key prefix")]
    [Fact]
    public async Task Uses_custom_key_prefix()
    {
        var options = new RedisDistributedStateOptions { KeyPrefix = "custom:" };
        var state = new RedisDistributedState(_connection!, options);

        await state.SetAsync("prefixed", "value");

        // Verify the key was stored with the custom prefix
        var db = _connection!.GetDatabase();
        var exists = await db.KeyExistsAsync("custom:prefixed");
        Assert.True(exists);

        var result = await state.GetAsync<string>("prefixed");
        Assert.Equal("value", result);
    }

    [Scenario("State uses custom JSON options")]
    [Fact]
    public async Task Uses_custom_json_options()
    {
        var options = new RedisDistributedStateOptions
        {
            JsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            }
        };
        var state = new RedisDistributedState(_connection!, options);
        var testObject = new TestDataWithLongName { UserId = 1, UserName = "snake", IsActive = false };

        await state.SetAsync("json-test", testObject);

        // Verify the JSON used snake_case
        var db = _connection!.GetDatabase();
        var json = await db.StringGetAsync("experiment:state:json-test");
        Assert.Contains("user_id", json.ToString()); // snake_case
        Assert.Contains("user_name", json.ToString());
        Assert.Contains("is_active", json.ToString());

        var result = await state.GetAsync<TestDataWithLongName>("json-test");
        Assert.NotNull(result);
        Assert.Equal("snake", result.UserName);
    }

    [Scenario("State handles null options")]
    [Fact]
    public async Task Handles_null_options()
    {
        var state = new RedisDistributedState(_connection!, null);

        await state.SetAsync("null-opts", "value");
        var result = await state.GetAsync<string>("null-opts");

        Assert.Equal("value", result);
    }

    [Scenario("Options have sensible defaults")]
    [Fact]
    public Task Options_have_sensible_defaults()
        => Given("default options", () => new RedisDistributedStateOptions())
            .Then("key prefix has default", o => o.KeyPrefix == "experiment:state:")
            .And("json options is null", o => o.JsonSerializerOptions == null)
            .AssertPassed();

    private sealed class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool Active { get; set; }
    }

    private sealed class TestDataWithLongName
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public bool IsActive { get; set; }
    }
}
