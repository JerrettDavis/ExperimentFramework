---
uid: user-guide/developer-setup/running-the-samples
---
# Running the Samples

This framework ships with 17+ runnable samples ranging from short console programs to a multi-service .NET Aspire orchestration. This page is your one-stop map: what each project does, which one to open first, and how to launch it in Rider.

## Pick a starting point

| I want to... | Open this project | Launch it with |
|---|---|---|
| See the framework end-to-end in minutes | `ExperimentFramework.ComprehensiveSample` | `dotnet run` |
| Learn just the basics | `ExperimentFramework.SampleConsole` | `dotnet run` |
| See the dashboard UI with real data | `ExperimentFramework.DashboardHost` | see [Dashboard Host](#dashboard-host) |
| Try a multi-service Aspire demo | `AspireDemo.AppHost` | Run the AppHost profile in Rider |
| See a real web API integration | `ExperimentFramework.SampleWebApp` | `dotnet run` |
| Explore governance and approvals | `ExperimentFramework.GovernanceSample` | `dotnet run` |
| See bandit / adaptive algorithms | `ExperimentFramework.BanditOptimizer` | `dotnet run` |
| See resilience and fallback patterns | `ExperimentFramework.ResilienceDemo` | `dotnet run` |
| Try feature flags and multi-variant | `ExperimentFramework.FeatureFlagDemo` | `dotnet run` |
| Run statistical / auto-stop logic | `ExperimentFramework.ScientificDemo` | `dotnet run` |
| Run a full statistical walkthrough | `ExperimentFramework.ScientificSample` | `dotnet run` |
| See shadow / simulation mode | `ExperimentFramework.SimulationSample` | `dotnet run` |
| See the data plane with OTel tracing | `ExperimentFramework.OpenTelemetryDataPlaneSample` | `dotnet run` |
| See the raw data plane | `ExperimentFramework.DataPlaneSample` | `dotnet run` |
| See schema stamping / config versioning | `ExperimentFramework.SchemaStampingSample` | `dotnet run` |
| See plugin loading / hot reload | `ExperimentFramework.PluginHostSample` | see [Plugin Host Sample](#plugin-host-sample) |

## Sample categories

### Core learning

| Project | Type | Purpose |
|---|---|---|
| `ExperimentFramework.ComprehensiveSample` | Console | All 5 error policies, all 4 selection modes, all return types, decorators, OTel, variant flags, source generator |
| `ExperimentFramework.SampleConsole` | Console | Boolean feature flag and configuration-based routing; built-in decorators; minimal complexity |

Both are console apps: open the project in Rider, set it as startup, and press Run. No extra configuration needed.

### Dashboard host

| Project | Type | Purpose |
|---|---|---|
| `ExperimentFramework.DashboardHost` | Web (Blazor) | Standalone host for the ExperimentFramework dashboard; supports seeded demo data and frozen timestamps |

This is the canonical way to browse the **ExperimentFramework** dashboard UI. It is **not** an Aspire project — see [Native Aspire dashboard vs ExperimentFramework dashboard](#native-aspire-dashboard-vs-experimentframework-dashboard) below.

**Launch commands (from the repo root):**

```bash
# Basic run — seeded demo data, live clock
dotnet run --project samples/ExperimentFramework.DashboardHost -- \
  --seed=docs --Urls=http://localhost:5195
```

```bash
# Frozen timestamp — used for reproducible screenshots
dotnet run --project samples/ExperimentFramework.DashboardHost -- \
  --seed=docs --freeze-date 2026-04-01T12:00:00Z --Urls=http://localhost:5195
```

Browse to `http://localhost:5195/dashboard` once the host is running.

The `--seed=docs` flag loads 5 experiments across different lifecycle states plus real analytics and governance records. Without `--seed=docs` the dashboard starts with no data.

Full background on the DashboardHost is in [docs/user-guide/developer-setup/sample-host.md](sample-host.md).

### Web integration

| Project | Type | Purpose |
|---|---|---|
| `ExperimentFramework.SampleWebApp` | Web API | Sticky routing with deterministic per-session A/B assignment; feature flag checkout flow |
| `ExperimentFramework.GovernanceSample` | Console / hosted | Governance lifecycle, approval gates, configuration versioning, policy-as-code, SQL persistence |

**SampleWebApp** — starts on `http://localhost:5242` (http profile). After `dotnet run` try:

```bash
curl http://localhost:5242/api/recommendations
curl http://localhost:5242/api/recommendations/algorithm
curl http://localhost:5242/api/checkout/flow
```

**GovernanceSample** — console output walks through all governance steps; no browser needed.

### Aspire multi-service demo

| Project | Type | Purpose |
|---|---|---|
| `AspireDemo.AppHost` | Aspire orchestrator | Orchestrates ApiService, Web frontend, Blog service, and four Blog plugins; shows framework across multiple services with auth |

See [samples/ExperimentFramework.AspireDemo/README.md](../../samples/ExperimentFramework.AspireDemo/README.md) for full details, including service URLs, seeded users, and E2E test instructions.

### Specialized samples

| Project | Type | Purpose |
|---|---|---|
| `ExperimentFramework.ScientificSample` | Console | Statistical analysis end-to-end: power analysis, hypothesis definition, data collection, t-test/chi-square/ANOVA, publication-ready reports |
| `ExperimentFramework.ScientificDemo` | Console | Auto-stopping rules: p-value threshold, minimum sample size, automatic conclusion |
| `ExperimentFramework.BanditOptimizer` | Console | Multi-armed bandit: epsilon-greedy, Thompson sampling, UCB1 |
| `ExperimentFramework.ResilienceDemo` | Console | Error fallback policies: `RedirectAndReplayDefault`, `RedirectAndReplayAny`, `RedirectAndReplayOrdered` |
| `ExperimentFramework.FeatureFlagDemo` | Console | Microsoft.FeatureManagement integration: boolean flags, variant flags, percentage rollouts |
| `ExperimentFramework.SimulationSample` | Console | Shadow mode: output-based (read) and action-based (write) simulations with isolation |
| `ExperimentFramework.DataPlaneSample` | Console | `IDataBackplane` wiring, event capture |
| `ExperimentFramework.OpenTelemetryDataPlaneSample` | Console | Same as DataPlaneSample but with OpenTelemetry activity listener |
| `ExperimentFramework.SchemaStampingSample` | Console | Configuration schema hashing and versioning via YAML DSL |
| `ExperimentFramework.PluginHostSample` | Console | Dynamic plugin loading; see [Plugin Host Sample](#plugin-host-sample) |

## Native Aspire dashboard vs ExperimentFramework dashboard

These are two separate UIs and a common source of confusion.

### Native Aspire dashboard

- Launched **automatically** when you run `AspireDemo.AppHost`
- Built into .NET Aspire — it ships with the Aspire workload, not with this framework
- Shows Aspire-managed resources: service health, structured logs, distributed traces, environment variables, and endpoints for every service in the AppHost
- URL: the AppHost prints it to the console when it starts (typically `http://localhost:15110` for the http profile or `https://localhost:17014` for https — verify from the AppHost's `launchSettings.json`)
- You use this dashboard to monitor the Aspire services, not to manage experiments

### ExperimentFramework dashboard

- A Blazor UI embedded in your application via the `ExperimentFramework.Dashboard` and `ExperimentFramework.Dashboard.UI` NuGet packages
- Mounted at a route prefix (e.g. `/dashboard`) inside your own app
- Shows experiments, rollout percentages, governance state, analytics, and approval workflows — all framework-specific data
- In the samples, the **DashboardHost** project is the standalone host for this UI
- In the AspireDemo, it is embedded in the **AspireDemo.Web** frontend (accessible at `/dashboard` on that service's URL)
- See [Embed the Dashboard](embed.md) for integration instructions

**Rule of thumb:** If you want to manage experiments and see analytics, open the ExperimentFramework dashboard. If you want to check whether a service is up or trace a request across services, use the Aspire dashboard.

## Plugin Host Sample

The PluginHostSample demonstrates dynamic plugin loading. It expects the `SamplePlugin` DLL to already exist on disk before it runs.

**Build order:**

```bash
# Step 1: build the plugin first
dotnet build samples/ExperimentFramework.SamplePlugin

# Step 2: then run the host
dotnet run --project samples/ExperimentFramework.PluginHostSample
```

If you skip step 1, the host exits immediately with "Plugin not found — please build SamplePlugin first".

See [samples/ExperimentFramework.PluginHostSample/README.md](../../samples/ExperimentFramework.PluginHostSample/README.md) for details, and [docs/reference/plugins.md](../../docs/reference/plugins.md) for the plugin system reference.

## Rider tips

### Setting a startup project

Right-click the project in the Solution Explorer and choose **Set as Startup Project**. For console samples this is all you need.

### Passing launch arguments to DashboardHost

In Rider, open **Run > Edit Configurations**, select the DashboardHost run configuration, and add the program arguments in the **Program arguments** field:

```
--seed=docs --freeze-date 2026-04-01T12:00:00Z --Urls=http://localhost:5195
```

Alternatively, run from the terminal using the `dotnet run` commands shown above.

### Launching the Aspire AppHost

1. In the Solution Explorer, expand `samples/ExperimentFramework.AspireDemo/` and open `AspireDemo.AppHost`.
2. Right-click `AspireDemo.AppHost` and choose **Set as Startup Project**.
3. In **Run > Edit Configurations**, confirm the profile is `https` or `http` (matching your preference).
4. Press Run. Rider will build all dependent projects and the Aspire dashboard URL will appear in the run output.

The Aspire workload must be installed: `dotnet workload install aspire`.

## Troubleshooting

### "I ran the sample and nothing appeared in the browser"

- **Console samples** produce output in the terminal, not the browser. Run them in a terminal or Rider's built-in console.
- **DashboardHost** requires the URL `http://localhost:5195/dashboard` — the root `/` does not redirect by default.
- **SampleWebApp** does not auto-open a browser (`launchBrowser: false`). Hit the API endpoints with curl or a REST client.

### HTTPS certificate errors (localhost)

If you see certificate warnings or `ERR_CERT_AUTHORITY_INVALID`, run:

```bash
dotnet dev-certs https --trust
```

Then restart the affected app.

### Port conflicts

If a port is already in use, either stop the conflicting process or override the URL:

```bash
dotnet run --project samples/ExperimentFramework.DashboardHost -- --seed=docs --Urls=http://localhost:5999
```

### Playwright browser not installed (E2E tests)

The E2E test suites use Playwright. After building the test project for the first time, install the required browser:

```bash
# For the main docs E2E tests
pwsh tests/ExperimentFramework.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install chromium

# For the AspireDemo E2E tests
pwsh samples/ExperimentFramework.AspireDemo/AspireDemo.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium
```

### Aspire workload not found

If running `AspireDemo.AppHost` fails with a workload error:

```bash
dotnet workload install aspire
```

### DashboardHost shows no data

If the dashboard is blank, make sure you passed `--seed=docs`. Without it the host starts with zero experiments registered.
