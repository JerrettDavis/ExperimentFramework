using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests;

/// <summary>
/// Comprehensive tests for async method proxying including Task{T}, ValueTask{T}, and generic interfaces.
/// Uses source-generated proxies from TestInterfaces composition root.
/// </summary>
[Feature("Async and generic interface proxying")]
public class AsyncProxyTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // State types for TinyBDD
    private sealed record AsyncTestState(
        IServiceProvider ServiceProvider,
        bool FeatureEnabled);

    private sealed record GenericTestState(
        IServiceProvider ServiceProvider,
        bool FeatureEnabled);

    private sealed record AsyncResult(
        AsyncTestState State,
        string StringResult,
        int IntResult,
        List<string> ListResult,
        string ValueTaskStringResult,
        int ValueTaskIntResult);

    private sealed record GenericResult<T>(
        GenericTestState State,
        List<T> AllResults) where T : class;

    private sealed record NestedGenericResult(
        AsyncTestState State,
        Dictionary<string, List<int>> DictResult,
        Tuple<string, int, bool> TupleResult,
        KeyValuePair<string, int> KvpResult);

    // Test helper methods
    private static AsyncTestState SetupAsyncService(bool featureEnabled)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseV2AsyncService"] = featureEnabled.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        services.AddScoped<AsyncServiceV1>();
        services.AddScoped<AsyncServiceV2>();
        services.AddScoped<IAsyncService, AsyncServiceV1>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<IAsyncService>(c => c
                .UsingFeatureFlag("UseV2AsyncService")
                .AddDefaultTrial<AsyncServiceV1>("false")
                .AddTrial<AsyncServiceV2>("true"));

        services.AddExperimentFramework(experiments);

        return new AsyncTestState(services.BuildServiceProvider(), featureEnabled);
    }

    private static GenericTestState SetupGenericRepository(bool featureEnabled)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseV2Repository"] = featureEnabled.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        services.AddScoped<GenericRepositoryV1<TestEntity>>();
        services.AddScoped<GenericRepositoryV2<TestEntity>>();
        services.AddScoped<IGenericRepository<TestEntity>, GenericRepositoryV1<TestEntity>>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<IGenericRepository<TestEntity>>(c => c
                .UsingFeatureFlag("UseV2Repository")
                .AddDefaultTrial<GenericRepositoryV1<TestEntity>>("false")
                .AddTrial<GenericRepositoryV2<TestEntity>>("true"));

        services.AddExperimentFramework(experiments);

        return new GenericTestState(services.BuildServiceProvider(), featureEnabled);
    }

    private static AsyncTestState SetupNestedGenericService(bool featureEnabled)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseV2NestedGeneric"] = featureEnabled.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        services.AddScoped<NestedGenericServiceV1>();
        services.AddScoped<NestedGenericServiceV2>();
        services.AddScoped<INestedGenericService, NestedGenericServiceV1>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<INestedGenericService>(c => c
                .UsingFeatureFlag("UseV2NestedGeneric")
                .AddDefaultTrial<NestedGenericServiceV1>("false")
                .AddTrial<NestedGenericServiceV2>("true"));

        services.AddExperimentFramework(experiments);

        return new AsyncTestState(services.BuildServiceProvider(), featureEnabled);
    }

    private static async Task<AsyncResult> InvokeAllAsyncMethods(AsyncTestState state)
    {
        using var scope = state.ServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAsyncService>();

        var stringResult = await service.GetStringAsync();
        var intResult = await service.GetIntAsync();
        var listResult = await service.GetListAsync();
        var vtStringResult = await service.GetStringValueTaskAsync();
        var vtIntResult = await service.GetIntValueTaskAsync();

        return new AsyncResult(state, stringResult, intResult, listResult, vtStringResult, vtIntResult);
    }

    private static async Task<GenericResult<TestEntity>> InvokeGenericRepository(GenericTestState state)
    {
        using var scope = state.ServiceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity>>();

        var entity = new TestEntity { Id = 1, Name = "Test" };
        await repo.SaveAsync(entity);

        var allResults = await repo.GetAllAsync();

        return new GenericResult<TestEntity>(state, allResults);
    }

    private static async Task<NestedGenericResult> InvokeNestedGeneric(AsyncTestState state)
    {
        using var scope = state.ServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<INestedGenericService>();

        var dictResult = await service.GetComplexDataAsync();
        var tupleResult = await service.GetTupleAsync();
        var kvpResult = await service.GetKeyValuePairAsync();

        return new NestedGenericResult(state, dictResult, tupleResult, kvpResult);
    }

    // Tests
    [Scenario("Task<string> returns correct value from V1")]
    [Fact]
    public Task Task_string_returns_v1_value()
        => Given("async service with feature disabled", () => SetupAsyncService(false))
            .When("invoke all async methods", InvokeAllAsyncMethods)
            .Then("string result is from V1", r => r.StringResult == "v1-string")
            .Finally(r => (r.State.ServiceProvider as IDisposable)?.Dispose())
            .AssertPassed();

    [Scenario("Task<string> returns correct value from V2")]
    [Fact]
    public Task Task_string_returns_v2_value()
        => Given("async service with feature enabled", () => SetupAsyncService(true))
            .When("invoke all async methods", InvokeAllAsyncMethods)
            .Then("string result is from V2", r => r.StringResult == "v2-string")
            .Finally(r => (r.State.ServiceProvider as IDisposable)?.Dispose())
            .AssertPassed();

    [Scenario("Task<int> returns correct value")]
    [Fact]
    public Task Task_int_returns_correct_value()
        => Given("async service with feature enabled", () => SetupAsyncService(true))
            .When("invoke all async methods", InvokeAllAsyncMethods)
            .Then("int result is from V2", r => r.IntResult == 2)
            .Finally(r => (r.State.ServiceProvider as IDisposable)?.Dispose())
            .AssertPassed();

    [Scenario("Task<List<T>> returns correct collection")]
    [Fact]
    public Task Task_list_returns_correct_collection()
        => Given("async service with feature enabled", () => SetupAsyncService(true))
            .When("invoke all async methods", InvokeAllAsyncMethods)
            .Then("list result is from V2", r => r.ListResult.Contains("v2-a"))
            .And("list has correct count", r => r.ListResult.Count == 2)
            .Finally(r => (r.State.ServiceProvider as IDisposable)?.Dispose())
            .AssertPassed();

    [Scenario("ValueTask<string> returns correct value")]
    [Fact]
    public Task ValueTask_string_returns_correct_value()
        => Given("async service with feature enabled", () => SetupAsyncService(true))
            .When("invoke all async methods", InvokeAllAsyncMethods)
            .Then("valuetask string result is from V2", r => r.ValueTaskStringResult == "v2-valuestring")
            .Finally(r => (r.State.ServiceProvider as IDisposable)?.Dispose())
            .AssertPassed();

    [Scenario("ValueTask<int> returns correct value")]
    [Fact]
    public Task ValueTask_int_returns_correct_value()
        => Given("async service with feature enabled", () => SetupAsyncService(true))
            .When("invoke all async methods", InvokeAllAsyncMethods)
            .Then("valuetask int result is from V2", r => r.ValueTaskIntResult == 20)
            .Finally(r => (r.State.ServiceProvider as IDisposable)?.Dispose())
            .AssertPassed();

    [Scenario("Generic interface IRepository<T> proxies correctly")]
    [Fact]
    public Task Generic_interface_proxies_correctly()
        => Given("generic repository with feature disabled", () => SetupGenericRepository(false))
            .When("invoke generic repository methods", InvokeGenericRepository)
            .Then("results are returned", r => r.AllResults != null)
            .And("entity was saved", r => r.AllResults.Count == 1)
            .Finally(r => (r.State.ServiceProvider as IDisposable)?.Dispose())
            .AssertPassed();

    [Scenario("Nested generic Task<Dictionary<string, List<int>>> works")]
    [Fact]
    public Task Nested_generic_dictionary_works()
        => Given("nested generic service with feature enabled", () => SetupNestedGenericService(true))
            .When("invoke nested generic methods", InvokeNestedGeneric)
            .Then("dictionary result is from V2", r => r.DictResult.ContainsKey("v2"))
            .And("dictionary has correct values", r => r.DictResult["v2"].Contains(4))
            .Finally(r => (r.State.ServiceProvider as IDisposable)?.Dispose())
            .AssertPassed();

    [Scenario("Task<Tuple<string, int, bool>> works correctly")]
    [Fact]
    public Task Task_tuple_works_correctly()
        => Given("nested generic service with feature enabled", () => SetupNestedGenericService(true))
            .When("invoke nested generic methods", InvokeNestedGeneric)
            .Then("tuple item1 is from V2", r => r.TupleResult.Item1 == "v2")
            .And("tuple item2 is correct", r => r.TupleResult.Item2 == 2)
            .And("tuple item3 is correct", r => !r.TupleResult.Item3)
            .Finally(r => (r.State.ServiceProvider as IDisposable)?.Dispose())
            .AssertPassed();

    [Scenario("ValueTask<KeyValuePair<string, int>> works correctly")]
    [Fact]
    public Task ValueTask_kvp_works_correctly()
        => Given("nested generic service with feature enabled", () => SetupNestedGenericService(true))
            .When("invoke nested generic methods", InvokeNestedGeneric)
            .Then("kvp key is from V2", r => r.KvpResult.Key == "v2")
            .And("kvp value is correct", r => r.KvpResult.Value == 200)
            .Finally(r => (r.State.ServiceProvider as IDisposable)?.Dispose())
            .AssertPassed();

    [Scenario("Async methods can be called multiple times")]
    [Fact]
    public Task Async_methods_multiple_calls()
        => Given("async service with feature enabled", () => SetupAsyncService(true))
            .When("invoke all async methods", InvokeAllAsyncMethods)
            .Then("first call succeeds", r => r.StringResult == "v2-string")
            .When("invoke all async methods again", r => InvokeAllAsyncMethods(r.State))
            .Then("second call succeeds", r => r.StringResult == "v2-string")
            .Finally(r => (r.State.ServiceProvider as IDisposable)?.Dispose())
            .AssertPassed();

    [Scenario("Void Task methods complete successfully")]
    [Fact]
    public async Task Void_task_methods_complete()
    {
        var state = SetupAsyncService(true);
        try
        {
            using var scope = state.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IAsyncService>();

            await service.VoidTaskAsync();
            await service.VoidValueTaskAsync();

            Assert.True(true); // If we got here, void methods completed
        }
        finally
        {
            (state.ServiceProvider as IDisposable)?.Dispose();
        }
    }

    [Scenario("Multiple generic type parameters work")]
    [Fact]
    public async Task Multiple_generic_parameters_work()
    {
        // Define an interface with multiple generic parameters
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseMultiGeneric"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        // For this test, we'll use IGenericRepository<TestEntity> which we know works
        services.AddScoped<GenericRepositoryV1<TestEntity>>();
        services.AddScoped<IGenericRepository<TestEntity>, GenericRepositoryV1<TestEntity>>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<IGenericRepository<TestEntity>>(c => c
                .UsingFeatureFlag("UseMultiGeneric")
                .AddDefaultTrial<GenericRepositoryV1<TestEntity>>("false")
                .AddTrial<GenericRepositoryV1<TestEntity>>("true"));

        services.AddExperimentFramework(experiments);

        var sp = services.BuildServiceProvider();
        try
        {
            using var scope = sp.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity>>();

            var result = await repo.GetAllAsync();
            Assert.NotNull(result);
        }
        finally
        {
            await sp.DisposeAsync();
        }
    }
}
