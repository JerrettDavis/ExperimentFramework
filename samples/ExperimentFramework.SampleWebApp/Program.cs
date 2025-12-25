using ExperimentFramework;
using ExperimentFramework.SampleWebApp;
using ExperimentFramework.SampleWebApp.Services;
using ExperimentFramework.StickyRouting;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

// Add session support (required for session-based sticky routing)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HTTP context accessor (needed for SessionIdentityProvider)
builder.Services.AddHttpContextAccessor();

// Add feature management for feature flag experiments
builder.Services.AddFeatureManagement();

// Add controllers
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ========================================
// Register Service Implementations
// ========================================
// Recommendation algorithms
builder.Services.AddScoped<PopularityRecommendationEngine>();
builder.Services.AddScoped<MLRecommendationEngine>();
builder.Services.AddScoped<CollaborativeRecommendationEngine>();

// Checkout flows
builder.Services.AddScoped<StandardCheckoutFlow>();
builder.Services.AddScoped<ExpressCheckoutFlow>();
builder.Services.AddScoped<OneClickCheckoutFlow>();

// Register default interfaces
builder.Services.AddScoped<IRecommendationEngine, PopularityRecommendationEngine>();
builder.Services.AddScoped<ICheckoutFlow, StandardCheckoutFlow>();

// ========================================
// Configure Experiment Framework
// ========================================
// Register StickyRouting provider and identity provider for sticky routing
builder.Services.AddExperimentStickyRouting();
builder.Services.AddScoped<IExperimentIdentityProvider, SessionIdentityProvider>();

// Configure experiments using FLUENT API (.UseSourceGenerators())
var experiments = ExperimentConfiguration.ConfigureWebExperiments();
builder.Services.AddExperimentFramework(experiments);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable session middleware (must be before endpoints)
app.UseSession();

app.UseHttpsRedirection();
app.MapControllers();

// Add a simple home page with API documentation
app.MapGet("/", () => Results.Json(new
{
    AppName = "ExperimentFramework Web Sample",
    Description = "Demonstrates sticky A/B testing in a web application using the fluent API approach (.UseSourceGenerators())",
    Experiments = new[]
    {
        new
        {
            Name = "Recommendation Algorithm",
            Type = "Sticky Routing",
            Endpoint = "/api/recommendations",
            Description = "Each user consistently sees recommendations from the same algorithm (Popularity / ML / Collaborative)",
            HowItWorks = "Based on user/session identity hash. Try refreshing - you'll always see the same algorithm!"
        },
        new
        {
            Name = "Checkout Flow",
            Type = "Feature Flag",
            Endpoint = "/api/checkout/flow",
            Description = "Toggle between Standard and Express checkout flows",
            HowItWorks = "Controlled by 'EnableExpressCheckout' feature flag in appsettings.json"
        }
    },
    TryIt = new
    {
        GetRecommendations = "GET /api/recommendations",
        GetAlgorithmInfo = "GET /api/recommendations/algorithm",
        GetCheckoutFlow = "GET /api/checkout/flow",
        CompleteCheckout = "POST /api/checkout/complete"
    },
    SourceGeneration = new
    {
        Method = "Fluent API",
        Trigger = ".UseSourceGenerators()",
        Location = "ExperimentConfiguration.cs",
        Note = "Source generators create zero-overhead proxies at compile time - no attributes needed!"
    }
}))
.WithName("Home");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
