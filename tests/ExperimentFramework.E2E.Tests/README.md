# ExperimentFramework E2E Tests

Playwright + Reqnroll (Gherkin BDD) suite that drives a live DashboardHost.

## Running locally

**Terminal 1 — start the host:**

```bash
dotnet run --project samples/ExperimentFramework.DashboardHost -- \
  --seed=docs --Urls=http://localhost:5195
```

**Terminal 2 — run the suite once the host is ready:**

```bash
E2E__BaseUrl=http://localhost:5195 \
  dotnet test tests/ExperimentFramework.E2E.Tests --filter "Category!=Manual"
```

Install Playwright browsers once after building:

```bash
pwsh tests/ExperimentFramework.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
```

## Coverage collection in CI

CI instruments the DashboardHost process with `dotnet-coverage` so that
server-side code execution is recorded during the E2E run. The resulting
cobertura report is uploaded to Codecov with the `e2e` flag (separate from
the `unittests` flag produced by the main test job).

To reproduce locally:

```bash
# Terminal 1 — host under coverage
dotnet-coverage collect \
  -f cobertura \
  -o e2e-dashboard-coverage.xml \
  "dotnet run --project samples/ExperimentFramework.DashboardHost -- --seed=docs --Urls=http://localhost:5195"

# Terminal 2 — run tests
E2E__BaseUrl=http://localhost:5195 \
  dotnet test tests/ExperimentFramework.E2E.Tests --filter "Category!=Manual"

# Stop Terminal 1 with Ctrl+C; dotnet-coverage flushes the report on exit.
```

The generated `e2e-dashboard-coverage.xml` can be inspected with any
cobertura-compatible viewer (e.g. ReportGenerator).
