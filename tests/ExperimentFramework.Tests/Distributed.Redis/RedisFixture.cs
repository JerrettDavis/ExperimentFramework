using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace ExperimentFramework.Tests.Distributed.Redis;

public sealed class RedisFixture : IAsyncLifetime
{
    public RedisContainer Container { get; }
    public string ConnectionString => Container.GetConnectionString();
    public IConnectionMultiplexer Connection { get; private set; } = null!;

    public RedisFixture()
    {
        Container = new RedisBuilder("redis:7-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        Connection = await ConnectionMultiplexer.ConnectAsync(Container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        if (Connection != null)
        {
            await Connection.CloseAsync();
            Connection.Dispose();
        }
        await Container.DisposeAsync();
    }
}

[CollectionDefinition("Redis")]
public sealed class RedisCollection : ICollectionFixture<RedisFixture> { }
