using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Testing.Tests;

public class ExperimentTestHostTests
{
    [Fact]
    public void Create_WithServices_ShouldBuildSuccessfully()
    {
        // Arrange & Act
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

        // Assert
        Assert.NotNull(host);
        Assert.NotNull(host.Services);
        Assert.NotNull(host.Trace);
        Assert.NotNull(host.EventSink);
    }

    [Fact]
    public async Task ForceControl_ShouldRouteToControlImplementation()
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
            .ForceControl<IMyDatabase>();

        var db = host.Services.GetRequiredService<IMyDatabase>();
        var result = await db.GetConnectionStringAsync();

        // Assert
        Assert.Equal("localhost", result);
        Assert.True(host.Trace.ExpectRouted<IMyDatabase>("control"));
    }

    [Fact]
    public async Task ForceCondition_ShouldRouteToConditionImplementation()
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
            .ForceCondition<IMyDatabase>("true");

        var db = host.Services.GetRequiredService<IMyDatabase>();
        var result = await db.GetConnectionStringAsync();

        // Assert
        Assert.Equal("cloud.example.com", result);
        Assert.True(host.Trace.ExpectRouted<IMyDatabase>("true"));
    }

    [Fact]
    public void FreezeSelection_ShouldMaintainSameSelectionAcrossInvocations()
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

        var db1 = host.Services.GetRequiredService<IMyDatabase>();
        var result1 = db1.GetValue();

        var db2 = host.Services.GetRequiredService<IMyDatabase>();
        var result2 = db2.GetValue();

        // Assert
        Assert.Equal(2, result1); // CloudDatabase returns 2
        Assert.Equal(2, result2); // Should still be CloudDatabase
        
        var events = host.Trace.GetEventsFor<IMyDatabase>();
        Assert.All(events, e => Assert.Equal("true", e.SelectedTrialKey));
    }

    [Fact]
    public void ExperimentTestScope_ShouldRestorePreviousState()
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

        // Act & Assert - Without scope, should use control
        var db1 = host.Services.GetRequiredService<IMyDatabase>();
        var result1 = db1.GetValue();
        Assert.Equal(1, result1); // MyDatabase returns 1

        // With scope, should use condition
        using (var scope = ExperimentTestScope.Begin()
            .ForceCondition<IMyDatabase>("true"))
        {
            var db2 = host.Services.GetRequiredService<IMyDatabase>();
            var result2 = db2.GetValue();
            Assert.Equal(2, result2); // CloudDatabase returns 2
        }

        // After scope disposed, should use control again
        var db3 = host.Services.GetRequiredService<IMyDatabase>();
        var result3 = db3.GetValue();
        Assert.Equal(1, result3); // MyDatabase returns 1
    }
}
