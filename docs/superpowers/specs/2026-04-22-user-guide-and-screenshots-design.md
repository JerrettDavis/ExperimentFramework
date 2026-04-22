# User Guide & Dashboard Screenshots — Design

**Date:** 2026-04-22
**Status:** Approved for planning
**Scope:** New top-level User Guide in the DocFX site, dashboard screenshots captured from e2e tests with a curated demo seeder, and weekly CI regeneration.

## Goal

Ship a first-party User Guide in the DocFX site that onboards non-.NET operators to the dashboard and gives developers a fast path to stand it up. Every screen must *teach something*, which requires a seeded demo environment and deterministic screenshot capture wired into the existing Playwright + Reqnroll e2e suite.

## Non-Goals

- Rewriting existing reference documentation (it stays; we rename `docs/user-guide/` to `docs/reference/` and set up xref redirects).
- Adding new dashboard features.
- Dark-mode screenshots.
- Annotated overlays on screenshots (we use numbered lists beneath images instead).
- Real-data snapshots from production tenants.

## Information Architecture

New top-level TOC layout in `docs/toc.yml`:

```
Getting Started           (promoted from existing user-guide/getting-started.md)
User Guide                (new top-level section)
  Developer Setup
    - Embed the dashboard
    - Run the sample host
    - Production checklist
  Operator Guide
    Tutorials
      - Your first experiment
      - Progressive rollout
      - Approval & promotion
      - Analyzing results
      - Governance day-in-the-life
    Reference
      - Home
      - Experiments
      - Create
      - Analytics
      - Targeting
      - Rollout
      - Hypothesis Testing
      - Plugins
      - Configuration
      - DSL Editor
      - Governance / Lifecycle
      - Governance / Approvals
      - Governance / Audit
      - Governance / Policies
      - Governance / Versions
Reference                 (existing user-guide/*.md renamed to reference/*.md)
API                       (unchanged; DocFX-generated)
```

**Rename rationale:** the current `docs/user-guide/` contents (Bandit, Circuit Breaker, Data Backplane, etc.) are conceptual reference, not user-facing walkthroughs. Keeping "user-guide" on those pages would confuse readers arriving at the new User Guide.

**Redirects:** DocFX's xref map keeps existing deep-links alive. A `docs/redirects.yml` (or equivalent xref entries) maps `user-guide/*` → `reference/*`.

**Cross-linking rule:** every tutorial step links to the matching reference page; every reference page links back to tutorials that introduce it.

## Operator Guide — Content

### Tutorials (5 pages)

Each tutorial opens with "You'll learn / You'll need / ~X minutes" and closes with a "What next" block.

| # | Tutorial | Story arc | Pages touched | Screenshots |
|---|---|---|---|---|
| 1 | Your first experiment | Log in → create 2-arm experiment → toggle on → see it in list | Home, Create, Experiments | 6 |
| 2 | Progressive rollout | Define 10/50/100% stages → advance → audit confirms | Rollout, Experiments, Governance/Audit | 5 |
| 3 | Approval & promotion | Submit for approval → approver view → approve → promote → version snapshot | Governance/Lifecycle, Approvals, Versions | 6 |
| 4 | Analyzing results | Open Analytics → read winner callout → run hypothesis test → export | Analytics, Hypothesis | 5 |
| 5 | Governance day-in-the-life | Review policies → inspect audit → resolve violation → lifecycle transitions | Policies, Audit, Lifecycle | 5 |

**Total tutorial screenshots: ~27.**

### Reference (15 pages)

Every reference page follows this template:

```
# [Page Name]

> One-sentence purpose.

[Full-page screenshot in default seeded state]

## What you see
Numbered list of regions/controls (no image overlay; numbers in text align with numbered callouts in a simple numbered list).

## Common tasks
Task 1 → action screenshot
Task 2 → action screenshot

## Fields & controls
Table: control → what it does → when to use.

## Troubleshooting
Common issues with fixes.

## Related
- Tutorial links
- Reference cross-links
```

**Screenshots per reference page:** 1 baseline + 1–3 task = ~2–4 per page × 15 pages ≈ **45 screenshots**.

**Total screenshot budget: ~72 PNGs**, committed under `docs/images/screenshots/<area>/`.

### Annotations

No image overlays. Every screenshot followed by a numbered list when callouts are needed. Lists stay in sync with UI text changes more reliably than burned-in overlays.

## Developer Setup — Content

### 1. Embed the dashboard (`developer-setup/embed.md`)

- `dotnet add package` lines for `ExperimentFramework.Dashboard`, `.Dashboard.UI`, `.Dashboard.Api`
- Minimal `Program.cs` diff: `AddExperimentFramework()` + `MapExperimentDashboard("/dashboard")`
- Auth: how to plug into existing cookie/OIDC; `DashboardOptions.RequireAuthorization`
- Tenancy snippet with link to reference
- Verify step: browse `/dashboard`

**Screenshots (2):** `dashboard-first-load.png` (empty state), `dashboard-with-experiment.png` (after first create).

### 2. Run the sample host (`developer-setup/sample-host.md`)

- Clone → `cd samples/ExperimentFramework.DashboardHost` → `dotnet run -- --seed=docs`
- What the seed produces (paragraph summary)
- Default credentials
- Five recommended pages to explore first (with reference links)

**Screenshots (1):** `sample-host-experiments.png` (Experiments with seeded data).

### 3. Production checklist (`developer-setup/production-checklist.md`)

Plain-text checklist. No code, no screenshots. Covers: auth, authorization, durable backplane, governance approvers, telemetry, TLS/forwarded headers, kill switch, backup/restore.

**Developer Setup total:** 3 pages, 3 screenshots.

## Screenshot Infrastructure

**Test framework addition** — a new Reqnroll step definition:

```csharp
[When(@"I capture screenshot ""([^""]+)""")]
public async Task WhenICaptureScreenshot(string name)
{
    // path = docs/images/screenshots/<feature-area>/<name>.png
    // where feature-area is derived from the current feature file's folder or tag
    await _dashboardDriver.TakeScreenshotAsync(ResolveScreenshotPath(name));
}
```

**Scenario tagging:** `@docs-screenshot` + Reqnroll `[Category("DocsScreenshot")]` on generated tests so CI can filter.

**Determinism controls** (applied in a `BeforeScenario` hook tagged `@docs-screenshot`):
- Viewport: 1280×800
- Browser: Chromium (matches existing Playwright config)
- Theme: default (light)
- Inject CSS: `*, *::before, *::after { animation: none !important; transition: none !important; caret-color: transparent !important; }`
- Clock frozen to `2026-04-01T12:00:00Z` via the DashboardHost's `--freeze-date` flag (see Seeder)

**Output path:** `docs/images/screenshots/<area>/<name>.png` where `<area>` groups by dashboard module (experiments, analytics, governance, rollout, etc.). Markdown references use relative paths.

**Image storage:** PNGs committed to git. Loss of a PNG breaks the docs site; this is intentional so reviewers see screenshot changes in PRs.

## Docs-Demo Seeder

**Location:** `samples/ExperimentFramework.DashboardHost/DocsDemoSeeder.cs` (new file) + wire-up in `Program.cs`.

**Activation:** CLI arg `--seed=docs` *or* env var `EXPERIMENT_DEMO_SEED=docs`. A `--reset` flag wipes existing state before seeding. Bare `--seed=docs` is idempotent (no-op if seed already applied).

**Time freezing:** `--freeze-date <iso8601>` flag in the host controls the clock source injected into the experiment framework (via a `ISystemClock` stub), so seeded timestamps and "now" are deterministic.

**Seeded scenario (curated):**

| Experiment | State | Arms | Notable detail |
|---|---|---|---|
| `checkout-button-v2` | Running, 50% rollout | 2 | Stat-sig winner after 14 days |
| `search-ranker-ml` | Running, 10% rollout | 3 | Inconclusive so far |
| `homepage-layout-fall2026` | Draft, pending approval | 2 | Waiting on approver |
| `pricing-page-copy` | Paused | 2 | Circuit breaker tripped yesterday, policy violation recorded |
| `legacy-api-cutover` | Promoted, archived 30 days ago | 2 | Historical record |

Additional seeded state:
- Analytics: ~10k samples per running experiment, plausible distributions
- Pending approval: 1 (on `homepage-layout-fall2026`)
- Audit events: 20 recent, mixed types
- Governance policies: 3 (`require-two-approvers`, `no-friday-deploys`, `min-sample-size-1000`)
- Policy violation: 1 (`pricing-page-copy` tripped `min-sample-size-1000`)
- Version snapshots: 2 per non-draft experiment

## CI Automation

**Workflow:** `.github/workflows/docs-screenshots.yml`

**Triggers:**
- `schedule: cron '0 3 * * 1'` (weekly Monday 03:00 UTC)
- `workflow_dispatch` (manual)

**Steps:**
1. Checkout
2. Setup .NET 10
3. Install Playwright browsers (Chromium only, with deps)
4. Build solution
5. Launch DashboardHost in background: `dotnet run --project samples/ExperimentFramework.DashboardHost -- --seed=docs --reset --freeze-date 2026-04-01T12:00:00Z &`
6. Wait for readiness (curl loop on `/health` with timeout)
7. Run screenshot suite: `dotnet test tests/ExperimentFramework.E2E.Tests --filter Category=DocsScreenshot`
8. Detect drift: `git diff --quiet docs/images/screenshots/`
9. If drift, `peter-evans/create-pull-request@v6` opens a PR titled `docs: regenerate screenshots (YYYY-MM-DD)` with label `docs-screenshots` and assignee the repo owner.

**Interaction with existing `docs.yml`:** no interaction. The deploy workflow continues to run DocFX against whatever PNGs are committed. Screenshot freshness is a separate, slower loop.

## Execution Tracks

This spec decomposes into three tracks that can proceed partially in parallel:

1. **Foundation** (sequential prerequisite): docs-demo seeder + screenshot step/hook + `@docs-screenshot` tag plumbing + rename `user-guide/` → `reference/` with xref redirects + `docs/toc.yml` skeleton for the new User Guide tree.
2. **Content** (fan-out after foundation): 5 tutorial pages, 15 reference pages, 3 developer-setup pages. Each page's screenshots are generated by adding `@docs-screenshot` scenarios against the seeded host.
3. **CI automation** (can start after Foundation): `docs-screenshots.yml` workflow, tested manually via `workflow_dispatch` at least once before landing.

## Risks & Open Questions

- **Playwright flakiness on CI:** Chromium in headless mode on Linux runners occasionally renders fonts slightly differently than local Windows. Mitigation: pin to a specific Chromium version via Playwright's bundled browser and accept that screenshots are CI-canonical (local regens produce minor diffs that the author discards).
- **DocFX xref redirects:** need to verify the exact mechanism DocFX supports for path renames; fallback is a short client-side redirect shim on each old URL.
- **Seeder schema drift:** the seeder depends on stable public APIs for `ExperimentFramework.Configuration`, `Governance`, `Analytics`. If any of these change signature, the seeder breaks and screenshot CI starts failing. Mitigation: the seeder lives in the sample project, which is already part of the solution build, so a breaking API change surfaces at compile time.
- **Screenshot file size:** 72 PNGs at 1280×800 ≈ 10–20 MB total. Acceptable in-repo; run `oxipng` or equivalent in the CI workflow before opening the drift PR.

## Deliverables Summary

- 1 spec document (this file)
- 1 implementation plan (follow-up)
- 23 new markdown pages (5 tutorials + 15 reference + 3 developer-setup)
- ~72 PNG screenshots committed under `docs/images/screenshots/`
- 1 new Reqnroll step + 1 new scenario hook + scenario tag
- 1 new seeder class in `ExperimentFramework.DashboardHost` + `Program.cs` wiring
- 1 new GitHub Actions workflow
- Rename of `docs/user-guide/*.md` → `docs/reference/*.md` with xref redirects
- `docs/toc.yml` updated for the new tree
