# User Guide & Dashboard Screenshots Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a new top-level User Guide section in the DocFX site with Developer Setup + Operator Guide (tutorials + reference), backed by ~72 deterministic PNG screenshots captured from the Playwright+Reqnroll e2e suite against a curated demo seeder, plus a weekly CI workflow that auto-PRs screenshot drift.

**Architecture:** Three execution tracks — (1) Foundation: docs-demo seeder, `@docs-screenshot` Reqnroll tag/step/hook, rename `docs/user-guide/` → `docs/reference/` with xref redirects, new `docs/toc.yml` skeleton; (2) Content: 5 tutorials + 15 reference pages + 3 developer-setup pages with screenshots captured by scenarios against the seeded DashboardHost; (3) CI: `docs-screenshots.yml` weekly regen with auto-PR on drift. Foundation must land before Content. CI can land in parallel with Content.

**Tech Stack:** .NET 10, Blazor Server (dashboard), Playwright + Reqnroll (e2e), xUnit, DocFX, GitHub Actions.

---

## Phase 1 — Foundation

### Task 1: Set up `docs/images/screenshots/` folder and `.gitattributes` for PNG handling

**Estimated time:** 10–15 minutes

- [ ] **Step 1.1** — Create screenshot subdirectory tree:
  ```bash
  mkdir -p docs/images/screenshots/experiments
  mkdir -p docs/images/screenshots/analytics
  mkdir -p docs/images/screenshots/governance
  mkdir -p docs/images/screenshots/rollout
  mkdir -p docs/images/screenshots/targeting
  mkdir -p docs/images/screenshots/plugins
  mkdir -p docs/images/screenshots/configuration
  mkdir -p docs/images/screenshots/developer-setup
  # Add a .gitkeep in each so folders are tracked before screenshots exist
  for d in docs/images/screenshots/experiments docs/images/screenshots/analytics \
            docs/images/screenshots/governance docs/images/screenshots/rollout \
            docs/images/screenshots/targeting docs/images/screenshots/plugins \
            docs/images/screenshots/configuration docs/images/screenshots/developer-setup; do
    touch "$d/.gitkeep"
  done
  ```
  Expected: 8 subdirectories, each with a `.gitkeep`.

- [ ] **Step 1.2** — Add Git attributes for PNG binary handling. Open (or create) `.gitattributes` in the repo root and append:
  ```
  # Treat PNG screenshots as binary to avoid line-ending corruption
  docs/images/screenshots/**/*.png binary
  docs/images/screenshots/**/*.png -diff
  docs/images/screenshots/**/*.png linguist-generated=true
  ```

- [ ] **Step 1.3** — Commit skeleton:
  ```bash
  git add docs/images/screenshots/ .gitattributes
  git commit -m "chore(screenshots): add docs/images/screenshots/ folder tree and .gitattributes"
  ```
  Expected output: commit with ~9 files (`.gitkeep` × 8 + `.gitattributes` change).

---

### Task 2: Add `@docs-screenshot` Reqnroll step definition + BeforeScenario hook

**Estimated time:** 20–30 minutes

- [ ] **Step 2.1** — Create the step definitions file `tests/ExperimentFramework.E2E.Tests/StepDefinitions/DocsScreenshotStepDefinitions.cs`:
  ```csharp
  using ExperimentFramework.E2E.Tests.Drivers;
  using Reqnroll;

  namespace ExperimentFramework.E2E.Tests.StepDefinitions;

  [Binding]
  public class DocsScreenshotStepDefinitions
  {
      private readonly DashboardDriver _dashboardDriver;
      private readonly ScenarioContext _scenarioContext;

      // Root of the repo, resolved relative to the test assembly output directory.
      // Tests run from bin/Debug/net10.0 — go up five levels to reach the repo root.
      private static readonly string RepoRoot = Path.GetFullPath(
          Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));

      public DocsScreenshotStepDefinitions(
          DashboardDriver dashboardDriver,
          ScenarioContext scenarioContext)
      {
          _dashboardDriver = dashboardDriver;
          _scenarioContext = scenarioContext;
      }

      /// <summary>
      /// Captures a PNG screenshot to docs/images/screenshots/{area}/{name}.png.
      /// The {area} is derived from the ScenarioContext tag "screenshot-area:{area}"
      /// set by the BeforeScenario hook in DocsScreenshotHooks.
      /// </summary>
      [When(@"I capture screenshot ""([^""]+)""")]
      public async Task WhenICaptureScreenshot(string name)
      {
          var area = _scenarioContext.TryGetValue<string>("DocsScreenshotArea", out var a)
              ? a
              : "misc";

          var filePath = Path.Combine(RepoRoot, "docs", "images", "screenshots", area, $"{name}.png");
          Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
          await _dashboardDriver.TakeScreenshotAsync(filePath);
      }
  }
  ```

- [ ] **Step 2.2** — Create `tests/ExperimentFramework.E2E.Tests/Hooks/DocsScreenshotHooks.cs`:
  ```csharp
  using ExperimentFramework.E2E.Tests.Drivers;
  using Microsoft.Playwright;
  using Reqnroll;

  namespace ExperimentFramework.E2E.Tests.Hooks;

  [Binding]
  public class DocsScreenshotHooks
  {
      private readonly BrowserDriver _browserDriver;
      private readonly ScenarioContext _scenarioContext;

      // CSS injected into every docs-screenshot scenario to freeze animations,
      // hide carets, and suppress transitions for deterministic pixel output.
      private const string DeterminismCss =
          "*, *::before, *::after { " +
          "  animation: none !important; " +
          "  transition: none !important; " +
          "  caret-color: transparent !important; " +
          "}";

      public DocsScreenshotHooks(BrowserDriver browserDriver, ScenarioContext scenarioContext)
      {
          _browserDriver   = browserDriver;
          _scenarioContext = scenarioContext;
      }

      /// <summary>
      /// Runs after the default BeforeScenario (Order=0) and before the
      /// role-specific auto-login hooks (Order=10).
      /// Resizes the viewport to 1280x800 and injects determinism CSS.
      /// Also reads the "screenshot-area:{area}" tag and stores it in ScenarioContext.
      /// </summary>
      [BeforeScenario("docs-screenshot", Order = 5)]
      public async Task ConfigureDocsScreenshotScenario()
      {
          // Set viewport to canonical screenshot size
          await _browserDriver.Page.SetViewportSizeAsync(1280, 800);

          // Inject determinism CSS on every navigation
          await _browserDriver.Page.AddInitScriptAsync(
              $"document.addEventListener('DOMContentLoaded', () => {{ " +
              $"  const s = document.createElement('style'); " +
              $"  s.textContent = `{DeterminismCss}`; " +
              $"  document.head.appendChild(s); " +
              $"}});");

          // Also inject immediately in case page is already loaded
          await _browserDriver.Page.AddStyleTagAsync(new PageAddStyleTagOptions
          {
              Content = DeterminismCss
          });

          // Extract screenshot-area from scenario tags, preferring scenario-level over feature-level.
          // ScenarioInfo.Tags contains merged feature + scenario tags; iterate in reverse so that
          // a scenario-scoped @screenshot-area:X tag overrides a feature-scoped one.
          var areaTag = _scenarioContext.ScenarioInfo.Tags
              .Reverse()
              .FirstOrDefault(t => t.StartsWith("screenshot-area:", StringComparison.OrdinalIgnoreCase));

          var area = areaTag is not null
              ? areaTag["screenshot-area:".Length..]
              : "misc";

          _scenarioContext["DocsScreenshotArea"] = area;
      }
  }
  ```

- [ ] **Step 2.3** — Verify the `[Category("DocsScreenshot")]` trait is automatically applied by Reqnroll when the scenario is tagged `@docs-screenshot`. Open `tests/ExperimentFramework.E2E.Tests/reqnroll.json` — no changes needed; Reqnroll maps Gherkin tags to xUnit traits by default.

- [ ] **Step 2.4** — Build to confirm no compile errors:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  ```
  Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 2.5** — Commit:
  ```bash
  git add tests/ExperimentFramework.E2E.Tests/StepDefinitions/DocsScreenshotStepDefinitions.cs \
          tests/ExperimentFramework.E2E.Tests/Hooks/DocsScreenshotHooks.cs
  git commit -m "feat(e2e): add @docs-screenshot step definitions and BeforeScenario hook"
  ```

---

### Task 3: Define 5 demo service interfaces + implementations, and register them via `ExperimentFrameworkBuilder` in `Program.cs`

> **Amendment note (2026-04-22):** This task was rewritten from scratch. The original plan assumed `IExperimentRepository.CreateAsync` and related write-side APIs that do not exist in the codebase. Experiments are registered exclusively at startup through `ExperimentFrameworkBuilder` — there is no runtime "create experiment" path. The hallucinated types (`IExperimentRepository`, `ExperimentDefinition`, `ExperimentArm`, `ExperimentStatus`) were removed and replaced with the real builder pattern discovered during foundation exploration.

**Estimated time:** 30–40 minutes

- [ ] **Step 3.1** — Create the demo service folder `samples/ExperimentFramework.DashboardHost/DemoServices/`. Add the following five interface + implementation files. Each service has a single trivial method (`string Describe()`) — they exist solely to give `ExperimentFrameworkBuilder` concrete types to bind:

  **`ICheckoutButtonService.cs`:**
  ```csharp
  namespace ExperimentFramework.DashboardHost.DemoServices;
  public interface ICheckoutButtonService { string Describe(); }
  public sealed class CheckoutButtonControl  : ICheckoutButtonService { public string Describe() => "Control"; }
  public sealed class CheckoutButtonVariantA : ICheckoutButtonService { public string Describe() => "Variant A"; }
  ```

  **`ISearchRankerService.cs`:**
  ```csharp
  namespace ExperimentFramework.DashboardHost.DemoServices;
  public interface ISearchRankerService { string Describe(); }
  public sealed class SearchBaseline : ISearchRankerService { public string Describe() => "Baseline"; }
  public sealed class SearchMlV1     : ISearchRankerService { public string Describe() => "ML v1"; }
  public sealed class SearchMlV2     : ISearchRankerService { public string Describe() => "ML v2"; }
  ```

  **`IHomepageLayoutService.cs`:**
  ```csharp
  namespace ExperimentFramework.DashboardHost.DemoServices;
  public interface IHomepageLayoutService { string Describe(); }
  public sealed class HomepageLayoutControl  : IHomepageLayoutService { public string Describe() => "Current"; }
  public sealed class HomepageLayoutFallHero : IHomepageLayoutService { public string Describe() => "Fall Hero"; }
  ```

  **`IPricingCopyService.cs`:**
  ```csharp
  namespace ExperimentFramework.DashboardHost.DemoServices;
  public interface IPricingCopyService { string Describe(); }
  public sealed class PricingCopyOriginal     : IPricingCopyService { public string Describe() => "Original"; }
  public sealed class PricingCopyBenefitsLed  : IPricingCopyService { public string Describe() => "Benefits-Led"; }
  ```

  **`ILegacyApiService.cs`:**
  ```csharp
  namespace ExperimentFramework.DashboardHost.DemoServices;
  public interface ILegacyApiService { string Describe(); }
  public sealed class LegacyApiV1 : ILegacyApiService { public string Describe() => "V1 API"; }
  public sealed class LegacyApiV2 : ILegacyApiService { public string Describe() => "V2 API"; }
  ```

- [ ] **Step 3.2** — Open `samples/ExperimentFramework.DashboardHost/Program.cs`. Before `var builder = WebApplication.CreateBuilder(args);`, add simple arg parsing:
  ```csharp
  // Parse docs-specific CLI args before builder construction.
  // Example: dotnet run -- --seed=docs --freeze-date 2026-04-01T12:00:00Z
  var cliArgs = ParseDocsArgs(args);
  ```

  Then at the **bottom of the file** (after the `public partial class Program { }` line if present), add:
  ```csharp
  // ============================================
  // Docs Demo CLI Arg Parsing
  // ============================================

  file record DocsCliArgs(bool SeedDocs, DateTimeOffset? FreezeDate);

  file static DocsCliArgs ParseDocsArgs(string[] args)
  {
      var seedDocs = false;
      DateTimeOffset? freezeDate = null;

      for (var i = 0; i < args.Length; i++)
      {
          if (args[i].StartsWith("--seed=", StringComparison.OrdinalIgnoreCase) &&
              args[i]["--seed=".Length..].Equals("docs", StringComparison.OrdinalIgnoreCase))
              seedDocs = true;

          if (args[i].Equals("--freeze-date", StringComparison.OrdinalIgnoreCase) &&
              i + 1 < args.Length &&
              DateTimeOffset.TryParse(args[i + 1], out var fd))
          {
              freezeDate = fd;
              i++; // skip the value token
          }
      }

      // Also support env var EXPERIMENT_DEMO_SEED=docs
      if (!seedDocs &&
          Environment.GetEnvironmentVariable("EXPERIMENT_DEMO_SEED")
              ?.Equals("docs", StringComparison.OrdinalIgnoreCase) == true)
          seedDocs = true;

      return new DocsCliArgs(seedDocs, freezeDate);
  }
  ```
  > **Note:** `--reset` is dropped. Since analytics and governance state are fully in-memory, every process restart is a clean slate — `--reset` is a no-op and adds complexity for no benefit.

- [ ] **Step 3.3** — In `Program.cs`, wrap the existing experiment registration in an `if/else` on `cliArgs.SeedDocs`. When seeding, replace the existing registration block with the five demo experiments inside a `#region Docs Demo` block:
  ```csharp
  if (cliArgs.SeedDocs)
  {
      #region Docs Demo — five experiments for screenshot capture
      builder.Services.AddExperimentFramework(fw => fw
          .Define<ICheckoutButtonService>(e => e
              .UsingFeatureFlag("checkout-button-v2")
              .AddDefaultTrial<CheckoutButtonControl>("control")
              .AddTrial<CheckoutButtonVariantA>("variant-a"))
          .Define<ISearchRankerService>(e => e
              .UsingFeatureFlag("search-ranker-ml")
              .AddDefaultTrial<SearchBaseline>("baseline")
              .AddTrial<SearchMlV1>("ml-v1")
              .AddTrial<SearchMlV2>("ml-v2"))
          .Define<IHomepageLayoutService>(e => e
              .UsingFeatureFlag("homepage-layout-fall2026")
              .AddDefaultTrial<HomepageLayoutControl>("control")
              .AddTrial<HomepageLayoutFallHero>("fall-hero"))
          .Define<IPricingCopyService>(e => e
              .UsingFeatureFlag("pricing-page-copy")
              .AddDefaultTrial<PricingCopyOriginal>("control")
              .AddTrial<PricingCopyBenefitsLed>("benefits"))
          .Define<ILegacyApiService>(e => e
              .UsingFeatureFlag("legacy-api-cutover")
              .AddDefaultTrial<LegacyApiV1>("v1-api")
              .AddTrial<LegacyApiV2>("v2-api"))
          .UseDispatchProxy());
      #endregion
  }
  else
  {
      // existing non-demo experiment registration stays here unchanged
  }
  ```
  > **Implementer note:** The exact fluent method names (`Define<T>`, `UsingFeatureFlag`, `AddDefaultTrial<T>`, `AddTrial<T>`, `UseDispatchProxy`) come from `src/ExperimentFramework/ServiceCollectionExtensions.cs`. Open that file and confirm the chain shape before writing the code — if any method name differs from the sketch above, follow reality. The sketch is based on the pattern observed in the existing `Program.cs` for the two existing demo experiments.

- [ ] **Step 3.4** — Build to verify:
  ```bash
  dotnet build samples/ExperimentFramework.DashboardHost/ExperimentFramework.DashboardHost.csproj
  ```
  Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 3.5** — Commit:
  ```bash
  git add samples/ExperimentFramework.DashboardHost/DemoServices/ \
          samples/ExperimentFramework.DashboardHost/Program.cs
  git commit -m "feat(seeder): register five demo experiments for docs screenshots"
  ```

---

### Task 4: Add `DemoAnalyticsProvider` and `GovernanceDemoSeeder` for in-memory demo data

> **Amendment note (2026-04-22):** This task was rewritten. The original plan used `_repository.BulkInsertSamplesAsync`, `CreateApprovalRequestAsync`, `CreateAuditEventAsync`, `CreatePolicyAsync`, `CreatePolicyViolationAsync`, and `CreateVersionSnapshotAsync` — none of which exist. The real analytics surface is `IAnalyticsProvider` (read-only interface returning in-memory event collections). The real governance write surface is `IGovernancePersistenceBackplane` with `SaveExperimentStateAsync`, `AppendStateTransitionAsync`, `AppendApprovalRecordAsync`, `AppendConfigurationVersionAsync`, and `AppendPolicyEvaluationAsync`. Both are now used correctly.

**Estimated time:** 45–60 minutes

**Part A — `DemoAnalyticsProvider`**

- [ ] **Step 4.1** — Create `samples/ExperimentFramework.DashboardHost/Demo/DemoAnalyticsProvider.cs`:
  ```csharp
  using ExperimentFramework.Dashboard.Abstractions;
  using ExperimentFramework.DataPlane.Abstractions.Events;

  namespace ExperimentFramework.DashboardHost.Demo;

  /// <summary>
  /// In-memory IAnalyticsProvider with curated demo data.
  /// checkout-button-v2 shows a stat-sig winner on variant-a (4.8% vs 3.1% conversion).
  /// search-ranker-ml is inconclusive across three arms.
  /// </summary>
  public sealed class DemoAnalyticsProvider : IAnalyticsProvider
  {
      private readonly IReadOnlyList<AssignmentEvent>    _assignments;
      private readonly IReadOnlyList<ExposureEvent>       _exposures;
      private readonly IReadOnlyList<AnalysisSignalEvent> _signals;

      public DemoAnalyticsProvider(DateTimeOffset frozenNow)
      {
          _assignments = BuildAssignments(frozenNow);
          _exposures   = BuildExposures(frozenNow);
          _signals     = BuildSignals(frozenNow);
      }

      public Task<IReadOnlyList<AssignmentEvent>> GetAssignmentsAsync(
          string experimentName, CancellationToken ct) =>
          Task.FromResult<IReadOnlyList<AssignmentEvent>>(
              _assignments.Where(a => a.ExperimentName == experimentName).ToList());

      public Task<IReadOnlyList<ExposureEvent>> GetExposuresAsync(
          string experimentName, CancellationToken ct) =>
          Task.FromResult<IReadOnlyList<ExposureEvent>>(
              _exposures.Where(e => e.ExperimentName == experimentName).ToList());

      public Task<IReadOnlyList<AnalysisSignalEvent>> GetAnalysisSignalsAsync(
          string experimentName, CancellationToken ct) =>
          Task.FromResult<IReadOnlyList<AnalysisSignalEvent>>(
              _signals.Where(s => s.ExperimentName == experimentName).ToList());

      private static IReadOnlyList<AssignmentEvent> BuildAssignments(DateTimeOffset now)
      {
          var rng   = new Random(42);
          var list  = new List<AssignmentEvent>();
          var span  = (int)TimeSpan.FromDays(14).TotalSeconds;

          // checkout-button-v2: ~5k per arm over 14-day window
          foreach (var (arm, count) in new[] { ("control", 5100), ("variant-a", 5200) })
              for (var i = 0; i < count; i++)
                  list.Add(new AssignmentEvent
                  {
                      ExperimentName = "checkout-button-v2",
                      ArmName        = arm,
                      AssignedAt     = now.AddDays(-14).AddSeconds(rng.Next(0, span))
                      // Confirm exact property names from
                      // src/ExperimentFramework.DataPlane.Abstractions/Events/AssignmentEvent.cs
                  });

          // search-ranker-ml: ~3k per arm over 7-day window
          foreach (var (arm, count) in new[] { ("baseline", 3100), ("ml-v1", 3050), ("ml-v2", 3020) })
              for (var i = 0; i < count; i++)
                  list.Add(new AssignmentEvent
                  {
                      ExperimentName = "search-ranker-ml",
                      ArmName        = arm,
                      AssignedAt     = now.AddDays(-7).AddSeconds(rng.Next(0, span / 2))
                  });

          return list;
      }

      private static IReadOnlyList<ExposureEvent> BuildExposures(DateTimeOffset now)
      {
          // Exposures mirror assignments (every assigned user was exposed)
          // Construct similarly to BuildAssignments but using ExposureEvent type.
          // Confirm exact property names from
          // src/ExperimentFramework.DataPlane.Abstractions/Events/ExposureEvent.cs
          return Array.Empty<ExposureEvent>(); // placeholder — implementer fills in
      }

      private static IReadOnlyList<AnalysisSignalEvent> BuildSignals(DateTimeOffset now)
      {
          // checkout-button-v2: variant-a conversion 4.8% vs control 3.1%
          // search-ranker-ml: baseline 18.2%, ml-v1 18.5%, ml-v2 18.4% (inconclusive)
          // Use seeded Random(42) for determinism.
          // Confirm exact property names from
          // src/ExperimentFramework.DataPlane.Abstractions/Events/AnalysisSignalEvent.cs
          return Array.Empty<AnalysisSignalEvent>(); // placeholder — implementer fills in
      }
  }
  ```
  > **Implementer note:** Before writing the property initializers, read `src/ExperimentFramework.DataPlane.Abstractions/Events/AssignmentEvent.cs`, `ExposureEvent.cs`, and `AnalysisSignalEvent.cs` to get the exact constructor signatures or `init` property names. Use `new Random(42)` everywhere for determinism. Target the conversion rates from the comment above — checkout variant-a at 4.8% vs control at 3.1% (clearly stat-sig at these sample sizes); search arms all within 0.3pp of each other (inconclusive).

**Part B — `GovernanceDemoSeeder`**

- [ ] **Step 4.2** — Add `builder.Services.AddInMemoryGovernancePersistence();` to the `if (cliArgs.SeedDocs)` block in `Program.cs` (before `builder.Build()`). This extension method lives in `src/ExperimentFramework.Governance.Persistence/ServiceCollectionExtensions.cs` and is not wired by `AddExperimentDashboard` by default.

- [ ] **Step 4.3** — Create `samples/ExperimentFramework.DashboardHost/Demo/GovernanceDemoSeeder.cs`:
  ```csharp
  using ExperimentFramework.Governance.Persistence;
  using ExperimentFramework.Governance.Persistence.Models;

  namespace ExperimentFramework.DashboardHost.Demo;

  /// <summary>
  /// Seeds governance demo data: per-experiment state, ~20 state transitions,
  /// 1 pending approval, 8 configuration versions, and policy evaluations
  /// (including 1 fail on pricing-page-copy).
  /// Reference pattern: samples/ExperimentFramework.GovernanceSample/Program.cs
  /// </summary>
  public static class GovernanceDemoSeeder
  {
      public static async Task SeedAsync(
          IGovernancePersistenceBackplane backplane,
          DateTimeOffset frozenNow,
          CancellationToken ct = default)
      {
          // --- Experiment state records ---
          // checkout-button-v2: Running, 50%
          await backplane.SaveExperimentStateAsync(
              new PersistedExperimentState
              {
                  ExperimentName = "checkout-button-v2",
                  // fill remaining fields per Models/PersistedExperimentState.cs
              },
              expectedETag: null, ct);

          // search-ranker-ml: Running, 10%
          await backplane.SaveExperimentStateAsync(
              new PersistedExperimentState { ExperimentName = "search-ranker-ml" },
              null, ct);

          // homepage-layout-fall2026: Draft / PendingApproval
          await backplane.SaveExperimentStateAsync(
              new PersistedExperimentState { ExperimentName = "homepage-layout-fall2026" },
              null, ct);

          // pricing-page-copy: Paused
          await backplane.SaveExperimentStateAsync(
              new PersistedExperimentState { ExperimentName = "pricing-page-copy" },
              null, ct);

          // legacy-api-cutover: Archived
          await backplane.SaveExperimentStateAsync(
              new PersistedExperimentState { ExperimentName = "legacy-api-cutover" },
              null, ct);

          // --- State transitions (~20 audit events across experiments) ---
          // checkout-button-v2 lifecycle
          await backplane.AppendStateTransitionAsync(new PersistedStateTransition
          {
              ExperimentName = "checkout-button-v2",
              // FromState, ToState, OccurredAt = frozenNow.AddDays(-21), Actor, etc.
              // Confirm field names from Models/PersistedStateTransition.cs
          }, ct);
          // ... add ~19 more transitions covering the same lifecycle events documented
          // in the original Task 4.3 audit event table (Created, StatusChanged,
          // RolloutChanged, ApprovalRequested, PolicyViolation, Promoted, Archived).

          // --- Pending approval: homepage-layout-fall2026 ---
          await backplane.AppendApprovalRecordAsync(new PersistedApprovalRecord
          {
              ExperimentName = "homepage-layout-fall2026",
              // RequestedBy = "experimenter@experimentdemo.com",
              // RequestedAt = frozenNow.AddDays(-1),
              // Status = pending equivalent,
              // Notes = "Requesting approval to launch Fall 2026 hero test."
              // Confirm field names from Models/PersistedApprovalRecord.cs
          }, ct);

          // --- Configuration versions (2 per non-draft experiment = 8 total) ---
          foreach (var (name, version, notes, when) in new[]
          {
              ("checkout-button-v2",  1, "Initial launch at 10% rollout",            frozenNow.AddDays(-21)),
              ("checkout-button-v2",  2, "Scaled to 50% after first-week data",      frozenNow.AddDays(-14)),
              ("search-ranker-ml",    1, "Initial launch at 10% rollout",            frozenNow.AddDays(-7)),
              ("search-ranker-ml",    2, "Extended run — no changes",                frozenNow.AddDays(-1)),
              ("pricing-page-copy",   1, "Initial launch at 10% rollout",            frozenNow.AddDays(-30)),
              ("pricing-page-copy",   2, "Scaled to 25%; subsequently paused",       frozenNow.AddDays(-2)),
              ("legacy-api-cutover",  1, "Initial launch at 10% rollout",            frozenNow.AddDays(-60)),
              ("legacy-api-cutover",  2, "Full rollout and promotion snapshot",       frozenNow.AddDays(-30)),
          })
          {
              await backplane.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
              {
                  ExperimentName = name,
                  // Version = version, Notes = notes, CreatedAt = when, CreatedBy = "admin@..."
                  // Confirm field names from Models/PersistedConfigurationVersion.cs
              }, ct);
          }

          // --- Policy evaluations ---
          // Three policies evaluated; pricing-page-copy fails min-sample-size-1000.
          // Pass evaluations for the other experiments/policies.
          await backplane.AppendPolicyEvaluationAsync(new PersistedPolicyEvaluation
          {
              ExperimentName = "pricing-page-copy",
              PolicyName     = "min-sample-size-1000",
              // Passed = false,
              // Detail = "Control arm had only 712 samples when decision was surfaced.",
              // EvaluatedAt = frozenNow.AddDays(-2)
              // Confirm field names from Models/PersistedPolicyEvaluation.cs
          }, ct);
          // Add passing evaluations for require-two-approvers and no-friday-deploys
          // on the running experiments.
      }
  }
  ```
  > **Implementer note:** All field names in `PersistedExperimentState`, `PersistedStateTransition`, `PersistedApprovalRecord`, `PersistedConfigurationVersion`, and `PersistedPolicyEvaluation` must be verified by reading the files under `src/ExperimentFramework.Governance.Persistence/Models/`. Study `samples/ExperimentFramework.GovernanceSample/Program.cs` as the primary working reference — it already demonstrates writing to `IGovernancePersistenceBackplane` after `AddInMemoryGovernancePersistence()`. Copy its patterns directly.
  >
  > **Policy registration:** The three named policies ("require-two-approvers", "no-friday-deploys", "min-sample-size-1000") referenced in the policy evaluations need to exist as policy definitions. Investigate whether policies can be registered via a `ExperimentFrameworkBuilder` API or are code-defined only (check `src/ExperimentFramework.Governance/Policies/`). If there is a registration API, use it in the `SeedDocs` block in `Program.cs`. If policies are code-defined only with no way to inject demo policies, create a minimal `DemoPolicyDefinitions` static class in the Demo folder. If neither approach is feasible, document this in the commit and omit the policy evaluation seeding. Report findings in the commit message.

- [ ] **Step 4.4** — Build:
  ```bash
  dotnet build samples/ExperimentFramework.DashboardHost/ExperimentFramework.DashboardHost.csproj
  ```
  Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4.5** — Commit:
  ```bash
  git add samples/ExperimentFramework.DashboardHost/Demo/
  git commit -m "feat(seeder): add DemoAnalyticsProvider and governance seed data"
  ```

---

### Task 5: Wire `--seed=docs` and `--freeze-date` in `Program.cs` to compose T3/T4 registrations

> **Amendment note (2026-04-22):** This task was rewritten. `--reset` is dropped — in-memory state is always fresh on restart, so it adds no value. The `DocsDemoSeeder` class from the original plan is replaced by the `DemoAnalyticsProvider` and `GovernanceDemoSeeder` composition introduced in T3/T4. The CLI arg parsing record now carries only `SeedDocs` and `FreezeDate`.

**Estimated time:** 20–25 minutes

- [ ] **Step 5.1** — Verify that the `ParseDocsArgs` helper added in Step 3.2 is in place and builds. Confirm the `DocsCliArgs` record contains `SeedDocs` and `FreezeDate` (no `Reset` field).

- [ ] **Step 5.2** — Confirm the full `if (cliArgs.SeedDocs)` registration block in `Program.cs` (before `builder.Build()`) reads:
  ```csharp
  if (cliArgs.SeedDocs)
  {
      var frozenNow = cliArgs.FreezeDate
          ?? new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

      // T3: five demo experiments registered via ExperimentFrameworkBuilder
      builder.Services.AddExperimentFramework(/* five .Define<T>() calls from T3 */);

      // T4-A: custom analytics provider
      builder.Services.AddSingleton<IAnalyticsProvider>(
          new DemoAnalyticsProvider(frozenNow));

      // T4-B: in-memory governance persistence
      builder.Services.AddInMemoryGovernancePersistence();
  }
  else
  {
      // existing non-demo registrations unchanged
  }
  ```

- [ ] **Step 5.3** — Confirm the post-build governance seeding block (after `var app = builder.Build();`) reads:
  ```csharp
  if (cliArgs.SeedDocs)
  {
      var frozenNow = cliArgs.FreezeDate
          ?? new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

      using var scope = app.Services.CreateScope();
      var backplane = scope.ServiceProvider
          .GetRequiredService<IGovernancePersistenceBackplane>();
      await GovernanceDemoSeeder.SeedAsync(backplane, frozenNow);
  }
  ```
  > **Note:** `DemoAnalyticsProvider` is constructed and registered directly in the builder phase (Step 5.2) — no post-build call needed for analytics. Only governance seeding requires the built service provider.

- [ ] **Step 5.4** — Wire the `ISystemClock` stub when `--freeze-date` is specified. If `ExperimentFramework` exposes `ISystemClock`, register a stub:
  ```csharp
  if (cliArgs.FreezeDate.HasValue)
  {
      builder.Services.AddSingleton<ISystemClock>(
          new FrozenSystemClock(cliArgs.FreezeDate.Value));
  }
  ```
  And add the record at the bottom of the file:
  ```csharp
  file sealed class FrozenSystemClock : ISystemClock
  {
      private readonly DateTimeOffset _frozen;
      public FrozenSystemClock(DateTimeOffset frozen) => _frozen = frozen;
      public DateTimeOffset UtcNow => _frozen;
  }
  ```
  If `ISystemClock` is not part of the public API, skip this step and note it in the commit message.

- [ ] **Step 5.5** — Build:
  ```bash
  dotnet build samples/ExperimentFramework.DashboardHost/ExperimentFramework.DashboardHost.csproj
  ```
  Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 5.6** — Smoke-run with `--seed=docs`:
  ```bash
  dotnet run --project samples/ExperimentFramework.DashboardHost -- --seed=docs &
  sleep 4 && curl -s http://localhost:5000/health || curl -s https://localhost:7201/health
  kill %1 2>/dev/null || true
  ```
  Expected: process starts, health endpoint responds, no unhandled exceptions in stderr.

- [ ] **Step 5.7** — Smoke-run without seed (should behave exactly as before):
  ```bash
  dotnet run --project samples/ExperimentFramework.DashboardHost -- &
  sleep 3 && curl -s http://localhost:5000/health || curl -s https://localhost:7201/health
  kill %1 2>/dev/null || true
  ```

- [ ] **Step 5.8** — Commit:
  ```bash
  git add samples/ExperimentFramework.DashboardHost/Program.cs
  git commit -m "feat(seeder): wire --seed=docs and --freeze-date CLI switches"
  ```

---

### Task 6: Rename `docs/user-guide/*.md` → `docs/reference/*.md` with xref redirects

**Estimated time:** 20–25 minutes

- [ ] **Step 6.1** — Create the `docs/reference/` directory and git-mv every `.md` file:
  ```bash
  mkdir -p docs/reference
  # Move all .md files from user-guide to reference
  for f in docs/user-guide/*.md docs/user-guide/**/*.md; do
    [ -f "$f" ] || continue
    rel="${f#docs/user-guide/}"
    target="docs/reference/$rel"
    mkdir -p "$(dirname "$target")"
    git mv "$f" "$target"
  done
  # Move the user-guide toc.yml too
  git mv docs/user-guide/toc.yml docs/reference/toc.yml 2>/dev/null || true
  ```

- [ ] **Step 6.2** — Update internal xref links within the moved files. All references like `xref:user-guide/foo` must become `xref:reference/foo`. Run a bulk in-place replacement:
  ```bash
  # Update xref links in all moved reference pages
  find docs/reference -name "*.md" -exec sed -i 's|xref:user-guide/|xref:reference/|g' {} +
  # Also update any remaining references in docs root .md files
  find docs -maxdepth 1 -name "*.md" -exec sed -i 's|xref:user-guide/|xref:reference/|g' {} +
  ```

- [ ] **Step 6.3** — Create xref redirect entries. DocFX does not have a native redirect mechanism; add a short `docs/redirects.md` that documents the moved pages and includes a JavaScript redirect snippet for direct URL visitors. Create `docs/redirects.md`:
  ```markdown
  # Redirects

  The pages previously under `user-guide/` have moved to `reference/`.

  Old path → New path:
  - `user-guide/getting-started` → `reference/getting-started`
  - `user-guide/governance` → `reference/governance`
  - (all other pages follow the same pattern)

  If you reached this page via an old bookmark, please update your link.
  ```
  Additionally, add a `uid` front-matter alias to the top of each moved file to preserve DocFX xref resolution. For example, open `docs/reference/getting-started.md` and add at the top:
  ```yaml
  ---
  uid: reference/getting-started
  aliases:
    - user-guide/getting-started
  ---
  ```
  Apply the same pattern to all moved files by looping through them.

- [ ] **Step 6.4** — Verify no broken references remain:
  ```bash
  grep -r "user-guide/" docs/ --include="*.md" | grep -v "redirects.md" | grep -v ".git"
  ```
  Expected: no matches (all `user-guide/` refs replaced).

- [ ] **Step 6.5** — Commit:
  ```bash
  git add docs/reference/ docs/redirects.md
  git add -u docs/user-guide/  # stage the git-mv deletions
  git commit -m "refactor(docs): rename docs/user-guide/ to docs/reference/ with xref redirect stubs"
  ```

---

### Task 7: Rewrite `docs/toc.yml` with the new top-level tree

**Estimated time:** 15–20 minutes

- [ ] **Step 7.1** — Replace the entire content of `docs/toc.yml` with the new structure. The `user-guide/` folder no longer exists (renamed to `reference/` in Task 6). The new User Guide section points to the subdirectories that will be created in Phase 2–4:
  ```yaml
  - name: Home
    href: index.md

  - name: Getting Started
    href: reference/getting-started.md

  - name: User Guide
    items:
      - name: Developer Setup
        items:
          - name: Embed the dashboard
            href: user-guide/developer-setup/embed.md
          - name: Run the sample host
            href: user-guide/developer-setup/sample-host.md
          - name: Production checklist
            href: user-guide/developer-setup/production-checklist.md

      - name: Operator Guide
        items:
          - name: Tutorials
            items:
              - name: Your first experiment
                href: user-guide/tutorials/first-experiment.md
              - name: Progressive rollout
                href: user-guide/tutorials/progressive-rollout.md
              - name: Approval & promotion
                href: user-guide/tutorials/approval-and-promotion.md
              - name: Analyzing results
                href: user-guide/tutorials/analyzing-results.md
              - name: Governance day-in-the-life
                href: user-guide/tutorials/governance-day-in-the-life.md
          - name: Reference
            items:
              - name: Home
                href: user-guide/reference/home.md
              - name: Experiments
                href: user-guide/reference/experiments.md
              - name: Create
                href: user-guide/reference/create.md
              - name: Analytics
                href: user-guide/reference/analytics.md
              - name: Hypothesis Testing
                href: user-guide/reference/hypothesis-testing.md
              - name: Targeting
                href: user-guide/reference/targeting.md
              - name: Rollout
                href: user-guide/reference/rollout.md
              - name: Plugins
                href: user-guide/reference/plugins.md
              - name: Configuration
                href: user-guide/reference/configuration.md
              - name: DSL Editor
                href: user-guide/reference/dsl-editor.md
              - name: Governance / Lifecycle
                href: user-guide/reference/governance-lifecycle.md
              - name: Governance / Approvals
                href: user-guide/reference/governance-approvals.md
              - name: Governance / Audit
                href: user-guide/reference/governance-audit.md
              - name: Governance / Policies
                href: user-guide/reference/governance-policies.md
              - name: Governance / Versions
                href: user-guide/reference/governance-versions.md

  - name: Reference
    href: reference/
    items:
      - name: Reference Overview
        href: reference/index.md

  - name: API
    href: api/
  ```

- [ ] **Step 7.2** — Create the placeholder stub directories so DocFX does not error on missing hrefs during the smoke test in Task 8:
  ```bash
  mkdir -p docs/user-guide/developer-setup
  mkdir -p docs/user-guide/tutorials
  mkdir -p docs/user-guide/reference
  # Create minimal stub files to prevent DocFX build errors
  echo "# Coming soon" > docs/user-guide/developer-setup/embed.md
  echo "# Coming soon" > docs/user-guide/developer-setup/sample-host.md
  echo "# Coming soon" > docs/user-guide/developer-setup/production-checklist.md
  for t in first-experiment progressive-rollout approval-and-promotion analyzing-results governance-day-in-the-life; do
    echo "# Coming soon" > "docs/user-guide/tutorials/$t.md"
  done
  for r in home experiments create analytics hypothesis-testing targeting rollout plugins configuration dsl-editor governance-lifecycle governance-approvals governance-audit governance-policies governance-versions; do
    echo "# Coming soon" > "docs/user-guide/reference/$r.md"
  done
  ```

- [ ] **Step 7.3** — Commit:
  ```bash
  git add docs/toc.yml docs/user-guide/
  git commit -m "feat(docs): rewrite toc.yml with User Guide tree; add stub pages for Phase 2-4 content"
  ```

---

### Task 8: Smoke test — seed DashboardHost, capture one probe screenshot, verify DocFX build

**Estimated time:** 20–30 minutes

- [ ] **Step 8.1** — Start DashboardHost with seed:
  ```bash
  dotnet run --project samples/ExperimentFramework.DashboardHost -- \
    --seed=docs --reset --freeze-date 2026-04-01T12:00:00Z &
  DASHBOARD_PID=$!
  ```

- [ ] **Step 8.2** — Wait for readiness:
  ```bash
  for i in $(seq 1 30); do
    if curl -sf http://localhost:5000/health > /dev/null 2>&1; then
      echo "Dashboard ready"; break
    fi
    echo "Waiting... ($i)"; sleep 2
  done
  ```
  Expected output: `Dashboard ready` within 60 seconds.

- [ ] **Step 8.3** — Create a probe Gherkin scenario. Add `tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsProbeScreenshot.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:experiments
  Feature: Docs Probe Screenshot
    A minimal smoke test to verify the screenshot infrastructure works.

  Scenario: Probe — capture experiments list
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/experiments"
    And I capture screenshot "experiments-list-probe"
    Then I should see "Experiments"
  ```

- [ ] **Step 8.4** — Run only the probe scenario:
  ```bash
  E2E__BaseUrl=http://localhost:5000 \
  dotnet test tests/ExperimentFramework.E2E.Tests \
    --filter "Category=DocsScreenshot&DisplayName~Probe"
  ```
  Expected: `1 passed, 0 failed`.

- [ ] **Step 8.5** — Verify PNG exists:
  ```bash
  ls -lh docs/images/screenshots/experiments/experiments-list-probe.png
  ```
  Expected: file exists, size ~100–400 KB.

- [ ] **Step 8.6** — Run DocFX build to confirm TOC is parseable:
  ```bash
  kill $DASHBOARD_PID 2>/dev/null || true
  dotnet tool update -g docfx
  docfx build docs/docfx.json --warningsAsErrors
  ```
  Expected: `Build succeeded` with 0 errors. Warnings about stub pages ("could not find article") are acceptable at this stage.

- [ ] **Step 8.7** — Remove the probe feature file and commit screenshots + stub files:
  ```bash
  rm tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsProbeScreenshot.feature
  git add docs/images/screenshots/experiments/experiments-list-probe.png
  git add tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsProbeScreenshot.feature
  git commit -m "test(smoke): verify screenshot infrastructure and DocFX build — remove probe feature"
  ```

---

## Phase 2 — Operator Guide Tutorials

### Task 9: Tutorial 1 — Your first experiment (6 screenshots)

**Estimated time:** 35–45 minutes

**Output files:**
- `docs/user-guide/tutorials/first-experiment.md`
- `tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsTutorial1FirstExperiment.feature`

**Screenshots:** `docs/images/screenshots/experiments/`
- `first-exp-dashboard-home.png`
- `first-exp-create-form.png`
- `first-exp-create-arms.png`
- `first-exp-create-submit.png`
- `first-exp-experiments-list-with-new.png`
- `first-exp-toggle-running.png`

- [ ] **Step 9.1** — Create `tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsTutorial1FirstExperiment.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:experiments
  Feature: Docs Tutorial 1 — Your First Experiment
    Captures all screenshots for the "Your first experiment" tutorial page.

  Scenario: Tutorial 1 — full walkthrough screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard"
    And I capture screenshot "first-exp-dashboard-home"
    When I navigate to "/dashboard/experiments/create"
    And I capture screenshot "first-exp-create-form"
    When I fill in the experiment name "button-color-test"
    And I fill in the description "Tests a blue vs green checkout button."
    And I capture screenshot "first-exp-create-arms"
    When I submit the create experiment form
    And I capture screenshot "first-exp-create-submit"
    When I navigate to "/dashboard/experiments"
    And I capture screenshot "first-exp-experiments-list-with-new"
    When I toggle the experiment "button-color-test" to running
    And I capture screenshot "first-exp-toggle-running"
    Then I should see "button-color-test"
  ```

- [ ] **Step 9.2** — Create `docs/user-guide/tutorials/first-experiment.md` with the following outline (executor expands each section to prose during implementation):

  ```markdown
  ---
  uid: user-guide/tutorials/first-experiment
  ---
  # Tutorial: Your First Experiment

  > **You'll learn:** How to create a 2-arm A/B experiment, enable it, and verify it appears in the list.
  > **You'll need:** Admin or Experimenter role. The sample host running with `--seed=docs`.
  > **~10 minutes.**

  ## Before you begin

  Confirm the dashboard is running and you can log in. Screenshot: `first-exp-dashboard-home.png` inserted here.
  (1-sentence callout pointing to the stats row at the top and the empty "Active" count before the new experiment is created.)

  ## Step 1: Open the Create form

  Navigate to **Experiments → Create**. Screenshot: `first-exp-create-form.png` inserted here.
  - Section 3.1: Section "Name & description" — fill `button-color-test` and the description.

  ## Step 2: Define your arms

  Two arms are pre-populated: Control and Variant A. Screenshot: `first-exp-create-arms.png` inserted here.
  - Section 3.2: Numbered list — (1) Arm name field, (2) IsDefault toggle, (3) Add Arm button.

  ## Step 3: Submit

  Click **Create Experiment**. Screenshot: `first-exp-create-submit.png` inserted here.
  - Section 3.3: Confirmation banner text.

  ## Step 4: See it in the list

  Return to Experiments. Screenshot: `first-exp-experiments-list-with-new.png` inserted here.
  - Section 3.4: Call out status badge ("Draft"), toggle control, and last-modified timestamp — numbered list of 3 items.

  ## Step 5: Toggle it on

  Use the kill-switch toggle to set the experiment to Running. Screenshot: `first-exp-toggle-running.png` inserted here.
  - Section 3.5: Status badge changes to "Running"; note the audit trail entry that is generated.

  ## What next

  - [Progressive rollout tutorial](progressive-rollout.md) — scale your experiment from 10% to 100%.
  - [Experiments reference](../reference/experiments.md) — full control reference for this page.
  ```

- [ ] **Step 9.3** — Build:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  ```

- [ ] **Step 9.4** — Commit:
  ```bash
  git add docs/user-guide/tutorials/first-experiment.md \
          tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsTutorial1FirstExperiment.feature
  git commit -m "docs(tutorial-1): Your first experiment page + @docs-screenshot scenario (6 screenshots)"
  ```

---

### Task 10: Tutorial 2 — Progressive rollout (5 screenshots)

**Estimated time:** 30–40 minutes

**Output files:**
- `docs/user-guide/tutorials/progressive-rollout.md`
- `tests/ExperimentFramework.E2E.Tests/Features/Rollout/DocsTutorial2ProgressiveRollout.feature`

**Screenshots:** `docs/images/screenshots/rollout/`
- `rollout-page-initial.png`
- `rollout-stage-10pct.png`
- `rollout-stage-50pct.png`
- `rollout-stage-100pct.png`
- `rollout-audit-trail.png`

- [ ] **Step 10.1** — Create `tests/ExperimentFramework.E2E.Tests/Features/Rollout/DocsTutorial2ProgressiveRollout.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:rollout
  Feature: Docs Tutorial 2 — Progressive Rollout
    Captures screenshots for the "Progressive rollout" tutorial.

  Scenario: Tutorial 2 — rollout stage screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/experiments/search-ranker-ml/rollout"
    And I capture screenshot "rollout-page-initial"
    When I set the rollout percentage to 10
    And I capture screenshot "rollout-stage-10pct"
    When I advance the rollout to 50
    And I capture screenshot "rollout-stage-50pct"
    When I advance the rollout to 100
    And I capture screenshot "rollout-stage-100pct"
    When I navigate to "/dashboard/governance/audit"
    And I capture screenshot "rollout-audit-trail"
    Then I should see "search-ranker-ml"
  ```

- [ ] **Step 10.2** — Create `docs/user-guide/tutorials/progressive-rollout.md` outline:

  ```markdown
  ---
  uid: user-guide/tutorials/progressive-rollout
  ---
  # Tutorial: Progressive Rollout

  > **You'll learn:** How to define 10/50/100% rollout stages, advance through them, and verify the audit trail.
  > **You'll need:** Admin or Experimenter role. The `search-ranker-ml` experiment from the demo seed.
  > **~8 minutes.**

  ## Before you begin

  Open the `search-ranker-ml` experiment and navigate to its Rollout tab.

  ## Step 1: Understand the Rollout page

  Screenshot: `rollout-page-initial.png`. Numbered list — (1) current rollout %, (2) stage history table, (3) Advance button.

  ## Step 2: Set 10% rollout

  Screenshot: `rollout-stage-10pct.png`. Stage added to history. Note the timestamp.

  ## Step 3: Advance to 50%

  Screenshot: `rollout-stage-50pct.png`. History now shows two stages.

  ## Step 4: Advance to 100%

  Screenshot: `rollout-stage-100pct.png`. Full rollout. Status badge changes to "Full Rollout".

  ## Step 5: Confirm in the audit trail

  Navigate to Governance → Audit. Screenshot: `rollout-audit-trail.png`. The three rollout-change events appear.

  ## What next

  - [Approval & promotion tutorial](approval-and-promotion.md) — learn to promote a fully-rolled-out experiment.
  - [Rollout reference](../reference/rollout.md) — detailed reference for all rollout controls.
  ```

- [ ] **Step 10.3** — Build and commit:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  git add docs/user-guide/tutorials/progressive-rollout.md \
          tests/ExperimentFramework.E2E.Tests/Features/Rollout/DocsTutorial2ProgressiveRollout.feature
  git commit -m "docs(tutorial-2): Progressive rollout page + @docs-screenshot scenario (5 screenshots)"
  ```

---

### Task 11: Tutorial 3 — Approval & promotion (6 screenshots)

**Estimated time:** 30–40 minutes

**Output files:**
- `docs/user-guide/tutorials/approval-and-promotion.md`
- `tests/ExperimentFramework.E2E.Tests/Features/Governance/DocsTutorial3ApprovalPromotion.feature`

**Screenshots:** `docs/images/screenshots/governance/`
- `approval-lifecycle-pending.png`
- `approval-approvals-list.png`
- `approval-approve-dialog.png`
- `approval-approved-status.png`
- `approval-promote-action.png`
- `approval-versions-snapshot.png`

- [ ] **Step 11.1** — Create `tests/ExperimentFramework.E2E.Tests/Features/Governance/DocsTutorial3ApprovalPromotion.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:governance
  Feature: Docs Tutorial 3 — Approval and Promotion
    Captures screenshots for the "Approval & promotion" tutorial.

  Scenario: Tutorial 3 — approval and promotion screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/governance/lifecycle"
    And I capture screenshot "approval-lifecycle-pending"
    When I navigate to "/dashboard/governance/approvals"
    And I capture screenshot "approval-approvals-list"
    When I open the approval request for "homepage-layout-fall2026"
    And I capture screenshot "approval-approve-dialog"
    When I approve the experiment "homepage-layout-fall2026"
    And I capture screenshot "approval-approved-status"
    When I navigate to "/dashboard/governance/lifecycle"
    And I promote the experiment "homepage-layout-fall2026"
    And I capture screenshot "approval-promote-action"
    When I navigate to "/dashboard/governance/versions"
    And I capture screenshot "approval-versions-snapshot"
    Then I should see "homepage-layout-fall2026"
  ```

- [ ] **Step 11.2** — Create `docs/user-guide/tutorials/approval-and-promotion.md` outline:

  ```markdown
  ---
  uid: user-guide/tutorials/approval-and-promotion
  ---
  # Tutorial: Approval & Promotion

  > **You'll learn:** How to submit an experiment for approval, review and approve it, promote it, and see the version snapshot.
  > **You'll need:** Admin role. The `homepage-layout-fall2026` experiment from the demo seed.
  > **~12 minutes.**

  ## Before you begin

  The `homepage-layout-fall2026` experiment is in `PendingApproval` state from the demo seed.

  ## Step 1: Lifecycle view

  Screenshot: `approval-lifecycle-pending.png`. Numbered list — (1) current stage, (2) approval badge, (3) available transitions.

  ## Step 2: Approvals queue

  Screenshot: `approval-approvals-list.png`. One pending item. Call out requester, timestamp, and notes field.

  ## Step 3: Open and approve

  Click the request. Screenshot: `approval-approve-dialog.png`. Approval dialog with notes field.
  Click Approve. Screenshot: `approval-approved-status.png`. Status badge changes to "Approved".

  ## Step 4: Promote

  Return to Lifecycle. Click Promote. Screenshot: `approval-promote-action.png`. Transition confirmation.

  ## Step 5: Version snapshot

  Navigate to Governance → Versions. Screenshot: `approval-versions-snapshot.png`. New snapshot entry.

  ## What next

  - [Analyzing results tutorial](analyzing-results.md) — what to do after promotion.
  - [Governance / Approvals reference](../reference/governance-approvals.md)
  - [Governance / Versions reference](../reference/governance-versions.md)
  ```

- [ ] **Step 11.3** — Build and commit:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  git add docs/user-guide/tutorials/approval-and-promotion.md \
          tests/ExperimentFramework.E2E.Tests/Features/Governance/DocsTutorial3ApprovalPromotion.feature
  git commit -m "docs(tutorial-3): Approval & promotion page + @docs-screenshot scenario (6 screenshots)"
  ```

---

### Task 12: Tutorial 4 — Analyzing results (5 screenshots)

**Estimated time:** 30–40 minutes

**Output files:**
- `docs/user-guide/tutorials/analyzing-results.md`
- `tests/ExperimentFramework.E2E.Tests/Features/Analytics/DocsTutorial4AnalyzingResults.feature`

**Screenshots:** `docs/images/screenshots/analytics/`
- `analytics-dashboard-overview.png`
- `analytics-winner-callout.png`
- `analytics-hypothesis-setup.png`
- `analytics-hypothesis-result.png`
- `analytics-export-dialog.png`

- [ ] **Step 12.1** — Create `tests/ExperimentFramework.E2E.Tests/Features/Analytics/DocsTutorial4AnalyzingResults.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:analytics
  Feature: Docs Tutorial 4 — Analyzing Results
    Captures screenshots for the "Analyzing results" tutorial.

  Scenario: Tutorial 4 — analytics and hypothesis testing screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/analytics"
    And I capture screenshot "analytics-dashboard-overview"
    When I select the experiment "checkout-button-v2"
    And I capture screenshot "analytics-winner-callout"
    When I navigate to "/dashboard/analytics/hypothesis"
    And I configure a hypothesis test for "checkout-button-v2"
    And I capture screenshot "analytics-hypothesis-setup"
    When I run the hypothesis test
    And I capture screenshot "analytics-hypothesis-result"
    When I open the export dialog
    And I capture screenshot "analytics-export-dialog"
    Then I should see "checkout-button-v2"
  ```

- [ ] **Step 12.2** — Create `docs/user-guide/tutorials/analyzing-results.md` outline:

  ```markdown
  ---
  uid: user-guide/tutorials/analyzing-results
  ---
  # Tutorial: Analyzing Results

  > **You'll learn:** How to open Analytics, read the winner callout, run a hypothesis test, and export your findings.
  > **You'll need:** Analyst or Admin role. `checkout-button-v2` from the demo seed with ~10k samples.
  > **~10 minutes.**

  ## Before you begin

  The demo seed pre-loads ~10k samples for `checkout-button-v2`. Variant A wins at p < 0.05.

  ## Step 1: Analytics overview

  Screenshot: `analytics-dashboard-overview.png`. Numbered list — (1) experiment selector, (2) time-range picker, (3) arm comparison chart.

  ## Step 2: Read the winner callout

  Screenshot: `analytics-winner-callout.png`. Green callout box — call out p-value, lift %, and confidence interval.

  ## Step 3: Set up a hypothesis test

  Navigate to Hypothesis Testing. Screenshot: `analytics-hypothesis-setup.png`. Fields: metric, significance level, power.

  ## Step 4: Run and read the result

  Screenshot: `analytics-hypothesis-result.png`. Result card with accept/reject decision.

  ## Step 5: Export

  Click Export. Screenshot: `analytics-export-dialog.png`. Format options: CSV, JSON, PDF.

  ## What next

  - [Governance day-in-the-life tutorial](governance-day-in-the-life.md)
  - [Analytics reference](../reference/analytics.md)
  - [Hypothesis Testing reference](../reference/hypothesis-testing.md)
  ```

- [ ] **Step 12.3** — Build and commit:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  git add docs/user-guide/tutorials/analyzing-results.md \
          tests/ExperimentFramework.E2E.Tests/Features/Analytics/DocsTutorial4AnalyzingResults.feature
  git commit -m "docs(tutorial-4): Analyzing results page + @docs-screenshot scenario (5 screenshots)"
  ```

---

### Task 13: Tutorial 5 — Governance day-in-the-life (5 screenshots)

**Estimated time:** 30–40 minutes

**Output files:**
- `docs/user-guide/tutorials/governance-day-in-the-life.md`
- `tests/ExperimentFramework.E2E.Tests/Features/Governance/DocsTutorial5GovernanceDayInTheLife.feature`

**Screenshots:** `docs/images/screenshots/governance/`
- `governance-policies-list.png`
- `governance-audit-violations.png`
- `governance-resolve-violation-dialog.png`
- `governance-lifecycle-transitions.png`
- `governance-day-complete.png`

- [ ] **Step 13.1** — Create `tests/ExperimentFramework.E2E.Tests/Features/Governance/DocsTutorial5GovernanceDayInTheLife.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:governance
  Feature: Docs Tutorial 5 — Governance Day-in-the-Life
    Captures screenshots for the "Governance day-in-the-life" tutorial.

  Scenario: Tutorial 5 — governance review screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/governance/policies"
    And I capture screenshot "governance-policies-list"
    When I navigate to "/dashboard/governance/audit"
    And I capture screenshot "governance-audit-violations"
    When I open the violation for "pricing-page-copy"
    And I capture screenshot "governance-resolve-violation-dialog"
    When I mark the violation as resolved
    When I navigate to "/dashboard/governance/lifecycle"
    And I capture screenshot "governance-lifecycle-transitions"
    When I navigate to "/dashboard/governance/audit"
    And I capture screenshot "governance-day-complete"
    Then I should see "pricing-page-copy"
  ```

- [ ] **Step 13.2** — Create `docs/user-guide/tutorials/governance-day-in-the-life.md` outline:

  ```markdown
  ---
  uid: user-guide/tutorials/governance-day-in-the-life
  ---
  # Tutorial: Governance Day-in-the-Life

  > **You'll learn:** How to review active policies, inspect and resolve a violation, and navigate lifecycle transitions.
  > **You'll need:** Admin role. `pricing-page-copy` policy violation from the demo seed.
  > **~15 minutes.**

  ## Before you begin

  The demo seed has one open violation (`min-sample-size-1000` on `pricing-page-copy`).

  ## Step 1: Review policies

  Screenshot: `governance-policies-list.png`. Three active policies. Table columns: name, description, active status.

  ## Step 2: Inspect the audit trail for violations

  Screenshot: `governance-audit-violations.png`. Filter by type "Policy Violation". Row for `pricing-page-copy`.

  ## Step 3: Resolve the violation

  Click the violation row. Screenshot: `governance-resolve-violation-dialog.png`. Resolution notes field.
  Click Resolve. Violation marked resolved.

  ## Step 4: Lifecycle transitions

  Screenshot: `governance-lifecycle-transitions.png`. Available transitions for paused experiments.

  ## Step 5: Confirm clean state in audit

  Screenshot: `governance-day-complete.png`. Audit trail now shows the resolution event.

  ## What next

  - [Governance / Policies reference](../reference/governance-policies.md)
  - [Governance / Audit reference](../reference/governance-audit.md)
  - [Governance / Lifecycle reference](../reference/governance-lifecycle.md)
  ```

- [ ] **Step 13.3** — Build and commit:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  git add docs/user-guide/tutorials/governance-day-in-the-life.md \
          tests/ExperimentFramework.E2E.Tests/Features/Governance/DocsTutorial5GovernanceDayInTheLife.feature
  git commit -m "docs(tutorial-5): Governance day-in-the-life page + @docs-screenshot scenario (5 screenshots)"
  ```

---

## Phase 3 — Operator Guide Reference Pages

### Task 14: Reference — Home, Experiments, Create (3 pages, ~9 screenshots)

**Estimated time:** 40–55 minutes

**Output files:**
- `docs/user-guide/reference/home.md`
- `docs/user-guide/reference/experiments.md`
- `docs/user-guide/reference/create.md`
- `tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsRefHomeExperimentsCreate.feature`

**Screenshots:**
- `docs/images/screenshots/experiments/`
  - `ref-home-overview.png`
  - `ref-home-active-cards.png`
  - `ref-home-stats-row.png`
  - `ref-experiments-list.png`
  - `ref-experiments-expanded.png`
  - `ref-experiments-filter.png`
  - `ref-create-form-empty.png`
  - `ref-create-form-filled.png`
  - `ref-create-confirmation.png`

- [ ] **Step 14.1** — Create `tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsRefHomeExperimentsCreate.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:experiments
  Feature: Docs Reference — Home, Experiments, Create
    Captures baseline and task screenshots for the Home, Experiments, and Create reference pages.

  Scenario: Ref — Home page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard"
    And I capture screenshot "ref-home-overview"
    When I capture screenshot "ref-home-active-cards"
    When I capture screenshot "ref-home-stats-row"
    Then I should see "Active Experiments"

  Scenario: Ref — Experiments page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/experiments"
    And I capture screenshot "ref-experiments-list"
    When I expand the first experiment
    And I capture screenshot "ref-experiments-expanded"
    When I filter experiments by category "Revenue"
    And I capture screenshot "ref-experiments-filter"
    Then I should see "checkout-button-v2"

  Scenario: Ref — Create experiment page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/experiments/create"
    And I capture screenshot "ref-create-form-empty"
    When I fill in the experiment name "docs-ref-example"
    And I fill in the description "Reference page example experiment."
    And I capture screenshot "ref-create-form-filled"
    When I submit the create experiment form
    And I capture screenshot "ref-create-confirmation"
    Then I should see "docs-ref-example"
  ```

- [ ] **Step 14.2** — Create `docs/user-guide/reference/home.md`:
  ```markdown
  ---
  uid: user-guide/reference/home
  ---
  # Home

  > The dashboard landing page gives a real-time pulse of your experiment portfolio.

  ![Home overview](../../images/screenshots/experiments/ref-home-overview.png)

  ## What you see

  1. **Stats row** — total, active, paused, and draft experiment counts.
  2. **Active experiment cards** — each running experiment with current rollout %, arm count, and last-modified date.
  3. **Quick-action links** — shortcut to Create and to Governance.

  ![Active cards](../../images/screenshots/experiments/ref-home-active-cards.png)

  ## Common tasks

  **Check how many experiments are running**
  The stats row updates in real time. The "Active" badge shows the count of experiments with status Running.

  ![Stats row](../../images/screenshots/experiments/ref-home-stats-row.png)

  ## Fields & controls

  | Control | What it does | When to use |
  |---------|-------------|-------------|
  | Stats row | Aggregate counts by status | Quick health check |
  | Experiment cards | Navigate to a specific experiment | When you know the name |
  | Create button | Opens the Create experiment form | Starting a new test |

  ## Troubleshooting

  **No experiments showing** — The list is empty if no experiments are seeded and no experiments have been created. Use `--seed=docs` to populate demo data.

  ## Related

  - [Your first experiment tutorial](../tutorials/first-experiment.md)
  - [Experiments reference](experiments.md)
  ```

- [ ] **Step 14.3** — Create `docs/user-guide/reference/experiments.md` following the same template (purpose sentence, baseline screenshot, What you see numbered list, Common tasks with screenshots, Fields & controls table, Troubleshooting, Related links). Reference `ref-experiments-list.png`, `ref-experiments-expanded.png`, `ref-experiments-filter.png`.

- [ ] **Step 14.4** — Create `docs/user-guide/reference/create.md` following the same template. Reference `ref-create-form-empty.png`, `ref-create-form-filled.png`, `ref-create-confirmation.png`.

- [ ] **Step 14.5** — Build:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  ```

- [ ] **Step 14.6** — Commit:
  ```bash
  git add docs/user-guide/reference/home.md \
          docs/user-guide/reference/experiments.md \
          docs/user-guide/reference/create.md \
          tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsRefHomeExperimentsCreate.feature
  git commit -m "docs(ref-1): Home, Experiments, Create reference pages + @docs-screenshot scenarios (9 screenshots)"
  ```

---

### Task 15: Reference — Analytics, Hypothesis Testing (2 pages, ~6 screenshots)

**Estimated time:** 30–40 minutes

**Output files:**
- `docs/user-guide/reference/analytics.md`
- `docs/user-guide/reference/hypothesis-testing.md`
- `tests/ExperimentFramework.E2E.Tests/Features/Analytics/DocsRefAnalyticsHypothesis.feature`

**Screenshots:** `docs/images/screenshots/analytics/`
- `ref-analytics-overview.png`
- `ref-analytics-arm-detail.png`
- `ref-analytics-winner-badge.png`
- `ref-hypothesis-form.png`
- `ref-hypothesis-result-accept.png`
- `ref-hypothesis-result-reject.png`

- [ ] **Step 15.1** — Create `tests/ExperimentFramework.E2E.Tests/Features/Analytics/DocsRefAnalyticsHypothesis.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:analytics
  Feature: Docs Reference — Analytics and Hypothesis Testing
    Captures screenshots for the Analytics and Hypothesis Testing reference pages.

  Scenario: Ref — Analytics page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/analytics"
    And I capture screenshot "ref-analytics-overview"
    When I select the experiment "checkout-button-v2"
    And I capture screenshot "ref-analytics-arm-detail"
    And I capture screenshot "ref-analytics-winner-badge"
    Then I should see "checkout-button-v2"

  Scenario: Ref — Hypothesis Testing page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/analytics/hypothesis"
    And I capture screenshot "ref-hypothesis-form"
    When I configure a hypothesis test for "checkout-button-v2"
    And I run the hypothesis test
    And I capture screenshot "ref-hypothesis-result-accept"
    When I configure a hypothesis test for "search-ranker-ml"
    And I run the hypothesis test
    And I capture screenshot "ref-hypothesis-result-reject"
    Then I should see "Hypothesis"
  ```

- [ ] **Step 15.2** — Create `docs/user-guide/reference/analytics.md` and `docs/user-guide/reference/hypothesis-testing.md` following the reference page template. Each page: purpose sentence → baseline screenshot → What you see list → Common tasks with screenshots → Fields & controls table → Troubleshooting → Related.

- [ ] **Step 15.3** — Build and commit:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  git add docs/user-guide/reference/analytics.md \
          docs/user-guide/reference/hypothesis-testing.md \
          tests/ExperimentFramework.E2E.Tests/Features/Analytics/DocsRefAnalyticsHypothesis.feature
  git commit -m "docs(ref-2): Analytics and Hypothesis Testing reference pages + @docs-screenshot scenarios (6 screenshots)"
  ```

---

### Task 16: Reference — Targeting, Rollout (2 pages, ~6 screenshots)

**Estimated time:** 30–40 minutes

**Output files:**
- `docs/user-guide/reference/targeting.md`
- `docs/user-guide/reference/rollout.md`
- `tests/ExperimentFramework.E2E.Tests/Features/Rollout/DocsRefTargetingRollout.feature`

**Screenshots:**
- `docs/images/screenshots/targeting/`
  - `ref-targeting-overview.png`
  - `ref-targeting-rule-form.png`
  - `ref-targeting-saved.png`
- `docs/images/screenshots/rollout/`
  - `ref-rollout-overview.png`
  - `ref-rollout-stage-history.png`
  - `ref-rollout-advance-dialog.png`

- [ ] **Step 16.1** — Create `tests/ExperimentFramework.E2E.Tests/Features/Rollout/DocsRefTargetingRollout.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:targeting
  Feature: Docs Reference — Targeting and Rollout
    Captures screenshots for the Targeting and Rollout reference pages.

  Scenario: Ref — Targeting page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/experiments/checkout-button-v2/targeting"
    And I capture screenshot "ref-targeting-overview"
    When I add a targeting rule for attribute "country" equals "US"
    And I capture screenshot "ref-targeting-rule-form"
    When I save the targeting rules
    And I capture screenshot "ref-targeting-saved"
    Then I should see "checkout-button-v2"

  @screenshot-area:rollout
  Scenario: Ref — Rollout page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/experiments/search-ranker-ml/rollout"
    And I capture screenshot "ref-rollout-overview"
    When I capture screenshot "ref-rollout-stage-history"
    When I click the "Advance Rollout" button
    And I capture screenshot "ref-rollout-advance-dialog"
    Then I should see "search-ranker-ml"
  ```

- [ ] **Step 16.2** — Create `docs/user-guide/reference/targeting.md` and `docs/user-guide/reference/rollout.md` following the reference template.

- [ ] **Step 16.3** — Build and commit:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  git add docs/user-guide/reference/targeting.md \
          docs/user-guide/reference/rollout.md \
          tests/ExperimentFramework.E2E.Tests/Features/Rollout/DocsRefTargetingRollout.feature
  git commit -m "docs(ref-3): Targeting and Rollout reference pages + @docs-screenshot scenarios (6 screenshots)"
  ```

---

### Task 17: Reference — Plugins, Configuration, DSL Editor (3 pages, ~9 screenshots)

**Estimated time:** 35–45 minutes

**Output files:**
- `docs/user-guide/reference/plugins.md`
- `docs/user-guide/reference/configuration.md`
- `docs/user-guide/reference/dsl-editor.md`
- `tests/ExperimentFramework.E2E.Tests/Features/Plugins/DocsRefPluginsConfigDsl.feature`

**Screenshots:**
- `docs/images/screenshots/plugins/`
  - `ref-plugins-overview.png`
  - `ref-plugins-detail.png`
  - `ref-plugins-enable-dialog.png`
- `docs/images/screenshots/configuration/`
  - `ref-configuration-overview.png`
  - `ref-configuration-edit-form.png`
  - `ref-configuration-saved.png`
  - `ref-dsl-editor-overview.png`
  - `ref-dsl-editor-validation-ok.png`
  - `ref-dsl-editor-validation-error.png`

- [ ] **Step 17.1** — Create `tests/ExperimentFramework.E2E.Tests/Features/Plugins/DocsRefPluginsConfigDsl.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:plugins
  Feature: Docs Reference — Plugins, Configuration, DSL Editor
    Captures screenshots for the Plugins, Configuration, and DSL Editor reference pages.

  Scenario: Ref — Plugins page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/plugins"
    And I capture screenshot "ref-plugins-overview"
    When I click on the first plugin
    And I capture screenshot "ref-plugins-detail"
    When I click the "Enable" button
    And I capture screenshot "ref-plugins-enable-dialog"
    Then I should see "Plugins"

  @screenshot-area:configuration
  Scenario: Ref — Configuration page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/configuration"
    And I capture screenshot "ref-configuration-overview"
    When I click the "Edit" button
    And I capture screenshot "ref-configuration-edit-form"
    When I save the configuration
    And I capture screenshot "ref-configuration-saved"
    Then I should see "Configuration"

  @screenshot-area:configuration
  Scenario: Ref — DSL Editor page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/dsl-editor"
    And I capture screenshot "ref-dsl-editor-overview"
    When I enter valid DSL content
    And I capture screenshot "ref-dsl-editor-validation-ok"
    When I introduce a syntax error
    And I capture screenshot "ref-dsl-editor-validation-error"
    Then I should see "DSL"
  ```

- [ ] **Step 17.2** — Create `docs/user-guide/reference/plugins.md`, `docs/user-guide/reference/configuration.md`, and `docs/user-guide/reference/dsl-editor.md` following the reference template.

- [ ] **Step 17.3** — Build and commit:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  git add docs/user-guide/reference/plugins.md \
          docs/user-guide/reference/configuration.md \
          docs/user-guide/reference/dsl-editor.md \
          tests/ExperimentFramework.E2E.Tests/Features/Plugins/DocsRefPluginsConfigDsl.feature
  git commit -m "docs(ref-4): Plugins, Configuration, DSL Editor reference pages + @docs-screenshot scenarios (9 screenshots)"
  ```

---

### Task 18: Reference — Governance suite (Lifecycle, Approvals, Audit, Policies, Versions — 5 pages, ~15 screenshots)

**Estimated time:** 50–65 minutes

**Output files:**
- `docs/user-guide/reference/governance-lifecycle.md`
- `docs/user-guide/reference/governance-approvals.md`
- `docs/user-guide/reference/governance-audit.md`
- `docs/user-guide/reference/governance-policies.md`
- `docs/user-guide/reference/governance-versions.md`
- `tests/ExperimentFramework.E2E.Tests/Features/Governance/DocsRefGovernanceSuite.feature`

**Screenshots:** `docs/images/screenshots/governance/`
- `ref-lifecycle-overview.png`
- `ref-lifecycle-transition-dialog.png`
- `ref-lifecycle-history.png`
- `ref-approvals-queue.png`
- `ref-approvals-detail.png`
- `ref-approvals-approve-action.png`
- `ref-audit-full-log.png`
- `ref-audit-filter-type.png`
- `ref-audit-event-detail.png`
- `ref-policies-list.png`
- `ref-policies-create-form.png`
- `ref-policies-violation-badge.png`
- `ref-versions-list.png`
- `ref-versions-diff.png`
- `ref-versions-restore-dialog.png`

- [ ] **Step 18.1** — Create `tests/ExperimentFramework.E2E.Tests/Features/Governance/DocsRefGovernanceSuite.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:governance
  Feature: Docs Reference — Governance Suite
    Captures screenshots for all five Governance reference pages.

  Scenario: Ref — Lifecycle page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/governance/lifecycle"
    And I capture screenshot "ref-lifecycle-overview"
    When I open a lifecycle transition for "pricing-page-copy"
    And I capture screenshot "ref-lifecycle-transition-dialog"
    When I close the dialog
    And I capture screenshot "ref-lifecycle-history"
    Then I should see "Lifecycle"

  Scenario: Ref — Approvals page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/governance/approvals"
    And I capture screenshot "ref-approvals-queue"
    When I open the approval request for "homepage-layout-fall2026"
    And I capture screenshot "ref-approvals-detail"
    When I click the "Approve" button
    And I capture screenshot "ref-approvals-approve-action"
    Then I should see "Approvals"

  Scenario: Ref — Audit page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/governance/audit"
    And I capture screenshot "ref-audit-full-log"
    When I filter the audit log by type "PolicyViolation"
    And I capture screenshot "ref-audit-filter-type"
    When I open the first audit event
    And I capture screenshot "ref-audit-event-detail"
    Then I should see "Audit"

  Scenario: Ref — Policies page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/governance/policies"
    And I capture screenshot "ref-policies-list"
    When I click the "Create Policy" button
    And I capture screenshot "ref-policies-create-form"
    When I navigate to "/dashboard/governance/policies"
    And I capture screenshot "ref-policies-violation-badge"
    Then I should see "Policies"

  Scenario: Ref — Versions page screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/governance/versions"
    And I capture screenshot "ref-versions-list"
    When I open the version diff for "checkout-button-v2"
    And I capture screenshot "ref-versions-diff"
    When I click the "Restore" button
    And I capture screenshot "ref-versions-restore-dialog"
    Then I should see "Versions"
  ```

- [ ] **Step 18.2** — Create all five governance reference pages following the standard template:
  - `docs/user-guide/reference/governance-lifecycle.md` — reference `ref-lifecycle-*.png`
  - `docs/user-guide/reference/governance-approvals.md` — reference `ref-approvals-*.png`
  - `docs/user-guide/reference/governance-audit.md` — reference `ref-audit-*.png`
  - `docs/user-guide/reference/governance-policies.md` — reference `ref-policies-*.png`
  - `docs/user-guide/reference/governance-versions.md` — reference `ref-versions-*.png`

  Each page: purpose → baseline screenshot → What you see numbered list → Common tasks with screenshots → Fields & controls table → Troubleshooting → Related. Cross-link each governance page to the relevant tutorials (Tutorial 3 and 5).

- [ ] **Step 18.3** — Build:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  ```

- [ ] **Step 18.4** — Commit:
  ```bash
  git add docs/user-guide/reference/governance-lifecycle.md \
          docs/user-guide/reference/governance-approvals.md \
          docs/user-guide/reference/governance-audit.md \
          docs/user-guide/reference/governance-policies.md \
          docs/user-guide/reference/governance-versions.md \
          tests/ExperimentFramework.E2E.Tests/Features/Governance/DocsRefGovernanceSuite.feature
  git commit -m "docs(ref-5): Governance suite reference pages + @docs-screenshot scenarios (15 screenshots)"
  ```

---

## Phase 4 — Developer Setup

### Task 19: `developer-setup/embed.md` with 2 screenshots

**Estimated time:** 25–35 minutes

**Output files:**
- `docs/user-guide/developer-setup/embed.md`
- `tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsDevSetupEmbed.feature`

**Screenshots:** `docs/images/screenshots/developer-setup/`
- `dashboard-first-load.png`
- `dashboard-with-experiment.png`

- [ ] **Step 19.1** — Create `tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsDevSetupEmbed.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:developer-setup
  Feature: Docs Developer Setup — Embed the Dashboard
    Captures the two screenshots for the embed.md developer setup page.

  Scenario: Dev setup — empty state and first experiment screenshots
    Given I am logged in as "Admin"
    When I navigate to "/dashboard"
    And I capture screenshot "dashboard-first-load"
    When I navigate to "/dashboard/experiments"
    And I capture screenshot "dashboard-with-experiment"
    Then I should see "Experiments"
  ```

- [ ] **Step 19.2** — Create `docs/user-guide/developer-setup/embed.md`:
  ```markdown
  ---
  uid: user-guide/developer-setup/embed
  ---
  # Embed the Dashboard

  This page shows you how to add the ExperimentFramework dashboard to an existing ASP.NET Core application
  in under 10 minutes.

  ## Prerequisites

  - .NET 10 SDK
  - An existing ASP.NET Core project (Blazor Server, Razor Pages, or MVC)

  ## Step 1: Add NuGet packages

  ```bash
  dotnet add package ExperimentFramework.Dashboard
  dotnet add package ExperimentFramework.Dashboard.UI
  dotnet add package ExperimentFramework.Dashboard.Api
  ```

  ## Step 2: Register services

  In `Program.cs`, add the following before `builder.Build()`:

  ```csharp
  builder.Services.AddExperimentDashboard(options =>
  {
      options.PathBase = "/dashboard";
      options.Title   = "My App — Experiments";
      options.EnableAnalytics     = true;
      options.EnableGovernanceUI  = true;
      options.RequireAuthorization = true; // set to false during development only
  });
  ```

  ## Step 3: Map the dashboard endpoint

  After `var app = builder.Build()`, add:

  ```csharp
  app.MapExperimentDashboard("/dashboard");
  ```

  ## Step 4: Configure authentication (optional)

  If `RequireAuthorization = true`, configure your policy:

  ```csharp
  builder.Services.AddAuthorization(options =>
  {
      options.AddPolicy("DashboardAccess", policy =>
          policy.RequireAuthenticatedUser().RequireRole("Admin", "Experimenter"));
  });
  ```

  Then pass the policy name:

  ```csharp
  options.RequireAuthorization = true;
  options.AuthorizationPolicy  = "DashboardAccess";
  ```

  See [Production checklist](production-checklist.md) for OIDC / cookie auth integration.

  ## Step 5: Verify

  Run your application and browse to `/dashboard`. With no experiments yet:

  ![Dashboard first load — empty state](../../images/screenshots/developer-setup/dashboard-first-load.png)

  1. The stats row shows `0` for all counts — this is correct for an empty seed.
  2. The "Create Experiment" button is visible in the top-right corner.
  3. Navigation links for Governance and Analytics are present.

  After creating your first experiment:

  ![Dashboard with one experiment](../../images/screenshots/developer-setup/dashboard-with-experiment.png)

  1. The "Active" count increments when you toggle the experiment to Running.
  2. The experiment card appears with its name, status badge, and rollout percentage.

  ## Multi-tenancy

  To isolate experiments per tenant, pass a `TenantResolver`:

  ```csharp
  options.TenantResolver = new HttpHeaderTenantResolver("X-Tenant-Id");
  ```

  Available resolvers: `HttpHeaderTenantResolver`, `SubdomainTenantResolver`, `ClaimTenantResolver`,
  `CompositeTenantResolver`. See the [tenancy reference](../../reference/configuration.md) for details.

  ## Related

  - [Run the sample host](sample-host.md) — explore a pre-seeded dashboard without any application code
  - [Production checklist](production-checklist.md) — what to review before going live
  ```

- [ ] **Step 19.3** — Build and commit:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  git add docs/user-guide/developer-setup/embed.md \
          tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsDevSetupEmbed.feature
  git commit -m "docs(dev-setup-1): embed.md developer setup page + @docs-screenshot scenario (2 screenshots)"
  ```

---

### Task 20: `developer-setup/sample-host.md` with 1 screenshot

**Estimated time:** 20–25 minutes

**Output files:**
- `docs/user-guide/developer-setup/sample-host.md`
- `tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsDevSetupSampleHost.feature`

**Screenshots:** `docs/images/screenshots/developer-setup/`
- `sample-host-experiments.png`

- [ ] **Step 20.1** — Create `tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsDevSetupSampleHost.feature`:
  ```gherkin
  @docs-screenshot @screenshot-area:developer-setup
  Feature: Docs Developer Setup — Sample Host
    Captures the screenshot for the sample-host.md developer setup page.

  Scenario: Dev setup — sample host experiments list
    Given I am logged in as "Admin"
    When I navigate to "/dashboard/experiments"
    And I capture screenshot "sample-host-experiments"
    Then I should see "checkout-button-v2"
  ```

- [ ] **Step 20.2** — Create `docs/user-guide/developer-setup/sample-host.md`:
  ```markdown
  ---
  uid: user-guide/developer-setup/sample-host
  ---
  # Run the Sample Host

  The `ExperimentFramework.DashboardHost` sample project lets you explore the full dashboard
  against a curated demo dataset — no application code needed.

  ## Clone and run

  ```bash
  git clone https://github.com/your-org/ExperimentFramework.git
  cd ExperimentFramework
  dotnet run --project samples/ExperimentFramework.DashboardHost -- \
    --seed=docs --freeze-date 2026-04-01T12:00:00Z
  ```

  Then browse to `http://localhost:5000/dashboard` (or `https://localhost:7201/dashboard`).

  **Default credentials:**
  | Role | Email | Password |
  |------|-------|----------|
  | Admin | admin@experimentdemo.com | Admin123! |
  | Experimenter | experimenter@experimentdemo.com | Experimenter123! |
  | Viewer | viewer@experimentdemo.com | Viewer123! |
  | Analyst | analyst@experimentdemo.com | Analyst123! |

  ## What the seed produces

  The `--seed=docs` flag runs `DocsDemoSeeder`, which creates five experiments across a range
  of lifecycle states (Running, Paused, PendingApproval, Archived), plus ~10k analytics samples,
  audit events, governance policies, a policy violation, and version snapshots.
  See [Docs-Demo Seeder](../../reference/samples.md) for the full inventory.

  ## Five pages to explore first

  | Page | Why |
  |------|-----|
  | [Experiments](../reference/experiments.md) | See all five seeded experiments and their states |
  | [Analytics](../reference/analytics.md) | `checkout-button-v2` has a stat-sig winner |
  | [Governance / Approvals](../reference/governance-approvals.md) | One pending approval for `homepage-layout-fall2026` |
  | [Governance / Audit](../reference/governance-audit.md) | 20 mixed audit events |
  | [Governance / Policies](../reference/governance-policies.md) | Three active policies + one violation |

  ![Sample host experiments list with seeded data](../../images/screenshots/developer-setup/sample-host-experiments.png)

  1. Five experiments visible — two Running, one Paused, one PendingApproval, one Archived.
  2. Status badges use colour coding: green = Running, yellow = Pending, red = Paused, grey = Archived.
  3. Rollout percentages shown inline on each row.

  ## Resetting and re-seeding

  ```bash
  dotnet run --project samples/ExperimentFramework.DashboardHost -- \
    --seed=docs --reset --freeze-date 2026-04-01T12:00:00Z
  ```

  The `--reset` flag wipes all existing state before re-seeding. Useful after running tutorials
  that modify seeded experiments.

  ## Related

  - [Embed the dashboard](embed.md) — add the dashboard to your own application
  - [Production checklist](production-checklist.md)
  ```

- [ ] **Step 20.3** — Build and commit:
  ```bash
  dotnet build tests/ExperimentFramework.E2E.Tests/ExperimentFramework.E2E.Tests.csproj
  git add docs/user-guide/developer-setup/sample-host.md \
          tests/ExperimentFramework.E2E.Tests/Features/Experiments/DocsDevSetupSampleHost.feature
  git commit -m "docs(dev-setup-2): sample-host.md developer setup page + @docs-screenshot scenario (1 screenshot)"
  ```

---

### Task 21: `developer-setup/production-checklist.md` (no screenshots)

**Estimated time:** 15–20 minutes

**Output file:** `docs/user-guide/developer-setup/production-checklist.md`

- [ ] **Step 21.1** — Create `docs/user-guide/developer-setup/production-checklist.md`:
  ```markdown
  ---
  uid: user-guide/developer-setup/production-checklist
  ---
  # Production Checklist

  Before deploying ExperimentFramework in a production environment, verify each item below.

  ## Authentication & Authorization

  - [ ] `DashboardOptions.RequireAuthorization` is `true`
  - [ ] An authorization policy (`DashboardOptions.AuthorizationPolicy`) is configured and tested
  - [ ] Dashboard routes are not accessible to unauthenticated users (verify with a logged-out browser tab)
  - [ ] If using OIDC, the callback URL is registered with your identity provider
  - [ ] If using cookie auth, `SlidingExpiration` and `ExpireTimeSpan` are set appropriately

  ## Durable Backplane

  - [ ] The default in-memory backplane is replaced with a durable backplane (Redis, SQL, or custom)
  - [ ] Experiment state survives application restarts
  - [ ] Backplane connection string is stored in a secret manager, not in `appsettings.json`

  ## Governance Approvers

  - [ ] At least two named approvers are configured for experiments requiring approval
  - [ ] The `require-two-approvers` policy (or equivalent) is active
  - [ ] Approver email notifications are wired (webhook or SMTP)

  ## Telemetry

  - [ ] OpenTelemetry traces and metrics are exported to your observability stack
  - [ ] The experiment assignment metric (`experiment.assignment.count`) is visible in dashboards
  - [ ] Alerting is configured on circuit-breaker trips

  ## TLS & Forwarded Headers

  - [ ] `UseHttpsRedirection()` is enabled in production
  - [ ] `UseForwardedHeaders()` is configured if the app sits behind a reverse proxy
  - [ ] `KnownProxies` or `KnownNetworks` is restricted (do not use `ClearKnownNetworks()` in production)

  ## Kill Switch

  - [ ] A kill-switch mechanism is in place to disable all experiments globally in an emergency
  - [ ] Kill-switch activation is logged to the audit trail

  ## Backup & Restore

  - [ ] Experiment definitions are backed up on a schedule
  - [ ] A restore procedure has been tested in a staging environment
  - [ ] Version snapshots are retained for a minimum of 90 days

  ## Related

  - [Embed the dashboard](embed.md)
  - [Run the sample host](sample-host.md)
  - [Governance / Policies reference](../reference/governance-policies.md)
  ```

- [ ] **Step 21.2** — Commit:
  ```bash
  git add docs/user-guide/developer-setup/production-checklist.md
  git commit -m "docs(dev-setup-3): production-checklist.md developer setup page (no screenshots)"
  ```

---

## Phase 5 — CI Automation

### Task 22: Write `.github/workflows/docs-screenshots.yml`

**Estimated time:** 25–35 minutes

- [ ] **Step 22.1** — Create `.github/workflows/docs-screenshots.yml`:
  ```yaml
  name: Docs Screenshots

  on:
    schedule:
      # Weekly Monday 03:00 UTC
      - cron: '0 3 * * 1'
    workflow_dispatch:

  permissions:
    contents: write
    pull-requests: write

  jobs:
    regen-screenshots:
      runs-on: ubuntu-latest
      timeout-minutes: 30

      steps:
        - name: Checkout
          uses: actions/checkout@v4

        - name: Setup .NET 10
          uses: actions/setup-dotnet@v5
          with:
            dotnet-version: 10.x

        - name: Restore
          run: dotnet restore ExperimentFramework.slnx

        - name: Build
          run: dotnet build ExperimentFramework.slnx --configuration Release --no-restore

        - name: Install Playwright browsers (Chromium only)
          run: |
            dotnet tool install --global Microsoft.Playwright.CLI 2>/dev/null || true
            pwsh tests/ExperimentFramework.E2E.Tests/bin/Release/net10.0/playwright.ps1 install chromium --with-deps

        - name: Launch DashboardHost (background)
          run: |
            dotnet run --project samples/ExperimentFramework.DashboardHost \
              --configuration Release -- \
              --seed=docs --reset --freeze-date 2026-04-01T12:00:00Z &
            echo "DASHBOARD_PID=$!" >> $GITHUB_ENV

        - name: Wait for DashboardHost readiness
          run: |
            for i in $(seq 1 30); do
              if curl -sf http://localhost:5000/health > /dev/null 2>&1; then
                echo "Dashboard is ready after $i attempts"
                exit 0
              fi
              echo "Attempt $i — waiting 3s..."
              sleep 3
            done
            echo "Dashboard did not become ready in 90 seconds" >&2
            exit 1

        - name: Run docs screenshot suite
          env:
            E2E__BaseUrl: http://localhost:5000
            E2E__Headless: true
          run: |
            dotnet test tests/ExperimentFramework.E2E.Tests \
              --configuration Release \
              --filter "Category=DocsScreenshot" \
              --logger "trx;LogFileName=docs-screenshots.trx"

        - name: Stop DashboardHost
          if: always()
          run: kill $DASHBOARD_PID 2>/dev/null || true

        - name: Compress PNGs with oxipng
          run: |
            cargo install oxipng 2>/dev/null || true
            if command -v oxipng > /dev/null 2>&1; then
              find docs/images/screenshots -name "*.png" -exec oxipng -o 2 {} \;
              echo "oxipng compression applied"
            else
              echo "oxipng not available — skipping compression"
            fi

        - name: Detect screenshot drift
          id: drift
          run: |
            if git diff --quiet docs/images/screenshots/; then
              echo "has_drift=false" >> $GITHUB_OUTPUT
              echo "No screenshot drift detected"
            else
              echo "has_drift=true" >> $GITHUB_OUTPUT
              echo "Screenshot drift detected:"
              git diff --stat docs/images/screenshots/
            fi

        - name: Open drift PR
          if: steps.drift.outputs.has_drift == 'true'
          uses: peter-evans/create-pull-request@v6
          with:
            commit-message: "docs: regenerate screenshots (${{ github.run_started_at }})"
            title: "docs: regenerate screenshots (${{ github.run_started_at }})"
            body: |
              Automated screenshot regeneration triggered by the weekly `docs-screenshots` workflow.

              ## Changes
              Screenshots in `docs/images/screenshots/` have drifted from the committed versions.
              Review the diff to confirm the changes reflect expected UI updates.

              ## How to merge
              If the new screenshots look correct, approve and squash-merge this PR.
              If they reveal a regression, fix the UI before merging.

              ---
              *Generated by `docs-screenshots.yml` on ${{ github.run_started_at }}*
            branch: docs/screenshot-drift-${{ github.run_id }}
            labels: docs-screenshots
            delete-branch: true
  ```

- [ ] **Step 22.2** — Commit:
  ```bash
  git add .github/workflows/docs-screenshots.yml
  git commit -m "feat(ci): add docs-screenshots.yml weekly workflow with drift auto-PR"
  ```

---

### Task 23: Manually dispatch the workflow and verify end-to-end

**Estimated time:** 20–30 minutes (plus workflow run time ~15–20 min)

- [ ] **Step 23.1** — Push the current branch so the workflow file is visible to GitHub Actions:
  ```bash
  git push -u origin docs/user-guide-and-screenshots
  ```

- [ ] **Step 23.2** — Dispatch the workflow manually:
  ```bash
  gh workflow run docs-screenshots.yml --ref docs/user-guide-and-screenshots
  ```
  Expected output: `Created workflow_dispatch event for docs-screenshots.yml at docs/user-guide-and-screenshots`

- [ ] **Step 23.3** — Wait for the run to complete and inspect results:
  ```bash
  # Wait for run to appear (may take ~30s for the event to register)
  sleep 30
  gh run list --workflow=docs-screenshots.yml --limit=1
  # Stream logs once run ID is known
  RUN_ID=$(gh run list --workflow=docs-screenshots.yml --limit=1 --json databaseId -q '.[0].databaseId')
  gh run watch $RUN_ID
  ```

- [ ] **Step 23.4** — If the run fails, inspect the log:
  ```bash
  gh run view $RUN_ID --log-failed
  ```
  Common failure modes and fixes:
  - **DashboardHost health check timeout** — increase the sleep loop limit or check the host's `--urls` default port.
  - **Playwright browser not installed** — verify the `playwright.ps1 install chromium` step ran; check binary path in `bin/Release/net10.0/`.
  - **`dotnet test` filter finds 0 tests** — verify `@docs-screenshot` tag is present on feature files and Reqnroll maps it to `Category=DocsScreenshot` (check a generated `.feature.cs` file).
  - **oxipng install failure** — the step already gracefully skips if unavailable; not a blocking issue.

- [ ] **Step 23.5** — If drift is detected (expected on first run since PNGs are new), review the opened PR via:
  ```bash
  gh pr list --label docs-screenshots
  ```

- [ ] **Step 23.6** — Document any fixes applied in a commit:
  ```bash
  git add -A
  git commit -m "fix(ci): docs-screenshots workflow fixes from first dispatch run"
  ```
  (Omit this step if no fixes were needed.)

---

## Phase 6 — Verification & Merge

### Task 24: Run full verification

**Estimated time:** 20–30 minutes

- [ ] **Step 24.1** — Full solution build:
  ```bash
  dotnet build ExperimentFramework.slnx --configuration Release
  ```
  Expected:
  ```
  Build succeeded.
      0 Error(s)
      0 Warning(s) (or only pre-existing warnings unrelated to this branch)
  ```

- [ ] **Step 24.2** — Unit and integration tests (excluding DocsScreenshot):
  ```bash
  dotnet test ExperimentFramework.slnx \
    --configuration Release \
    -p:ExcludeE2ETests=true \
    --filter "Category!=DocsScreenshot"
  ```
  Expected:
  ```
  Test Run Successful.
  Total tests: N
  Passed: N
  Failed: 0
  ```

- [ ] **Step 24.3** — DocFX build:
  ```bash
  cd docs && docfx build docfx.json
  ```
  Expected:
  ```
  Build succeeded in X seconds.
  ```
  Open `docs/_site/index.html` in a browser and navigate: Home → User Guide → Developer Setup → Operator Guide → Tutorials → Reference. Confirm the new TOC tree renders correctly.

- [ ] **Step 24.4** — Link check with lychee (via Docker):
  ```bash
  docker run --rm \
    -v "$(pwd)/docs:/docs" \
    lycheeverse/lychee:latest \
    --base /docs/_site \
    --exclude-mail \
    "/docs/_site/**/*.html"
  ```
  Expected: `0 errors` (broken anchor links to screenshot images are acceptable if PNGs have not yet been committed; all `href` links to other pages must pass).

  If Docker is not available, use the `markdown-link-check` npm package as a fallback:
  ```bash
  npx markdown-link-check docs/user-guide/**/*.md --quiet
  ```

- [ ] **Step 24.5** — Verify screenshot count:
  ```bash
  find docs/images/screenshots -name "*.png" | wc -l
  ```
  Expected: approximately 72 (range 65–75 is acceptable; the probe screenshot from Task 8 may or may not be present).

- [ ] **Step 24.6** — Commit any final fixes:
  ```bash
  git add -A
  git commit -m "fix(docs): verification pass — fix any broken links or build warnings"
  ```
  Omit if nothing needed fixing.

---

### Task 25: Push branch, open PR, and merge to main

**Estimated time:** 10–15 minutes

- [ ] **Step 25.1** — Final push:
  ```bash
  git push -u origin docs/user-guide-and-screenshots
  ```

- [ ] **Step 25.2** — Open pull request:
  ```bash
  gh pr create --title "docs: user guide and dashboard screenshots" --body "$(cat <<'EOF'
  ## Summary

  - New top-level User Guide section in the DocFX site (Developer Setup + Operator Guide tutorials + reference)
  - ~72 deterministic screenshots captured from Playwright+Reqnroll against a curated DashboardHost demo seeder
  - Weekly CI workflow auto-PRs screenshot drift
  - Existing `user-guide/` renamed to `reference/` with xref redirects

  ## What's included

  - 5 operator guide tutorials (Tasks 9–13)
  - 15 operator guide reference pages (Tasks 14–18)
  - 3 developer setup pages (Tasks 19–21)
  - `DocsDemoSeeder.cs` + CLI arg wiring in DashboardHost (Tasks 3–5)
  - `@docs-screenshot` Reqnroll step + `BeforeScenario` hook (Task 2)
  - `docs-screenshots.yml` CI workflow (Task 22)

  ## Test plan

  - [ ] `dotnet build ExperimentFramework.slnx` passes
  - [ ] `dotnet test --filter "Category!=DocsScreenshot"` passes
  - [ ] `docfx build docs/docfx.json` produces valid site; navigate the new tree manually in `docs/_site/`
  - [ ] Manually dispatch `docs-screenshots.yml` workflow and confirm it completes without drift on a clean checkout
  - [ ] All ~72 PNGs committed and visible in `docs/images/screenshots/`

  ## Risks acknowledged

  - Playwright font rendering on Linux CI may produce minor pixel diffs vs local Windows; CI-generated screenshots are canonical.
  - Seeder type names (`IExperimentRepository`, `ExperimentDefinition`, etc.) were resolved against actual project APIs during Task 3–4 implementation.

  🤖 Generated with [Claude Code](https://claude.com/claude-code)
  EOF
  )"
  ```

- [ ] **Step 25.3** — Enable auto-merge with squash:
  ```bash
  gh pr merge --squash --auto
  ```

---

## Appendix: Screenshot Filename Index

| Area | Filename | Task |
|------|----------|------|
| experiments | `first-exp-dashboard-home.png` | T9 |
| experiments | `first-exp-create-form.png` | T9 |
| experiments | `first-exp-create-arms.png` | T9 |
| experiments | `first-exp-create-submit.png` | T9 |
| experiments | `first-exp-experiments-list-with-new.png` | T9 |
| experiments | `first-exp-toggle-running.png` | T9 |
| rollout | `rollout-page-initial.png` | T10 |
| rollout | `rollout-stage-10pct.png` | T10 |
| rollout | `rollout-stage-50pct.png` | T10 |
| rollout | `rollout-stage-100pct.png` | T10 |
| rollout | `rollout-audit-trail.png` | T10 |
| governance | `approval-lifecycle-pending.png` | T11 |
| governance | `approval-approvals-list.png` | T11 |
| governance | `approval-approve-dialog.png` | T11 |
| governance | `approval-approved-status.png` | T11 |
| governance | `approval-promote-action.png` | T11 |
| governance | `approval-versions-snapshot.png` | T11 |
| analytics | `analytics-dashboard-overview.png` | T12 |
| analytics | `analytics-winner-callout.png` | T12 |
| analytics | `analytics-hypothesis-setup.png` | T12 |
| analytics | `analytics-hypothesis-result.png` | T12 |
| analytics | `analytics-export-dialog.png` | T12 |
| governance | `governance-policies-list.png` | T13 |
| governance | `governance-audit-violations.png` | T13 |
| governance | `governance-resolve-violation-dialog.png` | T13 |
| governance | `governance-lifecycle-transitions.png` | T13 |
| governance | `governance-day-complete.png` | T13 |
| experiments | `ref-home-overview.png` | T14 |
| experiments | `ref-home-active-cards.png` | T14 |
| experiments | `ref-home-stats-row.png` | T14 |
| experiments | `ref-experiments-list.png` | T14 |
| experiments | `ref-experiments-expanded.png` | T14 |
| experiments | `ref-experiments-filter.png` | T14 |
| experiments | `ref-create-form-empty.png` | T14 |
| experiments | `ref-create-form-filled.png` | T14 |
| experiments | `ref-create-confirmation.png` | T14 |
| analytics | `ref-analytics-overview.png` | T15 |
| analytics | `ref-analytics-arm-detail.png` | T15 |
| analytics | `ref-analytics-winner-badge.png` | T15 |
| analytics | `ref-hypothesis-form.png` | T15 |
| analytics | `ref-hypothesis-result-accept.png` | T15 |
| analytics | `ref-hypothesis-result-reject.png` | T15 |
| targeting | `ref-targeting-overview.png` | T16 |
| targeting | `ref-targeting-rule-form.png` | T16 |
| targeting | `ref-targeting-saved.png` | T16 |
| rollout | `ref-rollout-overview.png` | T16 |
| rollout | `ref-rollout-stage-history.png` | T16 |
| rollout | `ref-rollout-advance-dialog.png` | T16 |
| plugins | `ref-plugins-overview.png` | T17 |
| plugins | `ref-plugins-detail.png` | T17 |
| plugins | `ref-plugins-enable-dialog.png` | T17 |
| configuration | `ref-configuration-overview.png` | T17 |
| configuration | `ref-configuration-edit-form.png` | T17 |
| configuration | `ref-configuration-saved.png` | T17 |
| configuration | `ref-dsl-editor-overview.png` | T17 |
| configuration | `ref-dsl-editor-validation-ok.png` | T17 |
| configuration | `ref-dsl-editor-validation-error.png` | T17 |
| governance | `ref-lifecycle-overview.png` | T18 |
| governance | `ref-lifecycle-transition-dialog.png` | T18 |
| governance | `ref-lifecycle-history.png` | T18 |
| governance | `ref-approvals-queue.png` | T18 |
| governance | `ref-approvals-detail.png` | T18 |
| governance | `ref-approvals-approve-action.png` | T18 |
| governance | `ref-audit-full-log.png` | T18 |
| governance | `ref-audit-filter-type.png` | T18 |
| governance | `ref-audit-event-detail.png` | T18 |
| governance | `ref-policies-list.png` | T18 |
| governance | `ref-policies-create-form.png` | T18 |
| governance | `ref-policies-violation-badge.png` | T18 |
| governance | `ref-versions-list.png` | T18 |
| governance | `ref-versions-diff.png` | T18 |
| governance | `ref-versions-restore-dialog.png` | T18 |
| developer-setup | `dashboard-first-load.png` | T19 |
| developer-setup | `dashboard-with-experiment.png` | T19 |
| developer-setup | `sample-host-experiments.png` | T20 |

**Total: 72 screenshots across 8 area folders.**
