# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

**Build the whole solution:**
```bash
dotnet build ExperimentFramework.slnx
```

**Run all tests (excluding e2e/screenshot tests):**
```bash
dotnet test ExperimentFramework.slnx --filter "Category!=docs-screenshot"
```
CI uses the MSBuild flag `-p:ExcludeE2ETests=true` for the coverage run; the filter above is the local equivalent.

**Run a single test by name:**
```bash
dotnet test ExperimentFramework.slnx --filter "FullyQualifiedName~SomeTestClassName"
```

**Run e2e screenshot tests (requires a running DashboardHost):**

Terminal 1 — start the host with seeded demo data:
```bash
dotnet run --project samples/ExperimentFramework.DashboardHost -- \
  --seed=docs --freeze-date 2026-04-01T12:00:00Z --Urls=http://localhost:5195
```

Terminal 2 — run the screenshot suite once the host is ready:
```bash
E2E__BaseUrl=http://localhost:5195 dotnet test tests/ExperimentFramework.E2E.Tests \
  --filter "Category=docs-screenshot"
```

**Install Playwright browsers (required once after building E2E tests):**
```bash
pwsh tests/ExperimentFramework.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
```

**Build the DocFX site:**
```bash
dotnet tool install -g docfx   # once
docfx build docs/docfx.json
# output at docs/_site/
```

**Run the sample host with demo data (interactive dev):**
```bash
dotnet run --project samples/ExperimentFramework.DashboardHost -- \
  --seed=docs --Urls=http://localhost:5195
# dashboard at http://localhost:5195/dashboard
```

## Architecture

### Package layout

The `src/` folder is a modular NuGet family. Groups by purpose:

| Group | Projects |
|-------|----------|
| **Core** | `ExperimentFramework` (proxy/dispatch engine), `ExperimentFramework.Generators` (Roslyn source generator, targets `netstandard2.0`) |
| **Dashboard** | `ExperimentFramework.Dashboard` (middleware + `AddExperimentDashboard`), `ExperimentFramework.Dashboard.UI` (Blazor Razor components, RCL), `ExperimentFramework.Dashboard.Api` (REST endpoints), `ExperimentFramework.Dashboard.Abstractions` (interfaces: `IAnalyticsProvider`, `IDashboardDataProvider`, etc.), `ExperimentFramework.Dashboard.Analytics` |
| **Governance** | `ExperimentFramework.Governance` (lifecycle, approvals, policy-as-code), `ExperimentFramework.Governance.Persistence` (`IGovernancePersistenceBackplane`, in-memory impl), `ExperimentFramework.Governance.Persistence.Redis`, `ExperimentFramework.Governance.Persistence.Sql` |
| **Data plane** | `ExperimentFramework.DataPlane.Abstractions` (`IDataBackplane`), `ExperimentFramework.DataPlane`, `ExperimentFramework.DataPlane.SqlServer`, `ExperimentFramework.DataPlane.Kafka`, `ExperimentFramework.DataPlane.AzureServiceBus` |
| **Selection modes** | `ExperimentFramework.FeatureManagement`, `ExperimentFramework.OpenFeature`, `ExperimentFramework.StickyRouting` |
| **Algorithms** | `ExperimentFramework.Bandit`, `ExperimentFramework.Rollout`, `ExperimentFramework.Targeting`, `ExperimentFramework.AutoStop` |
| **Science** | `ExperimentFramework.Science` (t-test, chi-square, Mann-Whitney, ANOVA, effect size, power analysis), `ExperimentFramework.Data` (outcome recording) |
| **Infrastructure** | `ExperimentFramework.Resilience` (Polly circuit breaker/timeout), `ExperimentFramework.Distributed` + `.Redis`, `ExperimentFramework.Diagnostics` (InMemory/Logger/OTel event sinks), `ExperimentFramework.Metrics.Exporters` (Prometheus/OTel), `ExperimentFramework.Audit`, `ExperimentFramework.Admin` |
| **Extensibility** | `ExperimentFramework.Plugins` (dynamic assembly loading, hot reload), `ExperimentFramework.Plugins.Generators`, `ExperimentFramework.Simulation` (shadow mode), `ExperimentFramework.Configuration` (YAML/JSON DSL), `ExperimentFramework.Testing` |

The solution file is `ExperimentFramework.slnx`. The `benchmarks/` and `samples/` projects are included in the solution but are not part of the NuGet package graph.

### How experiments are registered

Experiments are **defined at startup, not at runtime**. There is no API for creating experiments after `app.Build()`. The typical pattern:

```csharp
var config = ExperimentFrameworkBuilder.Create()
    .Define<IMyService>(e => e
        .UsingFeatureFlag("my-flag")
        .AddDefaultTrial<ControlImpl>("control")
        .AddTrial<VariantImpl>("variant")
        .OnErrorFallbackToControl())
    .UseDispatchProxy();           // or no call = source-generated proxy

builder.Services.AddExperimentFramework(config);
```

The fluent DSL has synonym methods: `Trial<T>` = `Define<T>`, `AddControl` = `AddDefaultTrial`, `AddCondition` = `AddVariant`. Use whichever fits your mental model.

`IMutableExperimentRegistry` (accessible at runtime) can only toggle active/rollout percentage on **already-registered** experiments. It cannot create new ones. This is a common foot-gun.

### Dashboard data plane

The dashboard queries `IAnalyticsProvider` (defined in `Dashboard.Abstractions`). **Critical wiring notes:**

- `IAnalyticsProvider` is **not registered by default**. Pass it via `DashboardOptions.AnalyticsProvider` in `AddExperimentDashboard(options => ...)`.
- `IAnalyticsProvider` is **not automatically fed** by `IDataBackplane`. The two interfaces are independent; callers must bridge them if needed.
- In `--seed=docs` mode, `DemoAnalyticsProvider` (`samples/ExperimentFramework.DashboardHost/Demo/DemoAnalyticsProvider.cs`) is wired in directly via `DashboardOptions`.

### Governance persistence

`IGovernancePersistenceBackplane` (in `ExperimentFramework.Governance.Persistence`) exposes:
`SaveExperimentStateAsync`, `AppendStateTransitionAsync`, `AppendApprovalRecordAsync`, `AppendConfigurationVersionAsync`, `AppendPolicyEvaluationAsync`.

Enable with `services.AddInMemoryGovernancePersistence()` (or Redis/SQL variants). Not enabled by default — the dashboard governance UI is non-functional without it.

### Sample hosts

| Sample | Purpose |
|--------|---------|
| `samples/ExperimentFramework.DashboardHost/` | Canonical standalone dashboard host. Supports `--seed=docs` (5 experiments across lifecycle states, real analytics, governance records, 3 policies) and `--freeze-date <ISO>` for deterministic screenshots. |
| `samples/ExperimentFramework.SampleWebApp/` | Minimal ASP.NET web app integration |
| `samples/ExperimentFramework.ComprehensiveSample/` | Showcase of most features |
| `samples/ExperimentFramework.SimulationSample/` | Shadow mode / simulation |
| `samples/ExperimentFramework.AspireDemo/` | .NET Aspire multi-project demo |
| Other samples | BanditOptimizer, ResilienceDemo, FeatureFlagDemo, ScientificDemo, ScientificSample, PluginHostSample |

### Blazor Web App hosting quirks

These are production foot-guns in the `DashboardHost` setup:

- `DashboardHost.csproj` sets `<RequiresAspNetWebAssets>true</RequiresAspNetWebAssets>` — without this, `/_framework/blazor.web.js` returns 404 (the host has no `.razor` files of its own; they live in the `Dashboard.UI` RCL).
- Use `MapRazorComponents<App>().AddInteractiveServerRenderMode()`. Do **not** call `MapBlazorHub()` — that is legacy Blazor Server and will conflict.
- SSR prerender requires data loading in `OnInitializedAsync`, not `OnAfterRenderAsync`.
- Dashboard REST API is mounted at `/dashboard/api/...`; `ExperimentApiClient` base address must include the `/dashboard/` prefix or relative paths will 404.

## Docs and e2e screenshots

The DocFX site lives under `docs/`; output is `docs/_site/`.

**Docs structure:**
- `docs/user-guide/` — operator-guide (tutorials + reference) and developer-setup walkthroughs
- `docs/reference/` — conceptual reference docs (bandit, circuit-breaker, data-backplane, etc.)
- `docs/api/` — DocFX-generated API reference
- `docs/superpowers/specs/` — design documents for major features (not published)

**Screenshot pipeline:**
- PNGs live in `docs/images/screenshots/<area>/` and are committed to the repo
- Reqnroll step `When I capture screenshot "<name>"` (in `DocsScreenshotStepDefinitions.cs`) writes to `docs/images/screenshots/{area}/{name}.png`
- `[BeforeScenario("docs-screenshot", Order = 5)]` hook (in `DocsScreenshotHooks.cs`) sets 1280×800 viewport and injects determinism CSS (no animations, no transitions)
- Feature files tag scenarios `@docs-screenshot @screenshot-area:<area>`; the area tag value becomes the subdirectory
- Design rationale: `docs/superpowers/specs/2026-04-22-user-guide-and-screenshots-design.md`

## CI / GitHub Actions

Workflows live in `.github/workflows/`:

| Workflow | Trigger | What it does |
|----------|---------|--------------|
| `ci.yml` | push/PR to `main` | Build + test (no E2E) with coverage; on PR adds Codecov report and sticky coverage comment. On push to `main`: full release job — GitVersion, pack all NuGet packages, push to NuGet.org and GitHub Packages, create GitHub Release with generated release notes. |
| `docs.yml` | push to `main` | Builds DocFX site and deploys to GitHub Pages. |
| `docs-screenshots.yml` | weekly Monday 03:00 UTC + `workflow_dispatch` | Starts DashboardHost with `--seed=docs`, runs `Category=docs-screenshot` suite, optionally compresses PNGs with oxipng, opens a drift PR if any screenshot changed. |
| `codeql-analysis.yml` | push/PR to `main` + schedule | CodeQL security scanning. |
| `dependency-review.yml` | PR | Dependency review. |
| `update-packages.yml` | schedule | Automated dependency updates. |
| `stale.yml` | schedule | Marks stale issues and PRs. |
| `labeler.yml` | PR | Auto-labels PRs. |

## Conventions worth knowing

- **Multi-targeting:** Most `src/` projects target `net8.0;net9.0;net10.0`. `ExperimentFramework.Generators` targets `netstandard2.0` (Roslyn analyzers must).
- **Solution format:** `ExperimentFramework.slnx` is the new XML-format solution file, not `.sln`. Pass it explicitly to `dotnet build/test/pack`.
- **E2E test framework:** Playwright + Reqnroll (Gherkin BDD) with xUnit runner. A Gherkin tag `@docs-screenshot` maps to xUnit Trait `Category=docs-screenshot` (lowercase, hyphenated). The filter string is `Category=docs-screenshot`, not `DocsScreenshot` or `Category=DocsScreenshot`.
- **Versioning:** GitVersion drives package versions from git history. The `release` CI job calculates and injects version numbers; local builds use the placeholder `1.0.0` in the csproj.
- **Commit style:** Conventional commits (`feat(scope): ...`, `fix:`, `docs:`, `chore(deps):`) with a `Co-Authored-By:` footer. See `git log` for examples.
- **`ExcludeE2ETests` MSBuild property:** The E2E project references Playwright and requires a live host. CI sets `-p:ExcludeE2ETests=true` in the main coverage run to skip it; the `e2e-tests` job in `ci.yml` handles it separately (marked `continue-on-error`).
