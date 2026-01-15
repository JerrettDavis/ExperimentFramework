using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Testing.Tests;

public class ExperimentTestScopeTests
{
    [Fact]
    public void NestedScopes_ShouldWorkCorrectly()
    {
        // Arrange
        var host = ExperimentTestHost.Create(services =>
        {
            services.AddScoped<IMyDatabase, MyDatabase>();
            services.AddScoped<MyDatabase>();
            services.AddScoped<CloudDatabase>();
        })
        .WithExperiments(experiments => experiments
            .Trial<IMyDatabase>(trial => trial
                .UsingTest()
                .AddControl<MyDatabase>()
                .AddCondition<CloudDatabase>("true"))
            .UseDispatchProxy())
        .Build();

        // Act & Assert - Outer scope forces control
        using (ExperimentTestScope.Begin().ForceControl<IMyDatabase>())
        {
            var db1 = host.Services.GetRequiredService<IMyDatabase>();
            var result1 = db1.GetValue();
            Assert.Equal(1, result1); // MyDatabase returns 1

            // Inner scope forces condition
            using (ExperimentTestScope.Begin().ForceCondition<IMyDatabase>("true"))
            {
                var db2 = host.Services.GetRequiredService<IMyDatabase>();
                var result2 = db2.GetValue();
                Assert.Equal(2, result2); // CloudDatabase returns 2
            }

            // Back to outer scope - should use control again
            var db3 = host.Services.GetRequiredService<IMyDatabase>();
            var result3 = db3.GetValue();
            Assert.Equal(1, result3); // MyDatabase returns 1
        }
    }

    [Fact]
    public void FreezeSelection_ShouldPersistFirstSelection()
    {
        // Arrange
        var host = ExperimentTestHost.Create(services =>
        {
            services.AddScoped<IMyDatabase, MyDatabase>();
            services.AddScoped<MyDatabase>();
            services.AddScoped<CloudDatabase>();
        })
        .WithExperiments(experiments => experiments
            .Trial<IMyDatabase>(trial => trial
                .UsingTest()
                .AddControl<MyDatabase>()
                .AddCondition<CloudDatabase>("true"))
            .UseDispatchProxy())
        .Build();

        // Act
        using var scope = ExperimentTestScope.Begin()
            .ForceCondition<IMyDatabase>("true")
            .FreezeSelection();

        // First call
        var db1 = host.Services.GetRequiredService<IMyDatabase>();
        var result1 = db1.GetValue();

        // Second call - should use the same frozen selection
        var db2 = host.Services.GetRequiredService<IMyDatabase>();
        var result2 = db2.GetValue();

        // Assert
        Assert.Equal(2, result1); // CloudDatabase
        Assert.Equal(2, result2); // Should still be CloudDatabase
    }
}
