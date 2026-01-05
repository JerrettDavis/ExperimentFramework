using AspireDemo.Web;
using AspireDemo.Web.Services;
using ExperimentFramework.Dashboard;
using ExperimentFramework.Dashboard.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

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
    options.RequireAuthorization = false;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAntiforgery();
app.UseOutputCache();

app.MapStaticAssets();

// Map the ExperimentFramework Dashboard
app.MapExperimentDashboard("/dashboard");

app.MapRazorComponents<AspireDemo.Web.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ExperimentFramework.Dashboard.UI.Components.Pages.Home).Assembly);

app.MapDefaultEndpoints();

app.Run();
