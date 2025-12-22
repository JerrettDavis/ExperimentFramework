using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;
using ExperimentFramework.Tests.TestInterfaces;

namespace ExperimentFramework.Tests;

[Feature("Error policies control fallback behavior when trials fail")]
public sealed class ErrorPolicyTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestState(
        ServiceProvider ServiceProvider,
        string PolicyType);

    private sealed record InvocationResult(
        TestState State,
        Exception? Exception,
        string? Result);

    // Helper to register all test services required by composition root
    private static void RegisterAllTestServices(IServiceCollection services)
    {
        // ITestService implementations
        services.AddScoped<StableService>();
        services.AddScoped<FailingService>();
        services.AddScoped<UnstableService>();
        services.AddScoped<AlsoFailingService>();
        services.AddScoped<ServiceA>();
        services.AddScoped<ServiceB>();

        // IDatabase implementations
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<ControlDatabase>();
        services.AddScoped<ExperimentalDatabase>();

        // ITaxProvider implementations
        services.AddScoped<DefaultTaxProvider>();
        services.AddScoped<OkTaxProvider>();
        services.AddScoped<TxTaxProvider>();

        // IVariantService implementations
        services.AddScoped<ControlVariant>();
        services.AddScoped<ControlImpl>();
        services.AddScoped<VariantA>();
        services.AddScoped<VariantB>();

        // IMyService implementations
        services.AddScoped<MyServiceV1>();
        services.AddScoped<MyServiceV2>();

        // IOtherService implementations
        services.AddScoped<ServiceC>();
        services.AddScoped<ServiceD>();

        // IVariantTestService implementations
        services.AddScoped<ControlService>();
        services.AddScoped<VariantAService>();
        services.AddScoped<VariantBService>();

        // IAsyncService implementations
        services.AddScoped<AsyncServiceV1>();
        services.AddScoped<AsyncServiceV2>();

        // IGenericRepository implementations
        services.AddScoped<GenericRepositoryV1<TestEntity>>();
        services.AddScoped<GenericRepositoryV2<TestEntity>>();

        // INestedGenericService implementations
        services.AddScoped<NestedGenericServiceV1>();
        services.AddScoped<NestedGenericServiceV2>();

        // Default registrations
        services.AddScoped<ITestService, StableService>();
        services.AddScoped<IDatabase, LocalDatabase>();
        services.AddScoped<ITaxProvider, DefaultTaxProvider>();
        services.AddScoped<IVariantService, ControlVariant>();
        services.AddScoped<IMyService, MyServiceV1>();
        services.AddScoped<IOtherService, ServiceC>();
        services.AddScoped<IVariantTestService, ControlService>();
        services.AddScoped<IAsyncService, AsyncServiceV1>();
        services.AddScoped<IGenericRepository<TestEntity>, GenericRepositoryV1<TestEntity>>();
        services.AddScoped<INestedGenericService, NestedGenericServiceV1>();
    }

    private static TestState SetupThrowPolicy(bool useFailingTrial)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseFailingService"] = useFailingTrial.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        RegisterAllTestServices(services);

        var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider(), "Throw");
    }

    private static TestState SetupRedirectAndReplayDefaultPolicy(bool useFailingTrial)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseFailingService"] = useFailingTrial.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        RegisterAllTestServices(services);

        var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider(), "RedirectAndReplayDefault");
    }

    private static TestState SetupRedirectAndReplayAnyPolicy()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseFailingService"] = "true" // Use failing service
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        RegisterAllTestServices(services);

        var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider(), "RedirectAndReplayDefault");
    }

    private static InvocationResult InvokeService(TestState state)
    {
        try
        {
            using var scope = state.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestService>();
            var result = service.Execute();
            return new InvocationResult(state, null, result);
        }
        catch (Exception ex)
        {
            return new InvocationResult(state, ex, null);
        }
    }

    [Scenario("Redirect policy falls back when trial fails")]
    [Fact]
    public Task Redirect_policy_falls_back_on_failure()
        => Given("redirect policy with failing trial", () => SetupThrowPolicy(useFailingTrial: true))
            .When("invoke service", InvokeService)
            .Then("no exception is thrown", r => r.Exception == null)
            .And("result is from default stable service", r => r.Result == "StableService")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Throw policy succeeds when trial works")]
    [Fact]
    public Task Throw_policy_succeeds_when_trial_works()
        => Given("throw policy with stable trial", () => SetupThrowPolicy(useFailingTrial: false))
            .When("invoke service", InvokeService)
            .Then("no exception is thrown", r => r.Exception == null)
            .And("result is from stable service", r => r.Result == "StableService")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("RedirectAndReplayDefault falls back to default when trial fails")]
    [Fact]
    public Task RedirectAndReplayDefault_falls_back_to_default()
        => Given("redirect policy with failing trial", () => SetupRedirectAndReplayDefaultPolicy(useFailingTrial: true))
            .When("invoke service", InvokeService)
            .Then("no exception is thrown", r => r.Exception == null)
            .And("result is from default stable service", r => r.Result == "StableService")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("RedirectAndReplayDefault uses trial when it succeeds")]
    [Fact]
    public Task RedirectAndReplayDefault_uses_trial_when_succeeds()
        => Given("redirect policy with stable trial", () => SetupRedirectAndReplayDefaultPolicy(useFailingTrial: false))
            .When("invoke service", InvokeService)
            .Then("no exception is thrown", r => r.Exception == null)
            .And("result is from stable service", r => r.Result == "StableService")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("RedirectAndReplayDefault redirects to default on failure")]
    [Fact]
    public Task RedirectAndReplayDefault_redirects_to_default()
        => Given("redirect policy with failing trial selected", SetupRedirectAndReplayAnyPolicy)
            .When("invoke service", InvokeService)
            .Then("no exception is thrown", r => r.Exception == null)
            .And("result is from default stable service", r => r.Result == "StableService")
            // FailingService fails, falls back to StableService (default)
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Multiple concurrent invocations respect error policies")]
    [Fact]
    public Task Concurrent_invocations_respect_policies()
        => Given("redirect policy with failing trial", () => SetupRedirectAndReplayDefaultPolicy(useFailingTrial: true))
            .When("invoke service 5 times concurrently", state =>
            {
                var tasks = Enumerable.Range(0, 5)
                    .Select(_ => Task.Run(() => InvokeService(state)))
                    .ToArray();
                Task.WaitAll(tasks);
                return tasks.Select(t => t.Result).ToList();
            })
            .Then("all invocations succeed", results => results.All(r => r.Exception == null))
            .And("all use fallback service", results => results.All(r => r.Result == "StableService"))
            .Finally(results => results[0].State.ServiceProvider.Dispose())
            .AssertPassed();

}
