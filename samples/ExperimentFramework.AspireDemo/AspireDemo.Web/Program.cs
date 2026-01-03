using AspireDemo.Web;
using AspireDemo.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

// Configure the API client with service discovery
builder.Services.AddHttpClient<ExperimentApiClient>(client =>
{
    // Use Aspire service discovery
    client.BaseAddress = new("https+http://apiservice");
});

// Theme service for cross-component theme synchronization
builder.Services.AddScoped<ThemeService>();

// Centralized demo state service for cross-page state management
builder.Services.AddScoped<DemoStateService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAntiforgery();
app.UseOutputCache();

app.MapStaticAssets();
app.MapRazorComponents<AspireDemo.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
