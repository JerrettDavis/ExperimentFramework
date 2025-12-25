using ExperimentFramework.Decorators;
using ExperimentFramework.Naming;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests;

[Feature("Decorator pipeline and naming convention edge cases")]
public sealed class DecoratorAndNamingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private static void RegisterCommonServices(IServiceCollection services)
    {
        services.AddScoped<StableService>();
        services.AddScoped<FailingService>();
        services.AddScoped<ServiceA>();
        services.AddScoped<ServiceB>();
        services.AddScoped<ITestService, StableService>();
    }

    [Scenario("Benchmark decorator factory creates decorator")]
    [Fact]
    public void BenchmarkDecorator_creates_decorator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var factory = new BenchmarkDecoratorFactory();
        var decorator = factory.Create(sp);

        Assert.NotNull(decorator);
        sp.Dispose();
    }

    [Scenario("Benchmark decorator measures execution time")]
    [Fact]
    public async Task BenchmarkDecorator_measures_time()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var factory = new BenchmarkDecoratorFactory();
        var decorator = factory.Create(sp);

        var context = new InvocationContext(
            typeof(ITestService),
            "Execute",
            "test-trial",
            []);

        var result = await decorator.InvokeAsync(context, async () =>
        {
            await Task.Delay(10);  // Simulate work
            return "result";
        });

        Assert.Equal("result", result);

        // Test passes if no exception - decorator should work
        await sp.DisposeAsync();
    }

    [Scenario("Error logging decorator factory creates decorator")]
    [Fact]
    public void ErrorLoggingDecorator_creates_decorator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var factory = new ErrorLoggingDecoratorFactory();
        var decorator = factory.Create(sp);

        Assert.NotNull(decorator);
        sp.Dispose();
    }

    [Scenario("Error logging decorator logs exceptions")]
    [Fact]
    public async Task ErrorLoggingDecorator_logs_exceptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var factory = new ErrorLoggingDecoratorFactory();
        var decorator = factory.Create(sp);

        var context = new InvocationContext(
            typeof(ITestService),
            "Execute",
            "test-trial",
            []);

        var testException = new InvalidOperationException("Test error");

        var exceptionThrown = false;
        try
        {
            await decorator.InvokeAsync(context, () => throw testException);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown);
        await sp.DisposeAsync();
    }

    [Scenario("Error logging decorator passes through successful calls")]
    [Fact]
    public async Task ErrorLoggingDecorator_passes_through_success()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var factory = new ErrorLoggingDecoratorFactory();
        var decorator = factory.Create(sp);

        var context = new InvocationContext(
            typeof(ITestService),
            "Execute",
            "test-trial",
            []);

        var result = await decorator.InvokeAsync(context, () => ValueTask.FromResult<object?>("success"));

        Assert.Equal("success", result);
        await sp.DisposeAsync();
    }

    [Scenario("Decorator pipeline executes decorators in order")]
    [Fact]
    public async Task DecoratorPipeline_executes_in_order()
    {
        var executionOrder = new List<string>();

        var decorator1 = new TestDecorator("D1", executionOrder);
        var decorator2 = new TestDecorator("D2", executionOrder);

        var factory1 = new TestDecoratorFactory(decorator1);
        var factory2 = new TestDecoratorFactory(decorator2);

        var sp = new ServiceCollection().BuildServiceProvider();
        var pipeline = new DecoratorPipeline([factory1, factory2], sp);

        var context = new InvocationContext(
            typeof(ITestService),
            "Execute",
            "test-trial",
            []);

        await pipeline.InvokeAsync(context, () => ValueTask.FromResult<object?>("final"));

        Assert.Equal(new[] { "D1-before", "D2-before", "D2-after", "D1-after" }, executionOrder);
        await sp.DisposeAsync();
    }

    [Scenario("Decorator pipeline with no decorators works")]
    [Fact]
    public async Task DecoratorPipeline_with_no_decorators()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var pipeline = new DecoratorPipeline([], sp);

        var context = new InvocationContext(
            typeof(ITestService),
            "Execute",
            "test-trial",
            []);

        var result = await pipeline.InvokeAsync(context, () => ValueTask.FromResult<object?>("result"));

        Assert.Equal("result", result);
        await sp.DisposeAsync();
    }

    [Scenario("Default naming convention generates feature flag names")]
    [Fact]
    public void DefaultNamingConvention_generates_feature_flag_names()
    {
        var convention = new DefaultExperimentNamingConvention();

        var name = convention.FeatureFlagNameFor(typeof(ITestService));

        // Default convention uses type name as-is (including "I" prefix)
        Assert.Equal("ITestService", name);
    }

    [Scenario("Default naming convention generates variant flag names")]
    [Fact]
    public void DefaultNamingConvention_generates_variant_names()
    {
        var convention = new DefaultExperimentNamingConvention();

        var name = convention.VariantFlagNameFor(typeof(ITestService));

        // Default convention uses type name as-is (including "I" prefix)
        Assert.Equal("ITestService", name);
    }

    [Scenario("Default naming convention generates config keys")]
    [Fact]
    public void DefaultNamingConvention_generates_config_keys()
    {
        var convention = new DefaultExperimentNamingConvention();

        var name = convention.ConfigurationKeyFor(typeof(ITestService));

        // Default convention includes type name with "I" prefix in config key
        Assert.Equal("Experiments:ITestService", name);
    }

    [Scenario("Default naming convention preserves type name")]
    [Fact]
    public void DefaultNamingConvention_preserves_type_name()
    {
        var convention = new DefaultExperimentNamingConvention();

        var name1 = convention.FeatureFlagNameFor(typeof(ITestService));
        var name2 = convention.FeatureFlagNameFor(typeof(IDatabase));

        // Default convention uses type name as-is
        Assert.Equal("ITestService", name1);
        Assert.Equal("IDatabase", name2);
    }

    [Scenario("Default naming convention handles generic types")]
    [Fact]
    public void DefaultNamingConvention_handles_generics()
    {
        var convention = new DefaultExperimentNamingConvention();

        // Default convention uses full generic type name
        var name = convention.FeatureFlagNameFor(typeof(IGenericRepository<TestEntity>));

        Assert.Equal("IGenericRepository`1", name);
    }

    [Scenario("Custom naming convention can be used")]
    [Fact]
    public void CustomNamingConvention_can_be_set()
    {
        var builder = ExperimentFrameworkBuilder.Create();
        var convention = new CustomTestNamingConvention();

        builder.UseNamingConvention(convention);

        // Test passes if no exception - naming convention API works
        Assert.NotNull(builder);
    }

    [Scenario("InvocationContext stores all properties correctly")]
    [Fact]
    public void InvocationContext_stores_properties()
    {
        var args = new object?[] { "arg1", 42 };
        var context = new InvocationContext(
            typeof(ITestService),
            "Execute",
            "trial-key",
            args);

        Assert.Equal(typeof(ITestService), context.ServiceType);
        Assert.Equal("Execute", context.MethodName);
        Assert.Equal("trial-key", context.TrialKey);
        Assert.Equal(args, context.Arguments);
    }

    [Scenario("ExperimentSelectorName formats correctly")]
    [Fact]
    public void ExperimentSelectorName_formats_correctly()
    {
        var selector = new ExperimentSelectorName("TestSelector");

        var formatted = selector.ToString();

        Assert.Equal("TestSelector", formatted);
    }

    [Scenario("AddLogger adds both benchmark and error logging")]
    [Fact]
    public Task AddLogger_adds_decorators()
        => Given("builder with logger configuration", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:Test"] = "false"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();
            services.AddLogging();
            RegisterCommonServices(services);

            var builder = ExperimentFrameworkBuilder.Create();
            builder.AddLogger(l => l.AddBenchmarks().AddErrorLogging());
            builder.Define<ITestService>(c => c
                .UsingFeatureFlag("Test")
                .AddDefaultTrial<ServiceA>("false")
                .AddTrial<ServiceB>("true"));

            services.AddExperimentFramework(builder);
            return services.BuildServiceProvider();
        })
        .When("invoke service", sp =>
        {
            using var scope = sp.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestService>();
            return (sp, service.Execute());
        })
        .Then("works with decorators", r => r.Item2 == "ServiceA")
        .Finally(r => r.sp.Dispose())
        .AssertPassed();
}

// Helper classes for testing
internal class TestDecorator(string name, List<string> log) : IExperimentDecorator
{
    public async ValueTask<object?> InvokeAsync(InvocationContext context, Func<ValueTask<object?>> next)
    {
        log.Add($"{name}-before");
        var result = await next();
        log.Add($"{name}-after");
        return result;
    }
}

internal class TestDecoratorFactory(IExperimentDecorator decorator) : IExperimentDecoratorFactory
{
    public IExperimentDecorator Create(IServiceProvider serviceProvider) => decorator;
}

internal class CustomTestNamingConvention : IExperimentNamingConvention
{
    public string FeatureFlagNameFor(Type serviceType) => $"CustomFeatures:{serviceType.Name}";
    public string VariantFlagNameFor(Type serviceType) => $"CustomVariants:{serviceType.Name}";
    public string ConfigurationKeyFor(Type serviceType) => $"CustomConfig:{serviceType.Name}";
    public string OpenFeatureFlagNameFor(Type serviceType) => $"custom-{serviceType.Name.ToLowerInvariant()}";
}
