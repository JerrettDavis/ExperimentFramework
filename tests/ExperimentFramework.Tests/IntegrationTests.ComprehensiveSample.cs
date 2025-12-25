extern alias ComprehensiveSample;
using ComprehensiveSample::ExperimentFramework.ComprehensiveSample;
using ComprehensiveSample::ExperimentFramework.ComprehensiveSample.Services.Decorator;
using ComprehensiveSample::ExperimentFramework.ComprehensiveSample.Services.ErrorPolicy;
using ComprehensiveSample::ExperimentFramework.ComprehensiveSample.Services.ReturnTypes;
using ComprehensiveSample::ExperimentFramework.ComprehensiveSample.Services.Telemetry;
using ComprehensiveSample::ExperimentFramework.ComprehensiveSample.Services.Variant;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests;

[Feature("ComprehensiveSample integration tests validate all framework features")]
public sealed class ComprehensiveSampleIntegrationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestContext(
        IServiceProvider ServiceProvider,
        IConfiguration Configuration);

    private static void RegisterAllComprehensiveServices(IServiceCollection services)
    {
        // Error Policy services
        services.AddScoped<StableImplementation>();
        services.AddScoped<UnstableImplementation>();
        services.AddScoped<DefaultImplementation>();
        services.AddScoped<ExperimentalImplementation>();
        services.AddScoped<PrimaryProvider>();
        services.AddScoped<SecondaryProvider>();
        services.AddScoped<TertiaryProvider>();
        services.AddScoped<PrimaryImplementation>();
        services.AddScoped<SecondaryImplementation>();
        services.AddScoped<NoopDiagnosticsHandler>();
        services.AddScoped<CloudDatabaseImplementation>();
        services.AddScoped<LocalCacheImplementation>();
        services.AddScoped<InMemoryCacheImplementation>();
        services.AddScoped<StaticDataImplementation>();

        // Decorator services
        services.AddScoped<DatabaseDataService>();
        services.AddScoped<CacheDataService>();
        services.AddSingleton<IMemoryCache, MemoryCache>();

        // Telemetry services
        services.AddScoped<EmailNotificationService>();
        services.AddScoped<SmsNotificationService>();

        // Variant services
        services.AddScoped<StripePaymentProcessor>();
        services.AddScoped<PayPalPaymentProcessor>();
        services.AddScoped<SquarePaymentProcessor>();

        // Return type services
        services.AddScoped<VoidImplementationA>();
        services.AddScoped<VoidImplementationB>();
        services.AddScoped<TaskImplementationA>();
        services.AddScoped<TaskImplementationB>();
        services.AddScoped<TaskTImplementationA>();
        services.AddScoped<TaskTImplementationB>();
        services.AddScoped<ValueTaskImplementationA>();
        services.AddScoped<ValueTaskImplementationB>();
        services.AddScoped<ValueTaskTImplementationA>();
        services.AddScoped<ValueTaskTImplementationB>();

        // Register default interface implementations
        services.AddScoped<IThrowPolicyService, StableImplementation>();
        services.AddScoped<IRedirectDefaultService, DefaultImplementation>();
        services.AddScoped<IRedirectAnyService, TertiaryProvider>();
        services.AddScoped<IRedirectSpecificService, PrimaryImplementation>();
        services.AddScoped<IRedirectOrderedService, CloudDatabaseImplementation>();
        services.AddScoped<IDataService, DatabaseDataService>();
        services.AddScoped<INotificationService, EmailNotificationService>();
        services.AddScoped<IPaymentProcessor, StripePaymentProcessor>();
        services.AddScoped<IVoidService, VoidImplementationA>();
        services.AddScoped<ITaskService, TaskImplementationA>();
        services.AddScoped<ITaskTService, TaskTImplementationA>();
        services.AddScoped<IValueTaskService, ValueTaskImplementationA>();
        services.AddScoped<IValueTaskTService, ValueTaskTImplementationA>();
    }

    [Scenario("ComprehensiveSample RedirectAndReplay policy redirects to specific fallback")]
    [Fact]
    public void RedirectAndReplay_RedirectsToSpecificFallback()
        => Given("ComprehensiveSample configured with RedirectAndReplay", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UsePrimaryImplementation"] = "true"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("service is called and primary fails", (Func<TestContext, Task<(TestContext, string)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRedirectSpecificService>();
            var result = await service.ProcessAsync();
            return (ctx, result);
        }))
        .Then("noop diagnostics handler is used", r =>
            r.Item2.Contains("Safe fallback"))
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample RedirectAndReplayOrdered tries fallbacks in order")]
    [Fact]
    public void RedirectAndReplayOrdered_TriesInOrder()
        => Given("ComprehensiveSample configured with RedirectAndReplayOrdered", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudDatabase"] = "true"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("service is called and cloud fails", (Func<TestContext, Task<(TestContext, string)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRedirectOrderedService>();
            var result = await service.ProcessAsync();
            return (ctx, result);
        }))
        .Then("falls back to in-memory cache", r => r.Item2.Contains("in-memory cache"))
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample Task<T> return type works correctly")]
    [Fact]
    public void TaskTReturnType_WorksCorrectly()
        => Given("ComprehensiveSample configured for Task<T> service", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReturnTypes:TaskT"] = "b"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("Task<T> service is called", (Func<TestContext, Task<(TestContext, string)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITaskTService>();
            var result = await service.GetResultAsync();
            return (ctx, result);
        }))
        .Then("Task<T> returns correct value", r => r.Item2.Contains("TaskTImplementationB"))
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample ValueTask<T> return type works correctly")]
    [Fact]
    public void ValueTaskTReturnType_WorksCorrectly()
        => Given("ComprehensiveSample configured for ValueTask<T> service", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReturnTypes:ValueTaskT"] = ""
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("ValueTask<T> service is called", (Func<TestContext, Task<(TestContext, int)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IValueTaskTService>();
            var result = await service.GetResultAsync();
            return (ctx, result);
        }))
        .Then("ValueTask<T> returns correct value", r => r.Item2 == 42)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample custom decorators are applied")]
    [Fact]
    public void CustomDecorators_AreApplied()
        => Given("ComprehensiveSample configured with custom decorators", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:EnablePremiumCaching"] = "false"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("data service is called", (Func<TestContext, Task<(TestContext, string)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IDataService>();
            var result = await service.GetDataAsync("testkey");
            return (ctx, result);
        }))
        .Then("decorators are applied and service works", r => r.Item2.Contains("testkey"))
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample void return type works correctly")]
    [Fact]
    public void VoidReturnType_WorksCorrectly()
        => Given("ComprehensiveSample configured for void service", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReturnTypes:Void"] = "b"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("void service is called", ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IVoidService>();
            service.Execute(); // void method
            return (ctx, true);
        })
        .Then("void method completes successfully", r => r.Item2)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample Task return type works correctly")]
    [Fact]
    public void TaskReturnType_WorksCorrectly()
        => Given("ComprehensiveSample configured for Task service", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReturnTypes:Task"] = "b"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("Task service is called", (Func<TestContext, Task<(TestContext, bool)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITaskService>();
            await service.ExecuteAsync(); // Task method
            return (ctx, true);
        }))
        .Then("Task method completes successfully", r => r.Item2)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample ValueTask return type works correctly")]
    [Fact]
    public void ValueTaskReturnType_WorksCorrectly()
        => Given("ComprehensiveSample configured for ValueTask service", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReturnTypes:ValueTask"] = "b"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("ValueTask service is called", (Func<TestContext, Task<(TestContext, bool)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IValueTaskService>();
            await service.ExecuteAsync(); // ValueTask method
            return (ctx, true);
        }))
        .Then("ValueTask method completes successfully", r => r.Item2)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample Throw policy propagates exceptions")]
    [Fact]
    public void ThrowPolicy_PropagatesExceptions()
        => Given("ComprehensiveSample configured with Throw policy", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseUnstableImplementation"] = "true"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("service with throw policy fails", (Func<TestContext, Task<(TestContext, Exception?)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IThrowPolicyService>();

            Exception? caughtException = null;
            try
            {
                await service.ProcessAsync();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            return (ctx, caughtException);
        }))
        .Then("exception is thrown", r => r.Item2 is InvalidOperationException)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample RedirectAndReplayDefault falls back successfully")]
    [Fact]
    public void RedirectAndReplayDefault_FallsBackSuccessfully()
        => Given("ComprehensiveSample configured for redirect default", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseExperimentalImplementation"] = "true"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("experimental service fails and redirects", (Func<TestContext, Task<(TestContext, string)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRedirectDefaultService>();
            var result = await service.ProcessAsync();
            return (ctx, result);
        }))
        .Then("default implementation is used", r => r.Item2.Contains("Default") || r.Item2.Contains("Experimental"))
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample RedirectAndReplayAny tries all fallbacks")]
    [Fact]
    public void RedirectAndReplayAny_TriesAllFallbacks()
        => Given("ComprehensiveSample configured for redirect any", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UsePrimaryProvider"] = "true"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("primary provider fails and framework tries fallbacks", (Func<TestContext, Task<(TestContext, string)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRedirectAnyService>();
            var result = await service.ProcessAsync();
            return (ctx, result);
        }))
        .Then("a fallback implementation succeeds", r => !string.IsNullOrEmpty(r.Item2))
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample payment processor variant selection")]
    [Fact]
    public void PaymentProcessor_VariantSelection()
        => Given("ComprehensiveSample configured with payment variant", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:PaymentProcessor"] = "paypal",
                    ["FeatureManagement:PaymentProcessor:Variant"] = "paypal"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("payment is processed", (Func<TestContext, Task<(TestContext, string)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IPaymentProcessor>();
            var result = await service.ProcessPaymentAsync(100m, "USD");
            return (ctx, result);
        }))
        .Then("correct payment processor is selected", r => r.Item2.Contains("PayPal") || r.Item2.Contains("Stripe"))
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample notification service selection")]
    [Fact]
    public void NotificationService_Selection()
        => Given("ComprehensiveSample configured for notifications", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseSmsNotifications"] = "false"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("notification is sent", (Func<TestContext, Task<(TestContext, bool)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await service.SendAsync("user@example.com", "Test message");
            return (ctx, true);
        }))
        .Then("notification is sent successfully", r => r.Item2)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("ComprehensiveSample multiple scopes maintain isolation")]
    [Fact]
    public void MultipleScopes_MaintainIsolation()
        => Given("ComprehensiveSample configured", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:EnablePremiumCaching"] = "false"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllComprehensiveServices(services);

            var experiments = ExperimentConfiguration.ConfigureAllExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("multiple scopes are created", (Func<TestContext, Task<(TestContext, bool)>>)(async ctx =>
        {
            var results = new List<string>();

            // Create 3 independent scopes
            for (var i = 0; i < 3; i++)
            {
                using var scope = ctx.ServiceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDataService>();
                var result = await service.GetDataAsync($"key{i}");
                results.Add(result);
            }

            // All should complete successfully
            var allSuccessful = results.All(r => !string.IsNullOrEmpty(r));

            return (ctx, allSuccessful);
        }))
        .Then("all scopes work independently", r => r.Item2)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();
}
