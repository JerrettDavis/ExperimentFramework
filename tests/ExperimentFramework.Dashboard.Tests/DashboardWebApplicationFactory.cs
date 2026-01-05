using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ExperimentFramework;
using ExperimentFramework.Admin;
using ExperimentFramework.Dashboard;
using ExperimentFramework.Dashboard.Abstractions;
using AdminExperimentInfo = ExperimentFramework.Admin.ExperimentInfo;
using AdminTrialInfo = ExperimentFramework.Admin.TrialInfo;

namespace ExperimentFramework.Dashboard.Tests;

/// <summary>
/// Test web application factory for integration testing.
/// </summary>
public class DashboardWebApplicationFactory : WebApplicationFactory<TestProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Add a simple in-memory experiment registry for testing
            services.AddSingleton<IExperimentRegistry>(sp => new TestExperimentRegistry());
        });
    }
}

/// <summary>
/// Simple test implementation of IExperimentRegistry.
/// </summary>
public class TestExperimentRegistry : IExperimentRegistry
{
    private readonly List<AdminExperimentInfo> _experiments = new()
    {
        new AdminExperimentInfo
        {
            Name = "test-experiment",
            ServiceType = typeof(ITestService),
            IsActive = true,
            Trials = new List<AdminTrialInfo>
            {
                new AdminTrialInfo { Key = "control", ImplementationType = typeof(TestControlService), IsControl = true },
                new AdminTrialInfo { Key = "variant-a", ImplementationType = typeof(TestVariantService), IsControl = false }
            }
        }
    };

    public IEnumerable<AdminExperimentInfo> GetAllExperiments() => _experiments;

    public AdminExperimentInfo? GetExperiment(string name) =>
        _experiments.FirstOrDefault(e => e.Name == name);
}

/// <summary>
/// Test marker interface for experiments.
/// </summary>
public interface ITestService { }

/// <summary>
/// Test control implementation.
/// </summary>
public class TestControlService : ITestService { }

/// <summary>
/// Test variant implementation.
/// </summary>
public class TestVariantService : ITestService { }
