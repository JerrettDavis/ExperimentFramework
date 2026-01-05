using AspireDemo.Web;
using AspireDemo.Web.Data;
using AspireDemo.Web.Services;
using ExperimentFramework.Dashboard;
using ExperimentFramework.Dashboard.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

// Add database context for Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=aspiredemo.db"));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings (relaxed for demo purposes)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanAccessExperiments", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
                  context.User.IsInRole("Admin") ||
                  context.User.IsInRole("Experimenter") ||
                  context.User.IsInRole("Viewer") ||
                  context.User.IsInRole("Analyst")));

    options.AddPolicy("CanModifyExperiments", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
                  context.User.IsInRole("Admin") ||
                  context.User.IsInRole("Experimenter")));

    options.AddPolicy("CanManageRollouts", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
                  context.User.IsInRole("Admin") ||
                  context.User.IsInRole("Experimenter")));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Enable detailed circuit errors in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
    {
        options.DetailedErrors = true;
    });
}

builder.Services.AddOutputCache();

// Configure the API client with service discovery
// Register AspireDemo.Web version (for LiveDemo page)
builder.Services.AddHttpClient("AspireDemoApiClient", client =>
{
    // Use Aspire service discovery
    client.BaseAddress = new("https+http://apiservice");
})
.AddTypedClient<AspireDemo.Web.ExperimentApiClient>();

// Register Dashboard.UI version (for Dashboard pages)
builder.Services.AddHttpClient("DashboardApiClient", client =>
{
    // Use Aspire service discovery
    client.BaseAddress = new("https+http://apiservice");
})
.AddTypedClient<ExperimentFramework.Dashboard.UI.Services.ExperimentApiClient>();

// Theme service for cross-component theme synchronization
builder.Services.AddScoped<AspireDemo.Web.ThemeService>();

// Centralized demo state service for cross-page state management
builder.Services.AddScoped<DemoStateService>();

// Add ExperimentFramework Dashboard
builder.Services.AddExperimentDashboard(options =>
{
    options.PathBase = "/dashboard";
    options.Title = "AspireDemo Experiment Dashboard";
    options.EnableAnalytics = true;
    options.EnableGovernanceUI = true;
    options.RequireAuthorization = true;
    options.AuthorizationPolicy = "CanAccessExperiments";
});

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        await IdentitySeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAntiforgery();
app.UseOutputCache();

app.MapStaticAssets();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Add ExperimentFramework Dashboard middleware (must be after auth)
app.UseExperimentDashboard();

// Map the ExperimentFramework Dashboard endpoints
app.MapExperimentDashboard("/dashboard");

app.MapRazorComponents<AspireDemo.Web.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ExperimentFramework.Dashboard.UI.Components.Pages.Home).Assembly);

app.MapDefaultEndpoints();

app.Run();
