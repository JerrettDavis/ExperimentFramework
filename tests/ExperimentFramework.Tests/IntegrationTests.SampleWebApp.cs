extern alias SampleWebApp;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests;

[Feature("SampleWebApp integration tests validate web API functionality")]
public sealed class SampleWebAppIntegrationTests(ITestOutputHelper output) : TinyBddXunitBase(output), IClassFixture<WebApplicationFactory<SampleWebApp::Program>>
{
    private sealed record TestContext(
        WebApplicationFactory<SampleWebApp::Program> Factory,
        HttpClient Client);

    [Scenario("SampleWebApp home page returns API documentation")]
    [Fact]
    public void HomePage_ReturnsApiDocumentation()
        => Given("SampleWebApp is running", () =>
        {
            var factory = new WebApplicationFactory<SampleWebApp::Program>();
            var client = factory.CreateClient();
            return new TestContext(factory, client);
        })
        .When("home page is requested", (Func<TestContext, Task<(TestContext, JsonDocument)>>)(async ctx =>
        {
            var response = await ctx.Client.GetAsync("/");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return (ctx, json!);
        }))
        .Then("API documentation is returned", r =>
        {
            var root = r.Item2.RootElement;
            return root.GetProperty("AppName").GetString() == "ExperimentFramework Web Sample" &&
                   root.GetProperty("Experiments").GetArrayLength() == 2;
        })
        .Finally(r => { r.Item1.Client.Dispose(); r.Item1.Factory.Dispose(); })
        .AssertPassed();

    [Scenario("SampleWebApp sticky routing provides consistent recommendations")]
    [Fact]
    public void StickyRouting_ProvidesConsistentRecommendations()
        => Given("SampleWebApp is running", () =>
        {
            var factory = new WebApplicationFactory<SampleWebApp::Program>();
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true // Enable cookie handling for session
            });
            return new TestContext(factory, client);
        })
        .When("recommendations are requested multiple times", (Func<TestContext, Task<(TestContext, string, string)>>)(async ctx =>
        {
            // First request - establishes session
            var response1 = await ctx.Client.GetAsync("/api/recommendations?userId=testuser");
            response1.EnsureSuccessStatusCode();
            var json1 = await response1.Content.ReadFromJsonAsync<JsonDocument>();
            var algorithm1 = json1!.RootElement.GetProperty("Algorithm").GetString()!;

            // Second request - should use same algorithm due to sticky routing
            var response2 = await ctx.Client.GetAsync("/api/recommendations?userId=testuser");
            response2.EnsureSuccessStatusCode();
            var json2 = await response2.Content.ReadFromJsonAsync<JsonDocument>();
            var algorithm2 = json2!.RootElement.GetProperty("Algorithm").GetString()!;

            return (ctx, algorithm1, algorithm2);
        }))
        .Then("same algorithm is used consistently", r =>
            r.Item2 == r.Item3 && !string.IsNullOrEmpty(r.Item2))
        .Finally(r => { r.Item1.Client.Dispose(); r.Item1.Factory.Dispose(); })
        .AssertPassed();

    [Scenario("SampleWebApp algorithm endpoint returns algorithm information")]
    [Fact]
    public void AlgorithmEndpoint_ReturnsAlgorithmInfo()
        => Given("SampleWebApp is running", () =>
        {
            var factory = new WebApplicationFactory<SampleWebApp::Program>();
            var client = factory.CreateClient();
            return new TestContext(factory, client);
        })
        .When("algorithm info is requested", (Func<TestContext, Task<(TestContext, JsonDocument)>>)(async ctx =>
        {
            var response = await ctx.Client.GetAsync("/api/recommendations/algorithm");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return (ctx, json!);
        }))
        .Then("algorithm information is returned", r =>
        {
            var root = r.Item2.RootElement;
            var algorithm = root.GetProperty("Algorithm").GetString();
            var stickyRouting = root.GetProperty("StickyRouting").GetBoolean();

            return !string.IsNullOrEmpty(algorithm) && stickyRouting &&
                   (algorithm == "Popularity-Based" ||
                    algorithm == "Machine Learning" ||
                    algorithm == "Collaborative Filtering");
        })
        .Finally(r => { r.Item1.Client.Dispose(); r.Item1.Factory.Dispose(); })
        .AssertPassed();

    [Scenario("SampleWebApp checkout flow uses standard when feature flag is false")]
    [Fact]
    public void CheckoutFlow_UsesStandardWhenFeatureFlagFalse()
        => Given("SampleWebApp configured with EnableExpressCheckout=false", () =>
        {
            var factory = new WebApplicationFactory<SampleWebApp::Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["FeatureManagement:EnableExpressCheckout"] = "false"
                        });
                    });
                });
            var client = factory.CreateClient();
            return new TestContext(factory, client);
        })
        .When("checkout flow is requested", (Func<TestContext, Task<(TestContext, JsonDocument)>>)(async ctx =>
        {
            var response = await ctx.Client.GetAsync("/api/checkout/flow");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return (ctx, json!);
        }))
        .Then("standard checkout flow is returned", r =>
        {
            var root = r.Item2.RootElement;
            var flowName = root.GetProperty("FlowName").GetString();
            var steps = root.GetProperty("Steps").GetArrayLength();

            return flowName == "Standard Checkout" && steps == 5;
        })
        .Finally(r => { r.Item1.Client.Dispose(); r.Item1.Factory.Dispose(); })
        .AssertPassed();

    [Scenario("SampleWebApp checkout flow uses express when feature flag is true")]
    [Fact]
    public void CheckoutFlow_UsesExpressWhenFeatureFlagTrue()
        => Given("SampleWebApp configured with EnableExpressCheckout=true", () =>
        {
            var factory = new WebApplicationFactory<SampleWebApp::Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["FeatureManagement:EnableExpressCheckout"] = "true"
                        });
                    });
                });
            var client = factory.CreateClient();
            return new TestContext(factory, client);
        })
        .When("checkout flow is requested", (Func<TestContext, Task<(TestContext, JsonDocument)>>)(async ctx =>
        {
            var response = await ctx.Client.GetAsync("/api/checkout/flow");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return (ctx, json!);
        }))
        .Then("express checkout flow is returned", r =>
        {
            var root = r.Item2.RootElement;
            var flowName = root.GetProperty("FlowName").GetString();
            var steps = root.GetProperty("Steps").GetArrayLength();

            return flowName == "Express Checkout" && steps == 3;
        })
        .Finally(r => { r.Item1.Client.Dispose(); r.Item1.Factory.Dispose(); })
        .AssertPassed();

    [Scenario("SampleWebApp checkout complete returns success")]
    [Fact]
    public void CheckoutComplete_ReturnsSuccess()
        => Given("SampleWebApp is running", () =>
        {
            var factory = new WebApplicationFactory<SampleWebApp::Program>();
            var client = factory.CreateClient();
            return new TestContext(factory, client);
        })
        .When("checkout is completed", (Func<TestContext, Task<(TestContext, JsonDocument, HttpStatusCode)>>)(async ctx =>
        {
            var request = new
            {
                Items = new[] { "Product1", "Product2" },
                Total = 99.99m
            };

            var response = await ctx.Client.PostAsJsonAsync("/api/checkout/complete", request);
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return (ctx, json!, response.StatusCode);
        }))
        .Then("checkout succeeds with order ID", r =>
        {
            var root = r.Item2.RootElement;
            var success = root.GetProperty("Success").GetBoolean();
            var orderId = root.GetProperty("OrderId").GetString();

            return r.Item3 == HttpStatusCode.OK &&
                   success &&
                   !string.IsNullOrEmpty(orderId);
        })
        .Finally(r => { r.Item1.Client.Dispose(); r.Item1.Factory.Dispose(); })
        .AssertPassed();

    [Scenario("SampleWebApp recommendations return different algorithms for different sessions")]
    [Fact]
    public void StickyRouting_DifferentSessionsCanGetDifferentAlgorithms()
        => Given("SampleWebApp is running", () =>
        {
            var factory = new WebApplicationFactory<SampleWebApp::Program>();
            return factory;
        })
        .When("multiple clients request recommendations", (Func<WebApplicationFactory<SampleWebApp::Program>, Task<(WebApplicationFactory<SampleWebApp::Program>, HashSet<string>)>>)(async factory =>
        {
            var algorithms = new HashSet<string>();

            // Create multiple independent clients (different sessions)
            for (var i = 0; i < 10; i++)
            {
                using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
                {
                    HandleCookies = true
                });

                var response = await client.GetAsync("/api/recommendations?userId=user" + i);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
                var algorithm = json!.RootElement.GetProperty("Algorithm").GetString()!;
                algorithms.Add(algorithm);
            }

            return (factory, algorithms);
        }))
        .Then("different algorithms are distributed across sessions", r =>
            r.Item2.Count > 1) // Should have at least 2 different algorithms across 10 sessions
        .Finally(r => r.Item1.Dispose())
        .AssertPassed();
}
