# Authentication Test Plan

## Issue Fixed
The `MapExperimentDashboard()` was creating a new `DashboardOptions` instance instead of using the one configured with `AddExperimentDashboard()`, so the `RequireAuthorization = true` setting was being ignored.

## Fix Applied
Modified `EndpointRouteBuilderExtensions.cs` to retrieve `DashboardOptions` from DI before falling back to creating a new instance.

## Testing Steps

1. **Stop the running application** (if any)

2. **Rebuild and run**:
   ```bash
   cd samples/ExperimentFramework.AspireDemo
   dotnet build
   dotnet run --project AspireDemo.AppHost
   ```

3. **Test unauthenticated access**:
   - Open browser to `https://localhost:PORT/dashboard`
   - **Expected**: Should immediately redirect to `/Account/Login?returnUrl=/dashboard`
   - **Should NOT see**: Dashboard content without logging in

4. **Test login flow**:
   - Should see login page with 4 demo user cards
   - Click "Admin" card (auto-fills credentials)
   - **Expected**: Redirects back to `/dashboard` after successful login
   - **Should see**: Dashboard with user info in sidebar (Admin badge in red)

5. **Test authenticated access**:
   - Navigate around dashboard (Experiments, Rollout, etc.)
   - **Expected**: All pages accessible without re-login
   - **Should see**: User name and role badge in sidebar throughout

6. **Test logout**:
   - Click logout button in sidebar
   - **Expected**: Redirected to login page
   - Then try to access `/dashboard` again
   - **Expected**: Redirected to login (not seeing dashboard content)

## Demo Users

- **Admin** (admin@experimentdemo.com / Admin123!)
  - Full access to all features
  - Red "Admin" badge

- **Experimenter** (experimenter@experimentdemo.com / Experimenter123!)
  - Can modify experiments and rollouts
  - Blue "Experimenter" badge

- **Viewer** (viewer@experimentdemo.com / Viewer123!)
  - Read-only access
  - Gray "Viewer" badge

- **Analyst** (analyst@experimentdemo.com / Analyst123!)
  - Analytics focus
  - Green "Analyst" badge

## What Was Wrong Before

```csharp
// OLD CODE - Always created new options
var options = new DashboardOptions { PathBase = pathPrefix };
configure?.Invoke(options); // This was never called in the scenario
```

This meant that even though you configured:
```csharp
builder.Services.AddExperimentDashboard(options =>
{
    options.RequireAuthorization = true; // ⚠️ This was ignored!
    options.AuthorizationPolicy = "CanAccessExperiments";
});
```

The middleware was getting a fresh `DashboardOptions` with default values (`RequireAuthorization = false`).

## What's Fixed Now

```csharp
// NEW CODE - Gets options from DI first
var services = appBuilder.ApplicationServices;
options = services.GetService(typeof(DashboardOptions)) as DashboardOptions;

if (options == null)
{
    options = new DashboardOptions { PathBase = pathPrefix };
}
```

Now the middleware uses the options you configured in `AddExperimentDashboard`, so `RequireAuthorization = true` is properly enforced.
