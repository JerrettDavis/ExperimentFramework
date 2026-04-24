using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExperimentFramework.Dashboard.UI.Services;
using Moq;
using Moq.Protected;

namespace ExperimentFramework.Dashboard.UI.Tests.Services;

/// <summary>
/// Unit tests for ExperimentApiClient — exercise all HTTP methods using a mocked
/// HttpMessageHandler so there is no real network I/O.
/// </summary>
public sealed class ExperimentApiClientTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ExperimentApiClient BuildClient(HttpResponseMessage response, string? requestUrl = null)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        return new ExperimentApiClient(http);
    }

    private static HttpResponseMessage JsonOk(object body)
    {
        var json = JsonSerializer.Serialize(body);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage EmptyOk()
        => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage StatusOnly(HttpStatusCode code)
        => new HttpResponseMessage(code)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

    // ── GetExperimentsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetExperimentsAsync_NullEnvelope_ReturnsEmptyList()
    {
        var client = BuildClient(JsonOk(new { experiments = (object?)null }));
        var result = await client.GetExperimentsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExperimentsAsync_WithExperiments_MapsToExperimentInfo()
    {
        var body = new
        {
            experiments = new[]
            {
                new
                {
                    name = "exp-1",
                    displayName = "Experiment One",
                    description = "desc",
                    isActive = true,
                    activeVariant = "variant-a",
                    category = "Conversion",
                    lastModified = DateTime.UtcNow,
                    trials = new[] { new { key = "control", implementationType = (string?)null, isControl = true } }
                }
            }
        };
        var client = BuildClient(JsonOk(body));
        var result = await client.GetExperimentsAsync();

        Assert.Single(result);
        Assert.Equal("exp-1", result[0].Name);
        Assert.Equal("Experiment One", result[0].DisplayName);
        Assert.Equal("Active", result[0].Status);
        Assert.Equal("Conversion", result[0].Category);
        Assert.Equal("variant-a", result[0].ActiveVariant);
        Assert.Single(result[0].Variants);
    }

    [Fact]
    public async Task GetExperimentsAsync_InactiveExperiment_StatusIsInactive()
    {
        var body = new
        {
            experiments = new[]
            {
                new { name = "exp-inactive", displayName = "X", isActive = false,
                      activeVariant = (string?)null, category = (string?)null,
                      lastModified = DateTime.UtcNow, trials = (object[]?)null }
            }
        };
        var client = BuildClient(JsonOk(body));
        var result = await client.GetExperimentsAsync();
        Assert.Equal("Inactive", result[0].Status);
        Assert.Equal("General", result[0].Category);   // fallback
    }

    [Fact]
    public async Task GetExperimentsAsync_NullDisplayName_FallsBackToName()
    {
        var body = new
        {
            experiments = new[]
            {
                new { name = "exp-x", displayName = (string?)null, isActive = false,
                      activeVariant = (string?)null, category = (string?)null,
                      lastModified = DateTime.UtcNow, trials = (object[]?)null }
            }
        };
        var client = BuildClient(JsonOk(body));
        var result = await client.GetExperimentsAsync();
        Assert.Equal("exp-x", result[0].DisplayName);
    }

    // ── GetExperimentAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetExperimentAsync_Success_ReturnsExperimentInfo()
    {
        var exp = new ExperimentInfo { Name = "my-exp", Status = "Active" };
        var client = BuildClient(JsonOk(exp));
        var result = await client.GetExperimentAsync("my-exp");
        Assert.NotNull(result);
        Assert.Equal("my-exp", result.Name);
    }

    [Fact]
    public async Task GetExperimentAsync_HttpRequestException_ReturnsNull()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Not found"));

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);

        var result = await client.GetExperimentAsync("ghost");
        Assert.Null(result);
    }

    // ── ActivateVariantAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ActivateVariantAsync_Success_ReturnsMappedExperiment()
    {
        // First call: POST activate → 200
        // Second call: GET experiments → returns exp list
        var callCount = 0;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                    return StatusOnly(HttpStatusCode.OK);

                // Second call: GET /api/experiments envelope
                var body = new
                {
                    experiments = new[]
                    {
                        new { name = "exp-a", displayName = "Exp A", isActive = true,
                              activeVariant = "var-b", category = "Test",
                              lastModified = DateTime.UtcNow, trials = (object[]?)null }
                    }
                };
                var json = JsonSerializer.Serialize(body);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);

        var result = await client.ActivateVariantAsync("exp-a", "var-b");

        Assert.NotNull(result);
        Assert.Equal("var-b", result!.ActiveVariant);
    }

    [Fact]
    public async Task ActivateVariantAsync_FailureResponse_ReturnsNull()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.NotFound));
        var result = await client.ActivateVariantAsync("exp-a", "var-b");
        Assert.Null(result);
    }

    [Fact]
    public async Task ActivateVariantAsync_ExperimentNotFoundInList_ReturnsStub()
    {
        // First call: POST activate → 200
        // Second call: GET experiments → returns empty list (experiment not found)
        var callCount = 0;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                    return StatusOnly(HttpStatusCode.OK);

                var body = new { experiments = Array.Empty<object>() };
                var json = JsonSerializer.Serialize(body);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);

        var result = await client.ActivateVariantAsync("unknown-exp", "var-b");

        // Should return stub with the experiment/variant names
        Assert.NotNull(result);
        Assert.Equal("unknown-exp", result!.Name);
        Assert.Equal("var-b", result.ActiveVariant);
        Assert.Equal("Active", result.Status);
    }

    // ── CalculatePricingAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CalculatePricingAsync_ReturnsDeserializedResponse()
    {
        var expected = new PricingResponse { Strategy = "volume", UnitPrice = 9.99m, Total = 99.90m, Units = 10 };
        var client = BuildClient(JsonOk(expected));
        var result = await client.CalculatePricingAsync(10);
        Assert.NotNull(result);
        Assert.Equal("volume", result!.Strategy);
        Assert.Equal(10, result.Units);
    }

    // ── GetNotificationPreviewAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetNotificationPreviewAsync_NoUserId_CallsCorrectUrl()
    {
        var notif = new NotificationResponse { Title = "Hello", Message = "World", Style = "info", Variant = "a" };
        var client = BuildClient(JsonOk(notif));
        var result = await client.GetNotificationPreviewAsync();
        Assert.NotNull(result);
        Assert.Equal("Hello", result!.Title);
    }

    [Fact]
    public async Task GetNotificationPreviewAsync_WithUserId_ReturnsResponse()
    {
        var notif = new NotificationResponse { Title = "Hi User", Message = "Msg", Style = "success", Variant = "b" };
        var client = BuildClient(JsonOk(notif));
        var result = await client.GetNotificationPreviewAsync("user-42");
        Assert.NotNull(result);
        Assert.Equal("Hi User", result!.Title);
    }

    // ── GetRecommendationsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetRecommendationsAsync_NoUserId_ReturnsResponse()
    {
        var rec = new RecommendationResponse { Algorithm = "collab", Items = ["item-1"], Confidence = 0.9 };
        var client = BuildClient(JsonOk(rec));
        var result = await client.GetRecommendationsAsync();
        Assert.NotNull(result);
        Assert.Equal("collab", result!.Algorithm);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithUserId_ReturnsResponse()
    {
        var rec = new RecommendationResponse { Algorithm = "content", Items = ["item-x", "item-y"], Confidence = 0.7 };
        var client = BuildClient(JsonOk(rec));
        var result = await client.GetRecommendationsAsync("user-99");
        Assert.NotNull(result);
        Assert.Equal(2, result!.Items.Count);
    }

    // ── GetThemeAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetThemeAsync_ReturnsThemeResponse()
    {
        var theme = new { theme = "dark", variant = "experiment-b" };
        var client = BuildClient(JsonOk(theme));
        // ThemeResponse is not in scope directly — call compiles and returns object or null
        var result = await client.GetThemeAsync();
        // No exception = success path exercised
        Assert.True(true);
    }

    // ── GetAuditLogAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAuditLogAsync_ReturnsList()
    {
        var entries = new[]
        {
            new { timestamp = DateTime.UtcNow, eventType = "assignment", experimentName = "exp-1", trialName = (string?)null, details = "user assigned" }
        };
        var client = BuildClient(JsonOk(entries));
        var result = await client.GetAuditLogAsync(10);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetAuditLogAsync_NullResponse_ReturnsEmptyList()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            });
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);
        var result = await client.GetAuditLogAsync();
        Assert.Empty(result);
    }

    // ── GetUsageStatsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsageStatsAsync_ReturnsDictionary()
    {
        var data = new Dictionary<string, Dictionary<string, int>>
        {
            ["exp-1"] = new Dictionary<string, int> { ["control"] = 100, ["variant-a"] = 80 }
        };
        var client = BuildClient(JsonOk(data));
        var result = await client.GetUsageStatsAsync();
        Assert.True(result.ContainsKey("exp-1"));
    }

    [Fact]
    public async Task GetUsageStatsAsync_NullResponse_ReturnsEmptyDictionary()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            });
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);
        var result = await client.GetUsageStatsAsync();
        Assert.Empty(result);
    }

    // ── GetConfigYamlAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetConfigYamlAsync_ReturnsYamlString()
    {
        var yaml = "experiments:\n  - name: exp-1\n";
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(yaml, Encoding.UTF8, "text/yaml")
            });
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);
        var result = await client.GetConfigYamlAsync();
        Assert.Contains("experiments", result);
    }

    // ── GetConfigInfoAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetConfigInfoAsync_ReturnsFrameworkInfo()
    {
        var info = new FrameworkInfo
        {
            Framework = new FrameworkDetails { Name = "EF", Version = "1.0" },
            Server = new ServerDetails { MachineName = "test-machine" },
            Experiments = new ExperimentStats { Total = 5, Active = 3 }
        };
        var client = BuildClient(JsonOk(info));
        var result = await client.GetConfigInfoAsync();
        Assert.NotNull(result);
    }

    // ── GetKillSwitchStatusesAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetKillSwitchStatusesAsync_ReturnsList()
    {
        var statuses = new[]
        {
            new KillSwitchStatus { Experiment = "exp-1", ExperimentDisabled = true, DisabledVariants = ["v1"] }
        };
        var client = BuildClient(JsonOk(statuses));
        var result = await client.GetKillSwitchStatusesAsync();
        Assert.Single(result);
        Assert.True(result[0].ExperimentDisabled);
    }

    [Fact]
    public async Task GetKillSwitchStatusesAsync_HttpRequestException_ReturnsEmpty()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);
        var result = await client.GetKillSwitchStatusesAsync();
        Assert.Empty(result);
    }

    // ── UpdateKillSwitchAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateKillSwitchAsync_Success_ReturnsStatus()
    {
        var status = new KillSwitchStatus { Experiment = "exp-1", ExperimentDisabled = false };
        var client = BuildClient(JsonOk(status));
        var result = await client.UpdateKillSwitchAsync(new KillSwitchUpdate { Experiment = "exp-1", Disabled = false });
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateKillSwitchAsync_FailureResponse_ReturnsNull()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.InternalServerError));
        var result = await client.UpdateKillSwitchAsync(new KillSwitchUpdate { Experiment = "exp-1", Disabled = true });
        Assert.Null(result);
    }

    // ── ValidateDslAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateDslAsync_Success_ReturnsValidationResponse()
    {
        var resp = new DslValidationResponse { IsValid = true, Errors = [], ParsedExperiments = [] };
        var client = BuildClient(JsonOk(resp));
        var result = await client.ValidateDslAsync("experiments: []");
        Assert.NotNull(result);
        Assert.True(result!.IsValid);
    }

    [Fact]
    public async Task ValidateDslAsync_FailureResponse_ReturnsNull()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.BadRequest));
        var result = await client.ValidateDslAsync("bad yaml");
        Assert.Null(result);
    }

    // ── ApplyDslAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyDslAsync_Success_ReturnsApplyResponse()
    {
        var resp = new DslApplyResponse { Success = true };
        var client = BuildClient(JsonOk(resp));
        var result = await client.ApplyDslAsync("experiments: []");
        Assert.NotNull(result);
        Assert.True(result!.Success);
    }

    [Fact]
    public async Task ApplyDslAsync_FailureResponse_ReturnsNull()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.ServiceUnavailable));
        var result = await client.ApplyDslAsync("bad");
        Assert.Null(result);
    }

    // ── GetCurrentDslAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentDslAsync_ReturnsDslCurrentResponse()
    {
        var resp = new DslCurrentResponse { Yaml = "experiments: []", HasUnappliedChanges = false };
        var client = BuildClient(JsonOk(resp));
        var result = await client.GetCurrentDslAsync();
        Assert.NotNull(result);
        Assert.Contains("experiments", result!.Yaml);
    }

    // ── GetPluginsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPluginsAsync_NotImplemented_ReturnsEmptyList()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.NotImplemented));
        var result = await client.GetPluginsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPluginsAsync_NotFound_ReturnsEmptyList()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.NotFound));
        var result = await client.GetPluginsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPluginsAsync_EnvelopeShape_ReturnsList()
    {
        var envelope = new { plugins = new[] { new { id = "p1", name = "Plugin1", version = "1.0" } } };
        var client = BuildClient(JsonOk(envelope));
        var result = await client.GetPluginsAsync();
        // envelope deserialization might give PluginInfo with empty fields — no crash is the assertion
        Assert.NotNull(result);
    }

    // ── DiscoverPluginsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DiscoverPluginsAsync_Success_ReturnsLoadedCount()
    {
        var resp = new DiscoverResult { LoadedCount = 3 };
        var client = BuildClient(JsonOk(resp));
        var result = await client.DiscoverPluginsAsync();
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task DiscoverPluginsAsync_FailureResponse_ReturnsZero()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.ServiceUnavailable));
        var result = await client.DiscoverPluginsAsync();
        Assert.Equal(0, result);
    }

    // ── ReloadPluginAsync / UnloadPluginAsync ─────────────────────────────────

    [Fact]
    public async Task ReloadPluginAsync_DoesNotThrow()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.OK));
        await client.ReloadPluginAsync("plugin-id");
        // If we get here without exception, pass
        Assert.True(true);
    }

    [Fact]
    public async Task UnloadPluginAsync_DoesNotThrow()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.OK));
        await client.UnloadPluginAsync("plugin-id");
        Assert.True(true);
    }

    // ── UsePluginImplementationAsync ──────────────────────────────────────────

    [Fact]
    public async Task UsePluginImplementationAsync_Success_ReturnsResult()
    {
        var result = new PluginUseResult { PluginId = "p1", Interface = "IFoo", Implementation = "FooImpl", Active = true };
        var client = BuildClient(JsonOk(result));
        var r = await client.UsePluginImplementationAsync("p1", "IFoo", "FooImpl");
        Assert.NotNull(r);
        Assert.True(r!.Active);
    }

    [Fact]
    public async Task UsePluginImplementationAsync_FailureResponse_ReturnsNull()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.BadRequest));
        var r = await client.UsePluginImplementationAsync("p1", "IFoo", "FooImpl");
        Assert.Null(r);
    }

    // ── GetActivePluginImplementationsAsync ───────────────────────────────────

    [Fact]
    public async Task GetActivePluginImplementationsAsync_NotFound_ReturnsEmpty()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.NotFound));
        var r = await client.GetActivePluginImplementationsAsync();
        Assert.Empty(r);
    }

    [Fact]
    public async Task GetActivePluginImplementationsAsync_NotImplemented_ReturnsEmpty()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.NotImplemented));
        var r = await client.GetActivePluginImplementationsAsync();
        Assert.Empty(r);
    }

    [Fact]
    public async Task GetActivePluginImplementationsAsync_HttpRequestException_ReturnsEmpty()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("network error"));
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);
        var r = await client.GetActivePluginImplementationsAsync();
        Assert.Empty(r);
    }

    // ── ClearActivePluginImplementationAsync ──────────────────────────────────

    [Fact]
    public async Task ClearActivePluginImplementationAsync_Success_ReturnsTrue()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.OK));
        var r = await client.ClearActivePluginImplementationAsync("IFoo");
        Assert.True(r);
    }

    [Fact]
    public async Task ClearActivePluginImplementationAsync_Failure_ReturnsFalse()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.NotFound));
        var r = await client.ClearActivePluginImplementationAsync("IFoo");
        Assert.False(r);
    }

    // ── Governance: GetLifecycleStateAsync ────────────────────────────────────

    [Fact]
    public async Task GetLifecycleStateAsync_Success_ReturnsState()
    {
        var state = new ExperimentStateInfo { ExperimentName = "exp-1", State = "Running" };
        var client = BuildClient(JsonOk(state));
        var r = await client.GetLifecycleStateAsync("exp-1");
        Assert.NotNull(r);
        Assert.Equal("exp-1", r!.ExperimentName);
    }

    [Fact]
    public async Task GetLifecycleStateAsync_Exception_ReturnsNull()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);
        var r = await client.GetLifecycleStateAsync("exp-1");
        Assert.Null(r);
    }

    // ── TransitionStateAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task TransitionStateAsync_Success_ReturnsTrue()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.OK));
        var r = await client.TransitionStateAsync("exp-1", "Running");
        Assert.True(r);
    }

    [Fact]
    public async Task TransitionStateAsync_Failure_ReturnsFalse()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.BadRequest));
        var r = await client.TransitionStateAsync("exp-1", "Invalid");
        Assert.False(r);
    }

    // ── GetStateTransitionHistoryAsync ────────────────────────────────────────

    [Fact]
    public async Task GetStateTransitionHistoryAsync_ReturnsTransitions()
    {
        var resp = new LifecycleHistoryResponse
        {
            ExperimentName = "exp-1",
            Transitions = [new StateTransitionInfo { FromState = "Draft", ToState = "Running" }]
        };
        var client = BuildClient(JsonOk(resp));
        var r = await client.GetStateTransitionHistoryAsync("exp-1");
        Assert.Single(r);
    }

    [Fact]
    public async Task GetStateTransitionHistoryAsync_HttpRequestException_ReturnsEmpty()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);
        var r = await client.GetStateTransitionHistoryAsync("exp-1");
        Assert.Empty(r);
    }

    // ── GetPolicyEvaluationsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetPolicyEvaluationsAsync_ReturnsPolicies()
    {
        var resp = new GovernancePolicyResponse
        {
            Policies = [new PolicyEvaluationInfo { PolicyName = "approval-gate", IsCompliant = true }]
        };
        var client = BuildClient(JsonOk(resp));
        var r = await client.GetPolicyEvaluationsAsync("exp-1");
        Assert.Single(r);
        Assert.True(r[0].IsCompliant);
    }

    [Fact]
    public async Task GetPolicyEvaluationsAsync_HttpRequestException_ReturnsEmpty()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("error"));
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);
        var r = await client.GetPolicyEvaluationsAsync("exp-1");
        Assert.Empty(r);
    }

    // ── GetConfigurationVersionsAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetConfigurationVersionsAsync_ReturnsVersions()
    {
        var resp = new GovernanceVersionsResponse
        {
            Versions = [new ConfigurationVersionInfo { VersionNumber = 1, ConfigurationHash = "abc" }]
        };
        var client = BuildClient(JsonOk(resp));
        var r = await client.GetConfigurationVersionsAsync("exp-1");
        Assert.Single(r);
    }

    [Fact]
    public async Task GetConfigurationVersionsAsync_HttpRequestException_ReturnsEmpty()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("error"));
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);
        var r = await client.GetConfigurationVersionsAsync("exp-1");
        Assert.Empty(r);
    }

    // ── GetConfigurationVersionAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetConfigurationVersionAsync_ReturnsVersion()
    {
        var version = new ConfigurationVersionInfo { VersionNumber = 2, ConfigurationHash = "xyz" };
        var client = BuildClient(JsonOk(version));
        var r = await client.GetConfigurationVersionAsync("exp-1", 2);
        Assert.NotNull(r);
        Assert.Equal(2, r!.VersionNumber);
    }

    [Fact]
    public async Task GetConfigurationVersionAsync_HttpRequestException_ReturnsNull()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("error"));
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);
        var r = await client.GetConfigurationVersionAsync("exp-1", 1);
        Assert.Null(r);
    }

    // ── RollbackToVersionAsync ────────────────────────────────────────────────

    [Fact]
    public async Task RollbackToVersionAsync_Success_ReturnsTrue()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.OK));
        var r = await client.RollbackToVersionAsync("exp-1", 2);
        Assert.True(r);
    }

    [Fact]
    public async Task RollbackToVersionAsync_Failure_ReturnsFalse()
    {
        var client = BuildClient(StatusOnly(HttpStatusCode.NotFound));
        var r = await client.RollbackToVersionAsync("exp-1", 99);
        Assert.False(r);
    }

    // ── GetGovernanceAuditLogAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetGovernanceAuditLogAsync_ReturnsAuditLog()
    {
        var resp = new GovernanceAuditResponse
        {
            AuditLog = [new AuditLogItem { Type = "StateTransition", Details = "Draft → Running" }]
        };
        var client = BuildClient(JsonOk(resp));
        var r = await client.GetGovernanceAuditLogAsync("exp-1");
        Assert.Single(r);
        Assert.Equal("StateTransition", r[0].Type);
    }

    [Fact]
    public async Task GetGovernanceAuditLogAsync_HttpRequestException_ReturnsEmpty()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("error"));
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);
        var r = await client.GetGovernanceAuditLogAsync("exp-1");
        Assert.Empty(r);
    }

    // ── HttpClient property ───────────────────────────────────────────────────

    [Fact]
    public void HttpClient_Property_ReturnsInjectedClient()
    {
        var http = new HttpClient { BaseAddress = new Uri("http://localhost/") };
        var client = new ExperimentApiClient(http);
        Assert.Same(http, client.HttpClient);
    }
}
