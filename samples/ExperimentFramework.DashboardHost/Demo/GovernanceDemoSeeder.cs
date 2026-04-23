using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Persistence;
using ExperimentFramework.Governance.Persistence.Models;
using ExperimentFramework.Governance.Policy;

namespace ExperimentFramework.DashboardHost.Demo;

/// <summary>
/// Seeds governance demo data into an <see cref="IGovernancePersistenceBackplane"/>:
/// 5 experiment states, ~20 state transitions, 3 approval records, 8 configuration
/// versions, and policy evaluations (including 1 fail on pricing-page-copy).
/// </summary>
/// <remarks>
/// Deltas vs plan sketch:
/// - PersistedExperimentState: "ETag" is required; supplied as deterministic string.
/// - PersistedStateTransition: "TransitionId" is required; supplied as deterministic string.
///   "Timestamp" used (plan said "OccurredAt").
/// - PersistedApprovalRecord: "ApprovalId", "TransitionId", "GateName", "IsApproved" (bool)
///   all required. No "Status" enum — a pending approval is represented as IsApproved=false
///   with GateName = "require-two-approvers".
/// - PersistedConfigurationVersion: "VersionNumber" (not "Version"), "ChangeDescription"
///   (not "Notes"), plus required "ConfigurationJson" and "ConfigurationHash".
/// - PersistedPolicyEvaluation: "EvaluationId" required; "IsCompliant" (not "Passed");
///   "Severity" (PolicyViolationSeverity) is required.
/// - TenantId left null throughout; Environment = "demo" for all records.
/// </remarks>
public static class GovernanceDemoSeeder
{
    // Environment is left unset here (empty/null) so the InMemoryGovernance
    // persistence backplane stores records under the same composite key that
    // the dashboard API queries with (TenantId=null, Environment=null).
    // Previously Environment="demo" caused the seeded rows to live under
    // "checkout-button-v2::demo" while the endpoint looked up
    // "checkout-button-v2" — returning empty version history in every UI call.
    private const string DefaultEnvironment = "";
    private const string DefaultActor       = "admin@experimentdemo.com";

    public static async Task SeedAsync(
        IGovernancePersistenceBackplane backplane,
        DateTimeOffset frozenNow,
        CancellationToken ct = default)
    {
        await SeedExperimentStatesAsync(backplane, frozenNow, ct);
        await SeedStateTransitionsAsync(backplane, frozenNow, ct);
        await SeedApprovalRecordsAsync(backplane, frozenNow, ct);
        await SeedConfigurationVersionsAsync(backplane, frozenNow, ct);
        await SeedPolicyEvaluationsAsync(backplane, frozenNow, ct);
    }

    // -------------------------------------------------------------------------
    // Experiment States (5 records)
    // -------------------------------------------------------------------------

    private static async Task SeedExperimentStatesAsync(
        IGovernancePersistenceBackplane backplane,
        DateTimeOffset frozenNow,
        CancellationToken ct)
    {
        var states = new[]
        {
            new PersistedExperimentState
            {
                ExperimentName   = "checkout-button-v2",
                CurrentState     = ExperimentLifecycleState.Running,
                ConfigurationVersion = 2,
                LastModified     = frozenNow.AddDays(-14),
                LastModifiedBy   = DefaultActor,
                ETag             = "etag-checkout-button-v2-v2",
                Environment      = DefaultEnvironment,
                Metadata         = new Dictionary<string, object> { ["rolloutPct"] = 50 },
            },
            new PersistedExperimentState
            {
                ExperimentName   = "search-ranker-ml",
                CurrentState     = ExperimentLifecycleState.Running,
                ConfigurationVersion = 2,
                LastModified     = frozenNow.AddDays(-1),
                LastModifiedBy   = DefaultActor,
                ETag             = "etag-search-ranker-ml-v2",
                Environment      = DefaultEnvironment,
                Metadata         = new Dictionary<string, object> { ["rolloutPct"] = 10 },
            },
            new PersistedExperimentState
            {
                ExperimentName   = "homepage-layout-fall2026",
                CurrentState     = ExperimentLifecycleState.PendingApproval,
                ConfigurationVersion = 0,
                LastModified     = frozenNow.AddDays(-1),
                LastModifiedBy   = "experimenter@experimentdemo.com",
                ETag             = "etag-homepage-layout-fall2026-v0",
                Environment      = DefaultEnvironment,
            },
            new PersistedExperimentState
            {
                ExperimentName   = "pricing-page-copy",
                CurrentState     = ExperimentLifecycleState.Paused,
                ConfigurationVersion = 2,
                LastModified     = frozenNow.AddDays(-2),
                LastModifiedBy   = DefaultActor,
                ETag             = "etag-pricing-page-copy-v2",
                Environment      = DefaultEnvironment,
                Metadata         = new Dictionary<string, object> { ["pauseReason"] = "Insufficient sample size" },
            },
            new PersistedExperimentState
            {
                ExperimentName   = "legacy-api-cutover",
                CurrentState     = ExperimentLifecycleState.Archived,
                ConfigurationVersion = 2,
                LastModified     = frozenNow.AddDays(-30),
                LastModifiedBy   = DefaultActor,
                ETag             = "etag-legacy-api-cutover-v2",
                Environment      = DefaultEnvironment,
            },
        };

        foreach (var state in states)
        {
            await backplane.SaveExperimentStateAsync(state, expectedETag: null, ct);
        }
    }

    // -------------------------------------------------------------------------
    // State Transitions (~20 events across 5 experiments)
    // -------------------------------------------------------------------------

    private static async Task SeedStateTransitionsAsync(
        IGovernancePersistenceBackplane backplane,
        DateTimeOffset frozenNow,
        CancellationToken ct)
    {
        var transitions = new[]
        {
            // checkout-button-v2: Draft → PendingApproval → Approved → Running → Running (rollout 50%)
            Transition("t-cbv2-01", "checkout-button-v2",
                ExperimentLifecycleState.Draft,           ExperimentLifecycleState.PendingApproval,
                frozenNow.AddDays(-22), "Submitted for approval before launch."),
            Transition("t-cbv2-02", "checkout-button-v2",
                ExperimentLifecycleState.PendingApproval, ExperimentLifecycleState.Approved,
                frozenNow.AddDays(-21), "Two approvers signed off."),
            Transition("t-cbv2-03", "checkout-button-v2",
                ExperimentLifecycleState.Approved,        ExperimentLifecycleState.Running,
                frozenNow.AddDays(-21), "Initial 10% rollout."),
            Transition("t-cbv2-04", "checkout-button-v2",
                ExperimentLifecycleState.Running,         ExperimentLifecycleState.Ramping,
                frozenNow.AddDays(-14), "First-week data good; ramping to 50%."),
            Transition("t-cbv2-05", "checkout-button-v2",
                ExperimentLifecycleState.Ramping,         ExperimentLifecycleState.Running,
                frozenNow.AddDays(-14), "Ramp complete — stable at 50%."),

            // search-ranker-ml: Draft → Running (faster track, no ramp)
            Transition("t-srml-01", "search-ranker-ml",
                ExperimentLifecycleState.Draft,           ExperimentLifecycleState.PendingApproval,
                frozenNow.AddDays(-9), "Submitted for approval."),
            Transition("t-srml-02", "search-ranker-ml",
                ExperimentLifecycleState.PendingApproval, ExperimentLifecycleState.Approved,
                frozenNow.AddDays(-8), "Approved — single-approver fast track."),
            Transition("t-srml-03", "search-ranker-ml",
                ExperimentLifecycleState.Approved,        ExperimentLifecycleState.Running,
                frozenNow.AddDays(-7), "Running at 10% for baseline comparison."),

            // homepage-layout-fall2026: Draft → PendingApproval (still pending)
            Transition("t-hlf26-01", "homepage-layout-fall2026",
                ExperimentLifecycleState.Draft,           ExperimentLifecycleState.PendingApproval,
                frozenNow.AddDays(-1), "Requesting approval to launch Fall 2026 hero test."),

            // pricing-page-copy: Draft → Running → Paused
            Transition("t-ppc-01", "pricing-page-copy",
                ExperimentLifecycleState.Draft,           ExperimentLifecycleState.PendingApproval,
                frozenNow.AddDays(-31), "Submitted for approval."),
            Transition("t-ppc-02", "pricing-page-copy",
                ExperimentLifecycleState.PendingApproval, ExperimentLifecycleState.Approved,
                frozenNow.AddDays(-30), "Approved."),
            Transition("t-ppc-03", "pricing-page-copy",
                ExperimentLifecycleState.Approved,        ExperimentLifecycleState.Running,
                frozenNow.AddDays(-30), "Initial 10% rollout."),
            Transition("t-ppc-04", "pricing-page-copy",
                ExperimentLifecycleState.Running,         ExperimentLifecycleState.Ramping,
                frozenNow.AddDays(-10), "Scaled to 25%."),
            Transition("t-ppc-05", "pricing-page-copy",
                ExperimentLifecycleState.Ramping,         ExperimentLifecycleState.Paused,
                frozenNow.AddDays(-2),
                "Paused: min-sample-size-1000 policy violation — control arm had only 712 samples."),

            // legacy-api-cutover: full lifecycle through to Archived
            Transition("t-lac-01", "legacy-api-cutover",
                ExperimentLifecycleState.Draft,           ExperimentLifecycleState.PendingApproval,
                frozenNow.AddDays(-62), "Submitted for approval."),
            Transition("t-lac-02", "legacy-api-cutover",
                ExperimentLifecycleState.PendingApproval, ExperimentLifecycleState.Approved,
                frozenNow.AddDays(-61), "Approved."),
            Transition("t-lac-03", "legacy-api-cutover",
                ExperimentLifecycleState.Approved,        ExperimentLifecycleState.Running,
                frozenNow.AddDays(-60), "Initial 10% rollout."),
            Transition("t-lac-04", "legacy-api-cutover",
                ExperimentLifecycleState.Running,         ExperimentLifecycleState.Ramping,
                frozenNow.AddDays(-45), "Ramping to full rollout."),
            Transition("t-lac-05", "legacy-api-cutover",
                ExperimentLifecycleState.Ramping,         ExperimentLifecycleState.Running,
                frozenNow.AddDays(-30), "Fully promoted — variant is now the default."),
            Transition("t-lac-06", "legacy-api-cutover",
                ExperimentLifecycleState.Running,         ExperimentLifecycleState.Archived,
                frozenNow.AddDays(-30), "Archived after successful promotion."),
        };

        foreach (var t in transitions)
        {
            await backplane.AppendStateTransitionAsync(t, ct);
        }
    }

    private static PersistedStateTransition Transition(
        string id,
        string experimentName,
        ExperimentLifecycleState from,
        ExperimentLifecycleState to,
        DateTimeOffset timestamp,
        string reason) =>
        new()
        {
            TransitionId   = id,
            ExperimentName = experimentName,
            FromState      = from,
            ToState        = to,
            Timestamp      = timestamp,
            Actor          = DefaultActor,
            Reason         = reason,
            Environment    = DefaultEnvironment,
        };

    // -------------------------------------------------------------------------
    // Approval Records
    // -------------------------------------------------------------------------

    private static async Task SeedApprovalRecordsAsync(
        IGovernancePersistenceBackplane backplane,
        DateTimeOffset frozenNow,
        CancellationToken ct)
    {
        // Pending approval: homepage-layout-fall2026 awaiting two approvers.
        // IsApproved=false models an unanswered / pending record.
        await backplane.AppendApprovalRecordAsync(new PersistedApprovalRecord
        {
            ApprovalId     = "apr-hlf26-01",
            ExperimentName = "homepage-layout-fall2026",
            TransitionId   = "t-hlf26-01",
            FromState      = ExperimentLifecycleState.Draft,
            ToState        = ExperimentLifecycleState.PendingApproval,
            IsApproved     = false,
            Approver       = null,          // no decision yet
            Reason         = "Requesting approval to launch Fall 2026 hero test.",
            Timestamp      = frozenNow.AddDays(-1),
            GateName       = "require-two-approvers",
            Environment    = DefaultEnvironment,
        }, ct);

        // Approved records for checkout-button-v2 (two approvers)
        await backplane.AppendApprovalRecordAsync(new PersistedApprovalRecord
        {
            ApprovalId     = "apr-cbv2-01",
            ExperimentName = "checkout-button-v2",
            TransitionId   = "t-cbv2-02",
            FromState      = ExperimentLifecycleState.PendingApproval,
            ToState        = ExperimentLifecycleState.Approved,
            IsApproved     = true,
            Approver       = "approver1@experimentdemo.com",
            Reason         = "Design review passed.",
            Timestamp      = frozenNow.AddDays(-21),
            GateName       = "require-two-approvers",
            Environment    = DefaultEnvironment,
        }, ct);

        await backplane.AppendApprovalRecordAsync(new PersistedApprovalRecord
        {
            ApprovalId     = "apr-cbv2-02",
            ExperimentName = "checkout-button-v2",
            TransitionId   = "t-cbv2-02",
            FromState      = ExperimentLifecycleState.PendingApproval,
            ToState        = ExperimentLifecycleState.Approved,
            IsApproved     = true,
            Approver       = "approver2@experimentdemo.com",
            Reason         = "Safety review passed.",
            Timestamp      = frozenNow.AddDays(-21).AddHours(2),
            GateName       = "require-two-approvers",
            Environment    = DefaultEnvironment,
        }, ct);
    }

    // -------------------------------------------------------------------------
    // Configuration Versions (8 records — 2 per non-draft experiment)
    // -------------------------------------------------------------------------

    private static async Task SeedConfigurationVersionsAsync(
        IGovernancePersistenceBackplane backplane,
        DateTimeOffset frozenNow,
        CancellationToken ct)
    {
        var versions = new[]
        {
            // checkout-button-v2
            ConfigVersion("checkout-button-v2", 1,
                """{"rolloutPercentage":10,"arms":["control","variant-a"]}""",
                "Initial launch at 10% rollout",
                frozenNow.AddDays(-21),
                ExperimentLifecycleState.Running),
            ConfigVersion("checkout-button-v2", 2,
                """{"rolloutPercentage":50,"arms":["control","variant-a"]}""",
                "Scaled to 50% after first-week data",
                frozenNow.AddDays(-14),
                ExperimentLifecycleState.Running),

            // search-ranker-ml
            ConfigVersion("search-ranker-ml", 1,
                """{"rolloutPercentage":10,"arms":["baseline","ml-v1","ml-v2"]}""",
                "Initial launch at 10% rollout",
                frozenNow.AddDays(-7),
                ExperimentLifecycleState.Running),
            ConfigVersion("search-ranker-ml", 2,
                """{"rolloutPercentage":10,"arms":["baseline","ml-v1","ml-v2"],"note":"Extended run"}""",
                "Extended run — no changes",
                frozenNow.AddDays(-1),
                ExperimentLifecycleState.Running),

            // pricing-page-copy
            ConfigVersion("pricing-page-copy", 1,
                """{"rolloutPercentage":10,"arms":["control","copy-v2"]}""",
                "Initial launch at 10% rollout",
                frozenNow.AddDays(-30),
                ExperimentLifecycleState.Running),
            ConfigVersion("pricing-page-copy", 2,
                """{"rolloutPercentage":25,"arms":["control","copy-v2"]}""",
                "Scaled to 25%; subsequently paused",
                frozenNow.AddDays(-2),
                ExperimentLifecycleState.Paused),

            // legacy-api-cutover
            ConfigVersion("legacy-api-cutover", 1,
                """{"rolloutPercentage":10,"arms":["legacy","new-api"]}""",
                "Initial launch at 10% rollout",
                frozenNow.AddDays(-60),
                ExperimentLifecycleState.Running),
            ConfigVersion("legacy-api-cutover", 2,
                """{"rolloutPercentage":100,"arms":["new-api"],"promoted":true}""",
                "Full rollout and promotion snapshot",
                frozenNow.AddDays(-30),
                ExperimentLifecycleState.Archived),
        };

        foreach (var v in versions)
        {
            await backplane.AppendConfigurationVersionAsync(v, ct);
        }
    }

    private static PersistedConfigurationVersion ConfigVersion(
        string experimentName,
        int versionNumber,
        string configJson,
        string changeDescription,
        DateTimeOffset createdAt,
        ExperimentLifecycleState? lifecycleState) =>
        new()
        {
            ExperimentName     = experimentName,
            VersionNumber      = versionNumber,
            ConfigurationJson  = configJson,
            // Deterministic hash: SHA-style placeholder sufficient for demo
            ConfigurationHash  = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(configJson)))[..16],
            ChangeDescription  = changeDescription,
            CreatedAt          = createdAt,
            CreatedBy          = DefaultActor,
            LifecycleState     = lifecycleState,
            Environment        = DefaultEnvironment,
        };

    // -------------------------------------------------------------------------
    // Policy Evaluations
    // -------------------------------------------------------------------------

    private static async Task SeedPolicyEvaluationsAsync(
        IGovernancePersistenceBackplane backplane,
        DateTimeOffset frozenNow,
        CancellationToken ct)
    {
        var evaluations = new[]
        {
            // FAIL: pricing-page-copy vs min-sample-size-1000
            PolicyEval("pe-ppc-fail-01", "pricing-page-copy", "min-sample-size-1000",
                isCompliant:    false,
                reason:         "Control arm had only 712 samples when decision was surfaced. Minimum is 1000.",
                severity:       PolicyViolationSeverity.Critical,
                currentState:   ExperimentLifecycleState.Ramping,
                targetState:    null,
                timestamp:      frozenNow.AddDays(-2)),

            // PASS: checkout-button-v2 vs require-two-approvers
            PolicyEval("pe-cbv2-pass-01", "checkout-button-v2", "require-two-approvers",
                isCompliant:    true,
                reason:         "Two distinct approvers recorded for the activation transition.",
                severity:       PolicyViolationSeverity.Info,
                currentState:   ExperimentLifecycleState.PendingApproval,
                targetState:    ExperimentLifecycleState.Approved,
                timestamp:      frozenNow.AddDays(-21)),

            // PASS: checkout-button-v2 vs no-friday-deploys
            PolicyEval("pe-cbv2-pass-02", "checkout-button-v2", "no-friday-deploys",
                isCompliant:    true,
                reason:         "Launch occurred on a Tuesday — no Friday restriction applies.",
                severity:       PolicyViolationSeverity.Info,
                currentState:   ExperimentLifecycleState.Approved,
                targetState:    ExperimentLifecycleState.Running,
                timestamp:      frozenNow.AddDays(-21)),

            // PASS: search-ranker-ml vs min-sample-size-1000
            PolicyEval("pe-srml-pass-01", "search-ranker-ml", "min-sample-size-1000",
                isCompliant:    true,
                reason:         "All arms exceed 3000 samples; minimum of 1000 is satisfied.",
                severity:       PolicyViolationSeverity.Info,
                currentState:   ExperimentLifecycleState.Running,
                targetState:    null,
                timestamp:      frozenNow.AddDays(-1)),

            // PASS: legacy-api-cutover vs no-friday-deploys
            PolicyEval("pe-lac-pass-01", "legacy-api-cutover", "no-friday-deploys",
                isCompliant:    true,
                reason:         "Promotion occurred on a Wednesday.",
                severity:       PolicyViolationSeverity.Info,
                currentState:   ExperimentLifecycleState.Running,
                targetState:    ExperimentLifecycleState.Archived,
                timestamp:      frozenNow.AddDays(-30)),
        };

        foreach (var e in evaluations)
        {
            await backplane.AppendPolicyEvaluationAsync(e, ct);
        }
    }

    private static PersistedPolicyEvaluation PolicyEval(
        string evaluationId,
        string experimentName,
        string policyName,
        bool isCompliant,
        string reason,
        PolicyViolationSeverity severity,
        ExperimentLifecycleState? currentState,
        ExperimentLifecycleState? targetState,
        DateTimeOffset timestamp) =>
        new()
        {
            EvaluationId   = evaluationId,
            ExperimentName = experimentName,
            PolicyName     = policyName,
            IsCompliant    = isCompliant,
            Reason         = reason,
            Severity       = severity,
            CurrentState   = currentState,
            TargetState    = targetState,
            Timestamp      = timestamp,
            Environment    = DefaultEnvironment,
        };
}
