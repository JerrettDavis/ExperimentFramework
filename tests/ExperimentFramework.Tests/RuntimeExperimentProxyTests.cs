using ExperimentFramework.Decorators;
using ExperimentFramework.Models;
using ExperimentFramework.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests;

[Feature("RuntimeExperimentProxy validates DispatchProxy-based runtime proxies")]
public sealed class RuntimeExperimentProxyTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // Test interfaces and implementations
    public interface ITestService
    {
        string GetValue();
        Task<string> GetValueAsync();
        ValueTask<int> GetNumberAsync();
        Task DoWorkAsync();
        ValueTask DoWorkValueTaskAsync();
    }

    public class TestServiceA : ITestService
    {
        public string GetValue() => "ServiceA";
        public async Task<string> GetValueAsync()
        {
            await Task.Delay(1);
            return "ServiceA-Async";
        }
        public async ValueTask<int> GetNumberAsync()
        {
            await Task.Delay(1);
            return 42;
        }
        public async Task DoWorkAsync() => await Task.Delay(1);
        public async ValueTask DoWorkValueTaskAsync() => await Task.Delay(1);
    }

    public class TestServiceB : ITestService
    {
        public string GetValue() => "ServiceB";
        public async Task<string> GetValueAsync()
        {
            await Task.Delay(1);
            return "ServiceB-Async";
        }
        public async ValueTask<int> GetNumberAsync()
        {
            await Task.Delay(1);
            return 99;
        }
        public async Task DoWorkAsync() => await Task.Delay(1);
        public async ValueTask DoWorkValueTaskAsync() => await Task.Delay(1);
    }

    public class FailingTestService : ITestService
    {
        public string GetValue() => throw new InvalidOperationException("Service failed");
        public Task<string> GetValueAsync() => throw new InvalidOperationException("Async service failed");
        public ValueTask<int> GetNumberAsync() => throw new InvalidOperationException("ValueTask service failed");
        public Task DoWorkAsync() => throw new InvalidOperationException("Task service failed");
        public ValueTask DoWorkValueTaskAsync() => throw new InvalidOperationException("ValueTask service failed");
    }

    private sealed record TestContext(
        IServiceProvider ServiceProvider,
        ITestService Proxy);

    [Scenario("RuntimeExperimentProxy creates valid proxy instance")]
    [Fact]
    public void Create_ValidProxy()
        => Given("RuntimeExperimentProxy configuration", () =>
        {
            var services = new ServiceCollection();
            services.AddScoped<TestServiceA>();
            services.AddScoped<TestServiceB>();
            services.AddSingleton<IExperimentTelemetry>(NoopExperimentTelemetry.Instance);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestKey"] = "a"
                })
                .Build();

            services.AddSingleton<IConfiguration>(config);

            var sp = services.BuildServiceProvider();

            var registration = new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                SelectorName = "TestKey",
                Trials = new Dictionary<string, Type>
                {
                    ["a"] = typeof(TestServiceA),
                    ["b"] = typeof(TestServiceB)
                },
                DefaultKey = "a",
                OnErrorPolicy = OnErrorPolicy.Throw
            };

            var proxy = RuntimeExperimentProxy<ITestService>.Create(
                sp.GetRequiredService<IServiceScopeFactory>(),
                registration,
                Array.Empty<IExperimentDecoratorFactory>(),
                sp.GetRequiredService<IExperimentTelemetry>());

            return new TestContext(sp, proxy);
        })
        .When("proxy is used", ctx =>
        {
            var result = ctx.Proxy.GetValue();
            return (ctx, result);
        })
        .Then("proxy routes to correct implementation", r => r.Item2 == "ServiceA")
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("RuntimeExperimentProxy handles Task<T> return type")]
    [Fact]
    public void TaskT_ReturnType()
        => Given("RuntimeExperimentProxy for async method", () =>
        {
            var services = new ServiceCollection();
            services.AddScoped<TestServiceA>();
            services.AddScoped<TestServiceB>();
            services.AddSingleton<IExperimentTelemetry>(NoopExperimentTelemetry.Instance);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestKey"] = "b"
                })
                .Build();

            services.AddSingleton<IConfiguration>(config);

            var sp = services.BuildServiceProvider();

            var registration = new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                SelectorName = "TestKey",
                Trials = new Dictionary<string, Type>
                {
                    ["a"] = typeof(TestServiceA),
                    ["b"] = typeof(TestServiceB)
                },
                DefaultKey = "a",
                OnErrorPolicy = OnErrorPolicy.Throw
            };

            var proxy = RuntimeExperimentProxy<ITestService>.Create(
                sp.GetRequiredService<IServiceScopeFactory>(),
                registration,
                Array.Empty<IExperimentDecoratorFactory>(),
                sp.GetRequiredService<IExperimentTelemetry>());

            return new TestContext(sp, proxy);
        })
        .When("async method is called", (Func<TestContext, Task<(TestContext, string)>>)(async ctx =>
        {
            var result = await ctx.Proxy.GetValueAsync();
            return (ctx, result);
        }))
        .Then("async method returns correct value", r => r.Item2 == "ServiceB-Async")
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("RuntimeExperimentProxy handles ValueTask<T> return type")]
    [Fact]
    public void ValueTaskT_ReturnType()
        => Given("RuntimeExperimentProxy for ValueTask method", () =>
        {
            var services = new ServiceCollection();
            services.AddScoped<TestServiceA>();
            services.AddScoped<TestServiceB>();
            services.AddSingleton<IExperimentTelemetry>(NoopExperimentTelemetry.Instance);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestKey"] = "a"
                })
                .Build();

            services.AddSingleton<IConfiguration>(config);

            var sp = services.BuildServiceProvider();

            var registration = new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                SelectorName = "TestKey",
                Trials = new Dictionary<string, Type>
                {
                    ["a"] = typeof(TestServiceA),
                    ["b"] = typeof(TestServiceB)
                },
                DefaultKey = "a",
                OnErrorPolicy = OnErrorPolicy.Throw
            };

            var proxy = RuntimeExperimentProxy<ITestService>.Create(
                sp.GetRequiredService<IServiceScopeFactory>(),
                registration,
                Array.Empty<IExperimentDecoratorFactory>(),
                sp.GetRequiredService<IExperimentTelemetry>());

            return new TestContext(sp, proxy);
        })
        .When("ValueTask method is called", (Func<TestContext, Task<(TestContext, int)>>)(async ctx =>
        {
            var result = await ctx.Proxy.GetNumberAsync();
            return (ctx, result);
        }))
        .Then("ValueTask method returns correct value", r => r.Item2 == 42)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("RuntimeExperimentProxy handles Task return type")]
    [Fact]
    public void Task_ReturnType()
        => Given("RuntimeExperimentProxy for Task method", () =>
        {
            var services = new ServiceCollection();
            services.AddScoped<TestServiceA>();
            services.AddSingleton<IExperimentTelemetry>(NoopExperimentTelemetry.Instance);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestKey"] = "a"
                })
                .Build();

            services.AddSingleton<IConfiguration>(config);

            var sp = services.BuildServiceProvider();

            var registration = new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                SelectorName = "TestKey",
                Trials = new Dictionary<string, Type>
                {
                    ["a"] = typeof(TestServiceA)
                },
                DefaultKey = "a",
                OnErrorPolicy = OnErrorPolicy.Throw
            };

            var proxy = RuntimeExperimentProxy<ITestService>.Create(
                sp.GetRequiredService<IServiceScopeFactory>(),
                registration,
                Array.Empty<IExperimentDecoratorFactory>(),
                sp.GetRequiredService<IExperimentTelemetry>());

            return new TestContext(sp, proxy);
        })
        .When("Task method is called", (Func<TestContext, Task<(TestContext, bool)>>)(async ctx =>
        {
            await ctx.Proxy.DoWorkAsync();
            return (ctx, true);
        }))
        .Then("Task method completes successfully", r => r.Item2)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("RuntimeExperimentProxy handles ValueTask return type")]
    [Fact]
    public void ValueTask_ReturnType()
        => Given("RuntimeExperimentProxy for ValueTask method", () =>
        {
            var services = new ServiceCollection();
            services.AddScoped<TestServiceA>();
            services.AddSingleton<IExperimentTelemetry>(NoopExperimentTelemetry.Instance);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestKey"] = "a"
                })
                .Build();

            services.AddSingleton<IConfiguration>(config);

            var sp = services.BuildServiceProvider();

            var registration = new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                SelectorName = "TestKey",
                Trials = new Dictionary<string, Type>
                {
                    ["a"] = typeof(TestServiceA)
                },
                DefaultKey = "a",
                OnErrorPolicy = OnErrorPolicy.Throw
            };

            var proxy = RuntimeExperimentProxy<ITestService>.Create(
                sp.GetRequiredService<IServiceScopeFactory>(),
                registration,
                Array.Empty<IExperimentDecoratorFactory>(),
                sp.GetRequiredService<IExperimentTelemetry>());

            return new TestContext(sp, proxy);
        })
        .When("ValueTask method is called", (Func<TestContext, Task<(TestContext, bool)>>)(async ctx =>
        {
            await ctx.Proxy.DoWorkValueTaskAsync();
            return (ctx, true);
        }))
        .Then("ValueTask method completes successfully", r => r.Item2)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("RuntimeExperimentProxy uses BooleanFeatureFlag selection")]
    [Fact]
    public void BooleanFeatureFlag_Selection()
        => Given("RuntimeExperimentProxy with feature flag", () =>
        {
            var services = new ServiceCollection();
            services.AddScoped<TestServiceA>();
            services.AddScoped<TestServiceB>();
            services.AddSingleton<IExperimentTelemetry>(NoopExperimentTelemetry.Instance);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseServiceB"] = "true"
                })
                .Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            var sp = services.BuildServiceProvider();

            var registration = new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.BooleanFeatureFlag,
                SelectorName = "UseServiceB",
                Trials = new Dictionary<string, Type>
                {
                    ["false"] = typeof(TestServiceA),
                    ["true"] = typeof(TestServiceB)
                },
                DefaultKey = "false",
                OnErrorPolicy = OnErrorPolicy.Throw
            };

            var proxy = RuntimeExperimentProxy<ITestService>.Create(
                sp.GetRequiredService<IServiceScopeFactory>(),
                registration,
                Array.Empty<IExperimentDecoratorFactory>(),
                sp.GetRequiredService<IExperimentTelemetry>());

            return new TestContext(sp, proxy);
        })
        .When("proxy is used with feature flag enabled", ctx =>
        {
            var result = ctx.Proxy.GetValue();
            return (ctx, result);
        })
        .Then("feature flag selects correct service", r => r.Item2 == "ServiceB")
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("RuntimeExperimentProxy Throw policy propagates exceptions")]
    [Fact]
    public void ThrowPolicy_PropagatesExceptions()
        => Given("RuntimeExperimentProxy with Throw policy", () =>
        {
            var services = new ServiceCollection();
            services.AddScoped<FailingTestService>();
            services.AddSingleton<IExperimentTelemetry>(NoopExperimentTelemetry.Instance);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestKey"] = "fail"
                })
                .Build();

            services.AddSingleton<IConfiguration>(config);

            var sp = services.BuildServiceProvider();

            var registration = new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                SelectorName = "TestKey",
                Trials = new Dictionary<string, Type>
                {
                    ["fail"] = typeof(FailingTestService)
                },
                DefaultKey = "fail",
                OnErrorPolicy = OnErrorPolicy.Throw
            };

            var proxy = RuntimeExperimentProxy<ITestService>.Create(
                sp.GetRequiredService<IServiceScopeFactory>(),
                registration,
                Array.Empty<IExperimentDecoratorFactory>(),
                sp.GetRequiredService<IExperimentTelemetry>());

            return new TestContext(sp, proxy);
        })
        .When("failing service is called", ctx =>
        {
            Exception? caughtException = null;
            try
            {
                ctx.Proxy.GetValue();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
            return (ctx, caughtException);
        })
        .Then("exception is thrown", r => r.Item2 is InvalidOperationException)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("RuntimeExperimentProxy RedirectAndReplayDefault falls back to default")]
    [Fact]
    public void RedirectAndReplayDefault_FallsBack()
        => Given("RuntimeExperimentProxy with fallback policy", () =>
        {
            var services = new ServiceCollection();
            services.AddScoped<FailingTestService>();
            services.AddScoped<TestServiceA>();
            services.AddSingleton<IExperimentTelemetry>(NoopExperimentTelemetry.Instance);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestKey"] = "fail"
                })
                .Build();

            services.AddSingleton<IConfiguration>(config);

            var sp = services.BuildServiceProvider();

            var registration = new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                SelectorName = "TestKey",
                Trials = new Dictionary<string, Type>
                {
                    ["fail"] = typeof(FailingTestService),
                    ["a"] = typeof(TestServiceA)
                },
                DefaultKey = "a",
                OnErrorPolicy = OnErrorPolicy.RedirectAndReplayDefault
            };

            var proxy = RuntimeExperimentProxy<ITestService>.Create(
                sp.GetRequiredService<IServiceScopeFactory>(),
                registration,
                Array.Empty<IExperimentDecoratorFactory>(),
                sp.GetRequiredService<IExperimentTelemetry>());

            return new TestContext(sp, proxy);
        })
        .When("failing service is called with fallback", ctx =>
        {
            var result = ctx.Proxy.GetValue();
            return (ctx, result);
        })
        .Then("fallback to default succeeds", r => r.Item2 == "ServiceA")
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("RuntimeExperimentProxy RedirectAndReplayAny tries all implementations")]
    [Fact]
    public void RedirectAndReplayAny_TriesAll()
        => Given("RuntimeExperimentProxy with replay any policy", () =>
        {
            var services = new ServiceCollection();
            services.AddScoped<FailingTestService>();
            services.AddScoped<TestServiceB>();
            services.AddSingleton<IExperimentTelemetry>(NoopExperimentTelemetry.Instance);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestKey"] = "fail"
                })
                .Build();

            services.AddSingleton<IConfiguration>(config);

            var sp = services.BuildServiceProvider();

            var registration = new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                SelectorName = "TestKey",
                Trials = new Dictionary<string, Type>
                {
                    ["fail"] = typeof(FailingTestService),
                    ["b"] = typeof(TestServiceB)
                },
                DefaultKey = "fail",
                OnErrorPolicy = OnErrorPolicy.RedirectAndReplayAny
            };

            var proxy = RuntimeExperimentProxy<ITestService>.Create(
                sp.GetRequiredService<IServiceScopeFactory>(),
                registration,
                Array.Empty<IExperimentDecoratorFactory>(),
                sp.GetRequiredService<IExperimentTelemetry>());

            return new TestContext(sp, proxy);
        })
        .When("failing service is called", ctx =>
        {
            var result = ctx.Proxy.GetValue();
            return (ctx, result);
        })
        .Then("any working service succeeds", r => r.Item2 == "ServiceB")
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();

    [Scenario("RuntimeExperimentProxy handles concurrent invocations")]
    [Fact]
    public void Concurrent_Invocations()
        => Given("RuntimeExperimentProxy", () =>
        {
            var services = new ServiceCollection();
            services.AddScoped<TestServiceA>();
            services.AddSingleton<IExperimentTelemetry>(NoopExperimentTelemetry.Instance);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestKey"] = "a"
                })
                .Build();

            services.AddSingleton<IConfiguration>(config);

            var sp = services.BuildServiceProvider();

            var registration = new ExperimentRegistration
            {
                ServiceType = typeof(ITestService),
                Mode = SelectionMode.ConfigurationValue,
                SelectorName = "TestKey",
                Trials = new Dictionary<string, Type>
                {
                    ["a"] = typeof(TestServiceA)
                },
                DefaultKey = "a",
                OnErrorPolicy = OnErrorPolicy.Throw
            };

            var proxy = RuntimeExperimentProxy<ITestService>.Create(
                sp.GetRequiredService<IServiceScopeFactory>(),
                registration,
                Array.Empty<IExperimentDecoratorFactory>(),
                sp.GetRequiredService<IExperimentTelemetry>());

            return new TestContext(sp, proxy);
        })
        .When("multiple concurrent calls are made", (Func<TestContext, Task<(TestContext, bool)>>)(async ctx =>
        {
            var tasks = new List<Task<string>>();

            for (var i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () => await ctx.Proxy.GetValueAsync()));
            }

            var results = await Task.WhenAll(tasks);

            var allCorrect = results.All(r => r == "ServiceA-Async");
            return (ctx, allCorrect);
        }))
        .Then("all invocations complete successfully", r => r.Item2)
        .Finally(r => (r.Item1.ServiceProvider as ServiceProvider)?.Dispose())
        .AssertPassed();
}
