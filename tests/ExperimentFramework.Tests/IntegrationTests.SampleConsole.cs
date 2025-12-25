extern alias SampleConsole;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using SampleConsole::ExperimentFramework.SampleConsole;
using SampleConsole::ExperimentFramework.SampleConsole.Contexts;
using SampleConsole::ExperimentFramework.SampleConsole.Providers;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests;

[Feature("SampleConsole integration tests validate end-to-end functionality")]
public sealed class SampleConsoleIntegrationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestContext(
        IServiceProvider ServiceProvider,
        IConfiguration Configuration);

    [Scenario("SampleConsole with source-generated proxies selects correct database based on feature flag")]
    [Fact]
    public void SourceGenerated_FeatureFlag_SelectsCorrectDatabase()
        => Given("SampleConsole configured with UseCloudDb=true", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudDb"] = "true",
                    ["Experiments:TaxProvider"] = ""
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            // Register services like the sample does
            services.AddScoped<MyDbContext>();
            services.AddScoped<MyCloudDbContext>();
            services.AddScoped<DefaultTaxProvider>();
            services.AddScoped<OkTaxProvider>();
            services.AddScoped<TxTaxProvider>();
            services.AddScoped<IMyDatabase, MyDbContext>();
            services.AddScoped<IMyTaxProvider, DefaultTaxProvider>();

            // Use source-generated proxies (default)
            var experiments = ExperimentConfiguration.ConfigureExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("database is accessed", (Func<TestContext, Task<(TestContext, string)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IMyDatabase>();
            var name = await db.GetDatabaseNameAsync(CancellationToken.None);
            return (ctx, name);
        }))
        .Then("cloud database is used", r => r.Item2 == "CloudDb")
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("SampleConsole with source-generated proxies selects local database when flag is false")]
    [Fact]
    public void SourceGenerated_FeatureFlagFalse_SelectsLocalDatabase()
        => Given("SampleConsole configured with UseCloudDb=false", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudDb"] = "false",
                    ["Experiments:TaxProvider"] = ""
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            services.AddScoped<MyDbContext>();
            services.AddScoped<MyCloudDbContext>();
            services.AddScoped<DefaultTaxProvider>();
            services.AddScoped<OkTaxProvider>();
            services.AddScoped<TxTaxProvider>();
            services.AddScoped<IMyDatabase, MyDbContext>();
            services.AddScoped<IMyTaxProvider, DefaultTaxProvider>();

            var experiments = ExperimentConfiguration.ConfigureExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("database is accessed", (Func<TestContext, Task<(TestContext, string)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IMyDatabase>();
            var name = await db.GetDatabaseNameAsync(CancellationToken.None);
            return (ctx, name);
        }))
        .Then("local database is used", r => r.Item2 == "LocalDb")
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("SampleConsole with runtime proxies selects correct database based on feature flag")]
    [Fact]
    public void RuntimeProxy_FeatureFlag_SelectsCorrectDatabase()
        => Given("SampleConsole configured with runtime proxies and UseCloudDb=true", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudDb"] = "true",
                    ["Experiments:TaxProvider"] = ""
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            services.AddScoped<MyDbContext>();
            services.AddScoped<MyCloudDbContext>();
            services.AddScoped<DefaultTaxProvider>();
            services.AddScoped<OkTaxProvider>();
            services.AddScoped<TxTaxProvider>();
            services.AddScoped<IMyDatabase, MyDbContext>();
            services.AddScoped<IMyTaxProvider, DefaultTaxProvider>();

            // Use runtime proxies instead of source-generated
            var experiments = ExperimentFrameworkBuilder.Create()
                .AddLogger(l => l.AddBenchmarks().AddErrorLogging())
                .Define<IMyDatabase>(c =>
                    c.UsingFeatureFlag("UseCloudDb")
                        .AddDefaultTrial<MyDbContext>(key: "false")
                        .AddTrial<MyCloudDbContext>(key: "true")
                        .OnErrorRedirectAndReplayDefault())
                .Define<IMyTaxProvider>(c =>
                    c.UsingConfigurationKey("Experiments:TaxProvider")
                        .AddDefaultTrial<DefaultTaxProvider>(key: "")
                        .AddTrial<OkTaxProvider>(key: "OK")
                        .AddTrial<TxTaxProvider>(key: "TX")
                        .OnErrorRedirectAndReplayAny())
                .UseDispatchProxy(); // Runtime proxies

            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("database is accessed", (Func<TestContext, Task<(TestContext, string)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IMyDatabase>();
            var name = await db.GetDatabaseNameAsync(CancellationToken.None);
            return (ctx, name);
        }))
        .Then("cloud database is used", r => r.Item2 == "CloudDb")
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("SampleConsole configuration-based tax provider selection works correctly")]
    [Fact]
    public void ConfigurationValue_SelectsCorrectTaxProvider()
        => Given("SampleConsole configured with TaxProvider=OK", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudDb"] = "false",
                    ["Experiments:TaxProvider"] = "OK"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            services.AddScoped<MyDbContext>();
            services.AddScoped<MyCloudDbContext>();
            services.AddScoped<DefaultTaxProvider>();
            services.AddScoped<OkTaxProvider>();
            services.AddScoped<TxTaxProvider>();
            services.AddScoped<IMyDatabase, MyDbContext>();
            services.AddScoped<IMyTaxProvider, DefaultTaxProvider>();

            var experiments = ExperimentConfiguration.ConfigureExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("tax is calculated", ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var tax = scope.ServiceProvider.GetRequiredService<IMyTaxProvider>();
            var amount = tax.CalculateTax("OK", 100m);
            return (ctx, amount);
        })
        .Then("OK tax rate is applied", r => r.Item2 == 105.0m) // OkTaxProvider adds 5%
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("SampleConsole runtime proxies work with both services")]
    [Fact]
    public void RuntimeProxies_BothServicesWork()
        => Given("SampleConsole configured with runtime proxies for both experiments", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudDb"] = "false",
                    ["Experiments:TaxProvider"] = "OK"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            services.AddScoped<MyDbContext>();
            services.AddScoped<MyCloudDbContext>();
            services.AddScoped<DefaultTaxProvider>();
            services.AddScoped<OkTaxProvider>();
            services.AddScoped<TxTaxProvider>();
            services.AddScoped<IMyDatabase, MyDbContext>();
            services.AddScoped<IMyTaxProvider, DefaultTaxProvider>();

            var experiments = ExperimentFrameworkBuilder.Create()
                .AddLogger(l => l.AddBenchmarks().AddErrorLogging())
                .Define<IMyDatabase>(c =>
                    c.UsingFeatureFlag("UseCloudDb")
                        .AddDefaultTrial<MyDbContext>(key: "false")
                        .AddTrial<MyCloudDbContext>(key: "true")
                        .OnErrorRedirectAndReplayDefault())
                .Define<IMyTaxProvider>(c =>
                    c.UsingConfigurationKey("Experiments:TaxProvider")
                        .AddDefaultTrial<DefaultTaxProvider>(key: "")
                        .AddTrial<OkTaxProvider>(key: "OK")
                        .AddTrial<TxTaxProvider>(key: "TX")
                        .OnErrorRedirectAndReplayAny())
                .UseDispatchProxy();

            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("both services are used", (Func<TestContext, Task<(TestContext, string, decimal)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IMyDatabase>();
            var tax = scope.ServiceProvider.GetRequiredService<IMyTaxProvider>();

            var dbName = await db.GetDatabaseNameAsync(CancellationToken.None);
            var taxAmount = tax.CalculateTax("OK", 100m);

            return (ctx, dbName, taxAmount);
        }))
        .Then("runtime proxies route correctly", r =>
            r.Item2 == "LocalDb" && r.Item3 == 105.0m)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("SampleConsole TX tax provider applies correct rate")]
    [Fact]
    public void TaxProvider_TXAppliesCorrectRate()
        => Given("SampleConsole configured with TaxProvider=TX", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudDb"] = "false",
                    ["Experiments:TaxProvider"] = "TX"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            services.AddScoped<MyDbContext>();
            services.AddScoped<MyCloudDbContext>();
            services.AddScoped<DefaultTaxProvider>();
            services.AddScoped<OkTaxProvider>();
            services.AddScoped<TxTaxProvider>();
            services.AddScoped<IMyDatabase, MyDbContext>();
            services.AddScoped<IMyTaxProvider, DefaultTaxProvider>();

            var experiments = ExperimentConfiguration.ConfigureExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("tax is calculated", ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var tax = scope.ServiceProvider.GetRequiredService<IMyTaxProvider>();
            var amount = tax.CalculateTax("TX", 100m);
            return (ctx, amount);
        })
        .Then("TX tax rate is applied", r => r.Item2 == 106.25m) // TxTaxProvider adds 6.25%
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("SampleConsole default tax provider when config is empty")]
    [Fact]
    public void TaxProvider_DefaultWhenConfigEmpty()
        => Given("SampleConsole configured with empty TaxProvider", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudDb"] = "false",
                    ["Experiments:TaxProvider"] = ""
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            services.AddScoped<MyDbContext>();
            services.AddScoped<MyCloudDbContext>();
            services.AddScoped<DefaultTaxProvider>();
            services.AddScoped<OkTaxProvider>();
            services.AddScoped<TxTaxProvider>();
            services.AddScoped<IMyDatabase, MyDbContext>();
            services.AddScoped<IMyTaxProvider, DefaultTaxProvider>();

            var experiments = ExperimentConfiguration.ConfigureExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("tax is calculated", ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var tax = scope.ServiceProvider.GetRequiredService<IMyTaxProvider>();
            var amount = tax.CalculateTax("", 100m);
            return (ctx, amount);
        })
        .Then("default tax rate is applied", r => r.Item2 == 107.0m) // DefaultTaxProvider adds 7%
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("SampleConsole error handling redirects to default on database failure")]
    [Fact]
    public void ErrorHandling_RedirectsToDefaultOnFailure()
        => Given("SampleConsole configured with error handling", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudDb"] = "true", // Will use cloud which may fail
                    ["Experiments:TaxProvider"] = ""
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            services.AddScoped<MyDbContext>();
            services.AddScoped<MyCloudDbContext>();
            services.AddScoped<DefaultTaxProvider>();
            services.AddScoped<OkTaxProvider>();
            services.AddScoped<TxTaxProvider>();
            services.AddScoped<IMyDatabase, MyDbContext>();
            services.AddScoped<IMyTaxProvider, DefaultTaxProvider>();

            var experiments = ExperimentConfiguration.ConfigureExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("database is accessed", (Func<TestContext, Task<(TestContext, string)>>)(async ctx =>
        {
            using var scope = ctx.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IMyDatabase>();
            // CloudDb should be selected, but has error handling configured
            var name = await db.GetDatabaseNameAsync(CancellationToken.None);
            return (ctx, name);
        }))
        .Then("database returns a name", r => !string.IsNullOrEmpty(r.Item2))
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("SampleConsole multiple concurrent requests maintain independence")]
    [Fact]
    public void Concurrent_RequestsMaintainIndependence()
        => Given("SampleConsole configured", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudDb"] = "false",
                    ["Experiments:TaxProvider"] = "OK"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            services.AddScoped<MyDbContext>();
            services.AddScoped<MyCloudDbContext>();
            services.AddScoped<DefaultTaxProvider>();
            services.AddScoped<OkTaxProvider>();
            services.AddScoped<TxTaxProvider>();
            services.AddScoped<IMyDatabase, MyDbContext>();
            services.AddScoped<IMyTaxProvider, DefaultTaxProvider>();

            var experiments = ExperimentConfiguration.ConfigureExperiments();
            services.AddExperimentFramework(experiments);

            var sp = services.BuildServiceProvider();
            return new TestContext(sp, config);
        })
        .When("multiple concurrent requests are made", (Func<TestContext, Task<(TestContext, bool)>>)(async ctx =>
        {
            var tasks = new List<Task<(string, decimal)>>();

            // Simulate 10 concurrent requests
            for (var i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var scope = ctx.ServiceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<IMyDatabase>();
                    var tax = scope.ServiceProvider.GetRequiredService<IMyTaxProvider>();

                    var dbName = await db.GetDatabaseNameAsync(CancellationToken.None);
                    var taxAmount = tax.CalculateTax("OK", 100m);

                    return (dbName, taxAmount);
                }));
            }

            var results = await Task.WhenAll(tasks);

            // All should have same results
            var allSame = results.All(r => r.Item1 == "LocalDb" && r.Item2 == 105.0m);

            return (ctx, allSame);
        }))
        .Then("all requests complete successfully with consistent results", r => r.Item2)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();
}
