using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests;

[Feature("Fluent API (.UseSourceGenerators()) triggers source generation without attribute")]
public sealed class FluentApiTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestState(ServiceProvider ServiceProvider);
    private sealed record ExecutionResult(TestState State, string Result);

    private static TestState SetupWithFluentApi(bool useFluentV2)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseFluentV2"] = useFluentV2.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        // Register concrete implementations
        services.AddScoped<FluentServiceV1>();
        services.AddScoped<FluentServiceV2>();

        // Register default interface
        services.AddScoped<IFluentService, FluentServiceV1>();

        // Configure experiments using fluent API (NO attribute)
        var experiments = FluentApiCompositionRoot.ConfigureFluentApiExperiments();
        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider());
    }

    private static ExecutionResult InvokeService(TestState state)
    {
        using var scope = state.ServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IFluentService>();
        var result = service.Execute();
        return new ExecutionResult(state, result);
    }

    [Scenario("Fluent API with .UseSourceGenerators() generates proxies without attribute")]
    [Fact]
    public Task Fluent_api_generates_proxies_without_attribute()
        => Given("fluent API configuration with UseFluentV2 = false", () => SetupWithFluentApi(useFluentV2: false))
            .When("invoke service", InvokeService)
            .Then("uses default implementation FluentServiceV1", r => r.Result == "FluentServiceV1")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Fluent API routes to alternate trial when feature enabled")]
    [Fact]
    public Task Fluent_api_routes_to_trial_when_enabled()
        => Given("fluent API configuration with UseFluentV2 = true", () => SetupWithFluentApi(useFluentV2: true))
            .When("invoke service", InvokeService)
            .Then("uses trial implementation FluentServiceV2", r => r.Result == "FluentServiceV2")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Fluent API with multiple experiments")]
    [Fact]
    public Task Fluent_api_supports_multiple_experiments()
        => Given("fluent API configuration with multiple services", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseFluentV2"] = "true"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            // Register implementations
            services.AddScoped<FluentServiceV1>();
            services.AddScoped<FluentServiceV2>();

            // Register default
            services.AddScoped<IFluentService, FluentServiceV1>();

            // Configure experiment using fluent API
            var experiments = FluentApiCompositionRoot.ConfigureFluentApiExperiments();
            services.AddExperimentFramework(experiments);

            return services.BuildServiceProvider();
        })
        .When("invoke service", sp =>
        {
            using var scope = sp.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IFluentService>();
            return (sp, result: service.Execute());
        })
        .Then("experiment routes correctly", r => r.result == "FluentServiceV2")
        .Finally(r => r.sp.Dispose())
        .AssertPassed();
}
