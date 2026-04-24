# ExperimentFramework.AspireDemo

A .NET Aspire multi-service demo that shows ExperimentFramework running across several cooperating services, with a realistic auth flow, a blog backend with plugin-loaded extensions, and Playwright-based end-to-end tests.

## What this demo is

The demo orchestrates five backend services and exposes the ExperimentFramework dashboard inside the Web frontend. It is the most realistic sample in the repository: multiple services share experiment state, governance is enforced at the API layer, and the blog backend loads its feature implementations as runtime plugins.

This is distinct from the standalone [DashboardHost](../ExperimentFramework.DashboardHost/) sample, which runs the dashboard in isolation with seeded demo data. See [Native Aspire dashboard vs ExperimentFramework dashboard](../../docs/user-guide/developer-setup/running-the-samples.md#native-aspire-dashboard-vs-experimentframework-dashboard) if you are unsure which to open.

## Project topology

```
AspireDemo.AppHost              ← Aspire orchestrator; start here
├── AspireDemo.ApiService       ← Backend API; experiment routing logic
├── AspireDemo.Web              ← Blazor frontend; hosts the EF dashboard at /dashboard
│   └── AspireDemo.Web.Tests    ← Unit tests for identity / auth wiring
└── AspireDemo.Blog             ← Blog service; loads feature implementations as plugins
    ├── AspireDemo.Blog.Contracts
    ├── AspireDemo.Blog.Plugins.Auth
    ├── AspireDemo.Blog.Plugins.Data
    ├── AspireDemo.Blog.Plugins.Editor
    └── AspireDemo.Blog.Plugins.Syndication
```

`AspireDemo.ServiceDefaults` provides shared Aspire service defaults (health checks, OTel, etc.) and is referenced by all services.

## How to launch

**Prerequisites:**
- .NET 10 SDK
- .NET Aspire workload: `dotnet workload install aspire`

**From the terminal (repo root):**

```bash
dotnet run --project samples/ExperimentFramework.AspireDemo/AspireDemo.AppHost
```

**From Rider:**

1. Open the solution file `ExperimentFramework.slnx`.
2. In the Solution Explorer expand `samples/ExperimentFramework.AspireDemo/`.
3. Right-click `AspireDemo.AppHost` and choose **Set as Startup Project**.
4. Select the `https` or `http` run profile and press Run.

The Aspire dashboard URL is printed to the run console when the AppHost starts.

## URLs

| What | URL (http profile) | URL (https profile) |
|---|---|---|
| Aspire dashboard | `http://localhost:15110` | `https://localhost:17014` |
| Web frontend | `http://localhost:5083` | `https://localhost:7201` |
| Blog service | `http://localhost:5120` | `https://localhost:7120` |
| ApiService | `http://localhost:5526` | `https://localhost:7306` |
| EF dashboard (inside Web) | `http://localhost:5083/dashboard` | `https://localhost:7201/dashboard` |

> The Aspire dashboard URL is the one the AppHost prints to the console on startup — the values above are from `launchSettings.json` and should match, but confirm from the console output if you see something different.

## Seeded users and authentication

The Web frontend uses cookie authentication. Four demo users are seeded automatically on startup:

| Role | Email | Password |
|---|---|---|
| Admin | admin@experimentdemo.com | Admin123! |
| Experimenter | experimenter@experimentdemo.com | Experimenter123! |
| Viewer | viewer@experimentdemo.com | Viewer123! |
| Analyst | analyst@experimentdemo.com | Analyst123! |

Navigate to `/Account/Login` on the Web frontend. Each user's credentials are pre-filled by clicking their card on the login page.

Full authentication testing steps are documented in [test-auth.md](test-auth.md). Unit-level identity wiring tests are in `AspireDemo.Web.Tests/SignInTests.cs`.

## Running the E2E tests

The E2E suite uses Playwright + Reqnroll (Gherkin BDD) + xUnit. The AppHost **must be running** before you run the tests.

**Install the Playwright browser once** (after building the test project):

```bash
pwsh samples/ExperimentFramework.AspireDemo/AspireDemo.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium
```

**Run the tests:**

```powershell
# From samples/ExperimentFramework.AspireDemo/
./run-e2e-tests.ps1
```

The script checks that the Web frontend is reachable at `https://localhost:7201` before running. If the app is not running it exits with an error. Test feature files are under `AspireDemo.E2ETests/Features/` (Authentication, Blog, LiveDemo).

## Helper scripts

All scripts are in `samples/ExperimentFramework.AspireDemo/`.

| Script | Purpose |
|---|---|
| `run-e2e-tests.ps1` | Checks app is running, then runs the full E2E suite |
| `test-rollout.ps1` | Exercises progressive rollout scenarios against the running app |
| `test-api-port.ps1` | Checks which port the ApiService is bound to |
| `clean-restart.ps1` | Force-kills all Aspire and dotnet processes then rebuilds; use when ports are stuck |
| `kill-processes.ps1` | Force-kills Aspire processes without rebuilding |

## Related documentation

- [Running the Samples](../../docs/user-guide/developer-setup/running-the-samples.md) — full sample map and Rider tips
- [Embed the Dashboard](../../docs/user-guide/developer-setup/embed.md) — how the EF dashboard is wired into a host app
- [Plugin System reference](../../docs/reference/plugins.md) — how the Blog service loads plugin DLLs at runtime
- [Governance reference](../../docs/reference/governance.md) — governance lifecycle and approval gates
