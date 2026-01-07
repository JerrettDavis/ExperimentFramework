using Microsoft.AspNetCore.Builder;
using ExperimentFramework.Dashboard;

namespace ExperimentFramework.Dashboard.Tests;

/// <summary>
/// Test program for WebApplicationFactory.
/// </summary>
public class TestProgram
{
    public static void Main(string[] args)
    {
        var options = new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        };
        var builder = WebApplication.CreateBuilder(options);

        // Add required services
        builder.Services.AddRouting();

        // Add dashboard services
        builder.Services.AddExperimentDashboard(options =>
        {
            options.PathBase = "/dashboard";
            options.RequireAuthorization = false;
        });

        // Add default test registry (will be overridden by ConfigureTestServices)
        builder.Services.AddSingleton<ExperimentFramework.Admin.IExperimentRegistry>(sp => new TestExperimentRegistry());

        var app = builder.Build();

        app.UseRouting();
        app.MapExperimentDashboard("/dashboard");

        app.Run();
    }
}
