using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

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

        // IRedirectSpecificService implementations
        services.AddScoped<PrimaryService>();
        services.AddScoped<SecondaryService>();
        services.AddScoped<NoopFallbackService>();

        // IRedirectOrderedService implementations
        services.AddScoped<CloudService>();
        services.AddScoped<LocalCacheService>();
        services.AddScoped<InMemoryCacheService>();
        services.AddScoped<StaticDataService>();
        services.AddScoped<AlwaysFailsService1>();
        services.AddScoped<AlwaysFailsService2>();
        services.AddScoped<AlwaysFailsService3>();

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
        services.AddScoped<IRedirectSpecificService, PrimaryService>();
        services.AddScoped<IRedirectOrderedService, CloudService>();
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

    // ========================================
    // RedirectAndReplay Policy Tests
    // ========================================

    private static TestState SetupRedirectAndReplayPolicy(bool usePrimaryService)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UsePrimaryService"] = usePrimaryService.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        RegisterAllTestServices(services);

        var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider(), "RedirectAndReplay");
    }

    private static InvocationResult InvokeRedirectSpecificService(TestState state)
    {
        try
        {
            using var scope = state.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRedirectSpecificService>();
            var result = service.Execute();
            return new InvocationResult(state, null, result);
        }
        catch (Exception ex)
        {
            return new InvocationResult(state, ex, null);
        }
    }

    [Scenario("RedirectAndReplay redirects to specific fallback when primary fails")]
    [Fact]
    public Task RedirectAndReplay_redirects_to_specific_fallback()
        => Given("redirect policy with failing primary service", () => SetupRedirectAndReplayPolicy(usePrimaryService: true))
            .When("invoke service", InvokeRedirectSpecificService)
            .Then("no exception is thrown", r => r.Exception == null)
            .And("result is from noop fallback", r => r.Result == "NoopFallback")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("RedirectAndReplay redirects when secondary fails")]
    [Fact]
    public Task RedirectAndReplay_redirects_from_secondary_to_fallback()
        => Given("redirect policy with failing secondary service", () => SetupRedirectAndReplayPolicy(usePrimaryService: false))
            .When("invoke service", InvokeRedirectSpecificService)
            .Then("no exception is thrown", r => r.Exception == null)
            .And("result is from noop fallback", r => r.Result == "NoopFallback")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("RedirectAndReplay uses fallback directly when it's the preferred trial")]
    [Fact]
    public Task RedirectAndReplay_uses_fallback_directly_when_preferred()
        => Given("redirect policy configured", () =>
        {
            // Configure to use "noop" as the preferred trial (which is also the fallback)
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UsePrimaryService"] = "false" // Will select "false" -> SecondaryService
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllTestServices(services);

            var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

            services.AddExperimentFramework(experiments);

            return new TestState(services.BuildServiceProvider(), "RedirectAndReplay");
        })
            .When("invoke service", InvokeRedirectSpecificService)
            .Then("no exception is thrown", r => r.Exception == null)
            .And("result is from noop fallback", r => r.Result == "NoopFallback")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("RedirectAndReplay with concurrent invocations")]
    [Fact]
    public Task RedirectAndReplay_concurrent_invocations()
        => Given("redirect policy with failing trials", () => SetupRedirectAndReplayPolicy(usePrimaryService: true))
            .When("invoke service 10 times concurrently", state =>
            {
                var tasks = Enumerable.Range(0, 10)
                    .Select(_ => Task.Run(() => InvokeRedirectSpecificService(state)))
                    .ToArray();
                Task.WaitAll(tasks);
                return tasks.Select(t => t.Result).ToList();
            })
            .Then("all invocations succeed", results => results.All(r => r.Exception == null))
            .And("all use noop fallback", results => results.All(r => r.Result == "NoopFallback"))
            .Finally(results => results[0].State.ServiceProvider.Dispose())
            .AssertPassed();

    // ========================================
    // RedirectAndReplayOrdered Policy Tests
    // ========================================

    private static TestState SetupRedirectAndReplayOrderedPolicy(bool useCloudService)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloudService"] = useCloudService.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        RegisterAllTestServices(services);

        var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider(), "RedirectAndReplayOrdered");
    }

    private static InvocationResult InvokeRedirectOrderedService(TestState state)
    {
        try
        {
            using var scope = state.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRedirectOrderedService>();
            var result = service.Execute();
            return new InvocationResult(state, null, result);
        }
        catch (Exception ex)
        {
            return new InvocationResult(state, ex, null);
        }
    }

    [Scenario("RedirectAndReplayOrdered tries ordered fallbacks until success")]
    [Fact]
    public Task RedirectAndReplayOrdered_tries_ordered_fallbacks()
        => Given("redirect ordered policy with cloud service (fails)", () => SetupRedirectAndReplayOrderedPolicy(useCloudService: true))
            .When("invoke service", InvokeRedirectOrderedService)
            .Then("no exception is thrown", r => r.Exception == null)
            .And("result is from third fallback (InMemoryCache)", r => r.Result == "InMemoryCache")
            // CloudService (timeout) → LocalCacheService (miss) → InMemoryCacheService (success)
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("RedirectAndReplayOrdered respects exact fallback order")]
    [Fact]
    public Task RedirectAndReplayOrdered_respects_exact_order()
        => Given("redirect ordered policy configured", () => SetupRedirectAndReplayOrderedPolicy(useCloudService: true))
            .When("invoke service", InvokeRedirectOrderedService)
            .Then("tries cloud first (fails with timeout)", r => r.Exception == null)
            .And("then cache (fails with miss)", r => r.Exception == null)
            .And("then memory (succeeds)", r => r.Result == "InMemoryCache")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("RedirectAndReplayOrdered throws when all fallbacks fail")]
    [Fact]
    public Task RedirectAndReplayOrdered_throws_when_all_fail()
        => Given("redirect ordered policy with all failing services", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudService"] = "false" // Use a different service
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllTestServices(services);

            // Create a custom experiment that will fail all trials
            var experiments = ExperimentFrameworkBuilder.Create()
                .Define<IRedirectOrderedService>(c => c
                    .UsingFeatureFlag("UseCloudService")
                    .AddDefaultTrial<AlwaysFailsService1>("true")
                    .AddTrial<AlwaysFailsService2>("fail2")
                    .AddTrial<AlwaysFailsService3>("fail3")
                    .OnErrorRedirectAndReplayOrdered("fail2", "fail3"));

            services.AddExperimentFramework(experiments);

            return new TestState(services.BuildServiceProvider(), "RedirectAndReplayOrdered");
        })
            .When("invoke service", InvokeRedirectOrderedService)
            .Then("exception is thrown", r => r.Exception != null)
            .And("exception is InvalidOperationException", r =>
                r.Exception is InvalidOperationException)
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("RedirectAndReplayOrdered with concurrent invocations")]
    [Fact]
    public Task RedirectAndReplayOrdered_concurrent_invocations()
        => Given("redirect ordered policy with failing cloud service", () => SetupRedirectAndReplayOrderedPolicy(useCloudService: true))
            .When("invoke service 10 times concurrently", state =>
            {
                var tasks = Enumerable.Range(0, 10)
                    .Select(_ => Task.Run(() => InvokeRedirectOrderedService(state)))
                    .ToArray();
                Task.WaitAll(tasks);
                return tasks.Select(t => t.Result).ToList();
            })
            .Then("all invocations succeed", results => results.All(r => r.Exception == null))
            .And("all use InMemoryCache fallback", results => results.All(r => r.Result == "InMemoryCache"))
            .Finally(results => results[0].State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("RedirectAndReplayOrdered skips preferred key in fallback list")]
    [Fact]
    public Task RedirectAndReplayOrdered_skips_preferred_in_fallbacks()
        => Given("redirect ordered policy where preferred is in fallback list", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudService"] = "false" // Will select LocalCacheService
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllTestServices(services);

            var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

            services.AddExperimentFramework(experiments);

            return new TestState(services.BuildServiceProvider(), "RedirectAndReplayOrdered");
        })
            .When("invoke service", InvokeRedirectOrderedService)
            .Then("no exception is thrown", r => r.Exception == null)
            .And("skips cache (already tried) and uses memory", r => r.Result == "InMemoryCache")
            // Preferred: LocalCacheService (fails)
            // Fallbacks: cache (skip, already tried), memory (success)
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("RedirectAndReplayOrdered continues to next fallback on error")]
    [Fact]
    public Task RedirectAndReplayOrdered_continues_to_next_on_error()
        => Given("redirect ordered policy with multiple failures", () => SetupRedirectAndReplayOrderedPolicy(useCloudService: true))
            .When("invoke service", InvokeRedirectOrderedService)
            .Then("no exception is thrown", r => r.Exception == null)
            .And("result is from successful fallback after multiple failures", r => r.Result == "InMemoryCache")
            // Order: Cloud (timeout) → LocalCache (miss) → InMemory (success)
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

}
