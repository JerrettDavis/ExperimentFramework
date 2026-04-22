using ExperimentFramework.Admin;
using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Approval;
using ExperimentFramework.Governance.Policy;
using ExperimentFramework.Governance.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Json;

namespace ExperimentFramework.Tests.Admin;

public sealed class GovernanceAdminEndpointsTests : IAsyncDisposable
{
    // ───────────────────────── App factory ─────────────────────────

    private sealed class GovernanceTestApp : IAsyncDisposable
    {
        private readonly WebApplication _app;
        public HttpClient Client { get; }

        public GovernanceTestApp(
            ILifecycleManager? lifecycleManager = null,
            IVersionManager? versionManager = null,
            IPolicyEvaluator? policyEvaluator = null,
            IApprovalManager? approvalManager = null)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();

            if (lifecycleManager != null)
                builder.Services.AddSingleton(lifecycleManager);
            if (versionManager != null)
                builder.Services.AddSingleton(versionManager);
            if (policyEvaluator != null)
                builder.Services.AddSingleton(policyEvaluator);
            if (approvalManager != null)
                builder.Services.AddSingleton(approvalManager);

            _app = builder.Build();
            _app.MapGovernanceAdminApi();
            _app.Start();
            Client = _app.GetTestClient();
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.DisposeAsync();
        }
    }

    // ─────────────────────── Lifecycle: no manager ───────────────────────

    [Fact]
    public async Task GetLifecycleState_Returns404_WhenNoManager()
    {
        await using var app = new GovernanceTestApp();

        var response = await app.Client.GetAsync("/api/governance/exp1/lifecycle/state");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLifecycleHistory_Returns404_WhenNoManager()
    {
        await using var app = new GovernanceTestApp();

        var response = await app.Client.GetAsync("/api/governance/exp1/lifecycle/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllowedTransitions_Returns404_WhenNoManager()
    {
        await using var app = new GovernanceTestApp();

        var response = await app.Client.GetAsync("/api/governance/exp1/lifecycle/allowed-transitions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TransitionLifecycleState_Returns404_WhenNoManager()
    {
        await using var app = new GovernanceTestApp();

        var response = await app.Client.PostAsJsonAsync("/api/governance/exp1/lifecycle/transition",
            new { targetState = "PendingApproval", actor = (string?)null, reason = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─────────────────────── Lifecycle: state does not exist ───────────────────────

    [Fact]
    public async Task GetLifecycleState_Returns404_WhenStateNotFound()
    {
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance);
        await using var app = new GovernanceTestApp(lifecycleManager: manager);

        var response = await app.Client.GetAsync("/api/governance/unknown-exp/lifecycle/state");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─────────────────────── Lifecycle: with state ───────────────────────

    [Fact]
    public async Task GetLifecycleState_ReturnsState_WhenFound()
    {
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance);
        await manager.TransitionAsync("my-exp", ExperimentLifecycleState.PendingApproval);
        await using var app = new GovernanceTestApp(lifecycleManager: manager);

        var response = await app.Client.GetAsync("/api/governance/my-exp/lifecycle/state");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("PendingApproval", content);
    }

    [Fact]
    public async Task GetLifecycleHistory_ReturnsHistory()
    {
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance);
        await manager.TransitionAsync("hist-exp", ExperimentLifecycleState.PendingApproval, actor: "alice");
        await using var app = new GovernanceTestApp(lifecycleManager: manager);

        var response = await app.Client.GetAsync("/api/governance/hist-exp/lifecycle/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("hist-exp", content);
    }

    [Fact]
    public async Task GetAllowedTransitions_ReturnsTransitions()
    {
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance);
        await using var app = new GovernanceTestApp(lifecycleManager: manager);

        var response = await app.Client.GetAsync("/api/governance/new-exp/lifecycle/allowed-transitions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("PendingApproval", content);
    }

    // ─────────────────────── Transition request ───────────────────────

    [Fact]
    public async Task TransitionLifecycleState_Returns400_ForInvalidTargetState()
    {
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance);
        await using var app = new GovernanceTestApp(lifecycleManager: manager);

        var response = await app.Client.PostAsJsonAsync("/api/governance/exp/lifecycle/transition",
            new { targetState = "NotARealState", actor = (string?)null, reason = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid target state", content);
    }

    [Fact]
    public async Task TransitionLifecycleState_Returns400_ForInvalidTransition()
    {
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance);
        await using var app = new GovernanceTestApp(lifecycleManager: manager);

        // Draft -> Running is not valid
        var response = await app.Client.PostAsJsonAsync("/api/governance/exp/lifecycle/transition",
            new { targetState = "Running", actor = (string?)null, reason = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TransitionLifecycleState_Returns200_ForValidTransition()
    {
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance);
        await using var app = new GovernanceTestApp(lifecycleManager: manager);

        var response = await app.Client.PostAsJsonAsync("/api/governance/exp/lifecycle/transition",
            new { targetState = "PendingApproval", actor = "alice", reason = "ready" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("PendingApproval", content);
    }

    // ─────────────────────── Versions: no manager ───────────────────────

    [Fact]
    public async Task GetVersions_Returns404_WhenNoManager()
    {
        await using var app = new GovernanceTestApp();

        var response = await app.Client.GetAsync("/api/governance/exp/versions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestVersion_Returns404_WhenNoManager()
    {
        await using var app = new GovernanceTestApp();

        var response = await app.Client.GetAsync("/api/governance/exp/versions/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetVersion_Returns404_WhenNoManager()
    {
        await using var app = new GovernanceTestApp();

        var response = await app.Client.GetAsync("/api/governance/exp/versions/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetVersionDiff_Returns404_WhenNoManager()
    {
        await using var app = new GovernanceTestApp();

        var response = await app.Client.GetAsync("/api/governance/exp/versions/diff?fromVersion=1&toVersion=2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateVersion_Returns404_WhenNoManager()
    {
        await using var app = new GovernanceTestApp();

        var response = await app.Client.PostAsJsonAsync("/api/governance/exp/versions",
            new { configuration = new { }, actor = (string?)null, changeDescription = (string?)null, lifecycleState = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RollbackVersion_Returns404_WhenNoManager()
    {
        await using var app = new GovernanceTestApp();

        var response = await app.Client.PostAsJsonAsync("/api/governance/exp/versions/rollback",
            new { targetVersion = 1, actor = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─────────────────────── Versions: with manager ───────────────────────

    [Fact]
    public async Task GetVersions_ReturnsVersionList()
    {
        var manager = new VersionManager(NullLogger<VersionManager>.Instance);
        await manager.CreateVersionAsync("exp-v", new { x = 1 });
        await using var app = new GovernanceTestApp(versionManager: manager);

        var response = await app.Client.GetAsync("/api/governance/exp-v/versions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("exp-v", content);
    }

    [Fact]
    public async Task GetLatestVersion_Returns404_WhenNoVersionsExist()
    {
        var manager = new VersionManager(NullLogger<VersionManager>.Instance);
        await using var app = new GovernanceTestApp(versionManager: manager);

        var response = await app.Client.GetAsync("/api/governance/no-versions-exp/versions/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestVersion_ReturnsVersion_WhenVersionExists()
    {
        var manager = new VersionManager(NullLogger<VersionManager>.Instance);
        await manager.CreateVersionAsync("ver-exp", new { setting = "value" });
        await using var app = new GovernanceTestApp(versionManager: manager);

        var response = await app.Client.GetAsync("/api/governance/ver-exp/versions/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ver-exp", content);
    }

    [Fact]
    public async Task GetVersion_Returns404_WhenVersionNotFound()
    {
        var manager = new VersionManager(NullLogger<VersionManager>.Instance);
        await manager.CreateVersionAsync("my-exp", new { });
        await using var app = new GovernanceTestApp(versionManager: manager);

        var response = await app.Client.GetAsync("/api/governance/my-exp/versions/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetVersion_ReturnsVersion_WhenFound()
    {
        var manager = new VersionManager(NullLogger<VersionManager>.Instance);
        await manager.CreateVersionAsync("my-exp2", new { x = 1 });
        await using var app = new GovernanceTestApp(versionManager: manager);

        var response = await app.Client.GetAsync("/api/governance/my-exp2/versions/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVersionDiff_Returns404_WhenVersionsNotFound()
    {
        var manager = new VersionManager(NullLogger<VersionManager>.Instance);
        await using var app = new GovernanceTestApp(versionManager: manager);

        var response = await app.Client.GetAsync("/api/governance/exp/versions/diff?fromVersion=1&toVersion=2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetVersionDiff_ReturnsDiff_WhenBothVersionsExist()
    {
        var manager = new VersionManager(NullLogger<VersionManager>.Instance);
        await manager.CreateVersionAsync("diff-exp", new { x = "v1" });
        await manager.CreateVersionAsync("diff-exp", new { x = "v2" });
        await using var app = new GovernanceTestApp(versionManager: manager);

        var response = await app.Client.GetAsync("/api/governance/diff-exp/versions/diff?fromVersion=1&toVersion=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateVersion_Returns201_WithValidRequest()
    {
        var manager = new VersionManager(NullLogger<VersionManager>.Instance);
        await using var app = new GovernanceTestApp(versionManager: manager);

        var response = await app.Client.PostAsJsonAsync("/api/governance/new-exp/versions",
            new { configuration = new { setting = "test" }, actor = "alice", changeDescription = "first", lifecycleState = (string?)null });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateVersion_WithValidLifecycleState_Returns201()
    {
        var manager = new VersionManager(NullLogger<VersionManager>.Instance);
        await using var app = new GovernanceTestApp(versionManager: manager);

        var response = await app.Client.PostAsJsonAsync("/api/governance/new-exp2/versions",
            new { configuration = new { }, actor = (string?)null, changeDescription = (string?)null, lifecycleState = "Running" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task RollbackVersion_Returns400_WhenTargetVersionNotFound()
    {
        var manager = new VersionManager(NullLogger<VersionManager>.Instance);
        await using var app = new GovernanceTestApp(versionManager: manager);

        var response = await app.Client.PostAsJsonAsync("/api/governance/no-versions/versions/rollback",
            new { targetVersion = 1, actor = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RollbackVersion_Returns200_WhenVersionExists()
    {
        var manager = new VersionManager(NullLogger<VersionManager>.Instance);
        await manager.CreateVersionAsync("rollback-exp", new { v = 1 });
        await manager.CreateVersionAsync("rollback-exp", new { v = 2 });
        await using var app = new GovernanceTestApp(versionManager: manager);

        var response = await app.Client.PostAsJsonAsync("/api/governance/rollback-exp/versions/rollback",
            new { targetVersion = 1, actor = "admin" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("rollback-exp", content);
    }

    // ─────────────────────── Policy evaluation ───────────────────────

    [Fact]
    public async Task EvaluatePolicies_Returns404_WhenNoEvaluator()
    {
        await using var app = new GovernanceTestApp();

        var response = await app.Client.PostAsJsonAsync("/api/governance/exp/policies/evaluate",
            new { currentState = (string?)null, targetState = (string?)null, telemetry = (object?)null, metadata = (object?)null });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EvaluatePolicies_Returns200_WithNoRegisteredPolicies()
    {
        var evaluator = new PolicyEvaluator(Microsoft.Extensions.Logging.Abstractions.NullLogger<PolicyEvaluator>.Instance);
        await using var app = new GovernanceTestApp(policyEvaluator: evaluator);

        var response = await app.Client.PostAsJsonAsync("/api/governance/exp/policies/evaluate",
            new { currentState = (string?)null, targetState = (string?)null, telemetry = (object?)null, metadata = (object?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("allCriticalPoliciesCompliant", content);
    }

    [Fact]
    public async Task EvaluatePolicies_ParsesCurrentAndTargetStates()
    {
        var evaluator = new PolicyEvaluator(Microsoft.Extensions.Logging.Abstractions.NullLogger<PolicyEvaluator>.Instance);
        await using var app = new GovernanceTestApp(policyEvaluator: evaluator);

        var response = await app.Client.PostAsJsonAsync("/api/governance/exp/policies/evaluate",
            new { currentState = "Draft", targetState = "PendingApproval", telemetry = (object?)null, metadata = (object?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─────────────────────── Approval evaluation ───────────────────────

    [Fact]
    public async Task EvaluateApprovals_Returns404_WhenNoManager()
    {
        await using var app = new GovernanceTestApp();

        var response = await app.Client.PostAsJsonAsync("/api/governance/exp/approvals/evaluate",
            new { currentState = "Draft", targetState = "PendingApproval", actor = (string?)null, reason = (string?)null, metadata = (object?)null });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EvaluateApprovals_Returns400_ForInvalidCurrentState()
    {
        var manager = new ApprovalManager();
        await using var app = new GovernanceTestApp(approvalManager: manager);

        var response = await app.Client.PostAsJsonAsync("/api/governance/exp/approvals/evaluate",
            new { currentState = "NotAState", targetState = "PendingApproval", actor = (string?)null, reason = (string?)null, metadata = (object?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid current state", content);
    }

    [Fact]
    public async Task EvaluateApprovals_Returns400_ForInvalidTargetState()
    {
        var manager = new ApprovalManager();
        await using var app = new GovernanceTestApp(approvalManager: manager);

        var response = await app.Client.PostAsJsonAsync("/api/governance/exp/approvals/evaluate",
            new { currentState = "Draft", targetState = "NotAState", actor = (string?)null, reason = (string?)null, metadata = (object?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid target state", content);
    }

    [Fact]
    public async Task EvaluateApprovals_Returns200_WithValidRequest()
    {
        var manager = new ApprovalManager();
        manager.RegisterGate(null, ExperimentLifecycleState.PendingApproval, new AutomaticApprovalGate());
        await using var app = new GovernanceTestApp(approvalManager: manager);

        var response = await app.Client.PostAsJsonAsync("/api/governance/exp/approvals/evaluate",
            new { currentState = "Draft", targetState = "PendingApproval", actor = "alice", reason = "test", metadata = (object?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("isApproved", content);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
