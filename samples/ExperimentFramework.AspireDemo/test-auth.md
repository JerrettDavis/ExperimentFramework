# Authentication Test Plan

## Issue Fixed
The dashboard middleware was being registered too late in the ASP.NET Core pipeline (during endpoint mapping instead of in the middleware pipeline), so authentication was never enforced.

## Fix Applied
1. Created `UseExperimentDashboard()` method to properly register middleware in the pipeline
2. Register `DashboardOptions` as singleton (in addition to IOptions pattern)
3. Updated `Program.cs` to call `app.UseExperimentDashboard()` after authentication/authorization middleware
4. Split middleware registration from endpoint mapping

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

The middleware was being registered inside `MapExperimentDashboard()`, which is called during endpoint mapping:

```csharp
public static RouteGroupBuilder MapExperimentDashboard(...)
{
    // ⚠️ WRONG - Middleware registered during endpoint mapping (too late!)
    if (endpoints is IApplicationBuilder app)
    {
        app.UseMiddleware<DashboardMiddleware>(options);
    }

    var group = endpoints.MapGroup(pathPrefix);
    // ...
}
```

By the time endpoint mapping happens, the middleware pipeline is already built. Adding middleware here doesn't work correctly.

## What's Fixed Now

Now you must call `UseExperimentDashboard()` to register the middleware BEFORE mapping endpoints:

```csharp
// In Program.cs:
app.UseAuthentication();
app.UseAuthorization();

// ✅ Register middleware in the pipeline (BEFORE endpoint mapping)
app.UseExperimentDashboard();

// Then map endpoints
app.MapExperimentDashboard("/dashboard");
```

The middleware is now properly registered in the pipeline and will enforce authentication before allowing access to dashboard routes.

## Updated Usage Pattern

**Before** (broken):
```csharp
builder.Services.AddExperimentDashboard(options => { ... });
// ...
app.MapExperimentDashboard("/dashboard"); // ⚠️ Auth not enforced
```

**After** (working):
```csharp
builder.Services.AddExperimentDashboard(options => { ... });
// ...
app.UseAuthentication();
app.UseAuthorization();
app.UseExperimentDashboard(); // ✅ Register middleware
app.MapExperimentDashboard("/dashboard"); // ✅ Map endpoints
```
