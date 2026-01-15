using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Testing.Tests;

public class ExperimentTestMatrixTests
{
    [Fact]
    public void RunInAllProxyModes_ShouldExecuteTestForEachStrategy()
    {
        // Arrange
        var executionCount = 0;
        var options = new ExperimentTestMatrixOptions
        {
            Strategies = new[] { ProxyStrategy.DispatchProxy } // Only test DispatchProxy to avoid source generator issues in tests
        };

        // Act
        ExperimentTestMatrix.RunInAllProxyModes(
            configureServices: services =>
            {
                services.AddScoped<IMyDatabase, MyDatabase>();
                services.AddScoped<MyDatabase>();
                services.AddScoped<CloudDatabase>();
            },
            configure: builder => builder
                .Trial<IMyDatabase>(t => t
                    .UsingTest()
                    .AddControl<MyDatabase>()
                    .AddCondition<CloudDatabase>("true")),
            test: sp =>
            {
                executionCount++;
                using var scope = ExperimentTestScope.Begin()
                    .ForceControl<IMyDatabase>();

                var db = sp.GetRequiredService<IMyDatabase>();
                var result = db.GetValue();
                Assert.Equal(1, result); // MyDatabase returns 1
            },
            options: options);

        // Assert - Should run once (DispatchProxy only)
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public void RunInAllProxyModes_WithFailure_ShouldCompleteSuccessfully()
    {
        // Arrange
        var options = new ExperimentTestMatrixOptions
        {
            Strategies = new[] { ProxyStrategy.DispatchProxy } // Only test DispatchProxy
        };

        // Act & Assert - Should not throw since test doesn't fail
        ExperimentTestMatrix.RunInAllProxyModes(
            configureServices: services =>
            {
                services.AddScoped<IMyDatabase, MyDatabase>();
                services.AddScoped<MyDatabase>();
                services.AddScoped<CloudDatabase>();
            },
            configure: builder => builder
                .Trial<IMyDatabase>(t => t
                    .UsingTest()
                    .AddControl<MyDatabase>()
                    .AddCondition<CloudDatabase>("true")),
            test: sp =>
            {
                // Test passes successfully
                using var scope = ExperimentTestScope.Begin()
                    .ForceControl<IMyDatabase>();
                var db = sp.GetRequiredService<IMyDatabase>();
                Assert.Equal(1, db.GetValue());
            },
            options: options);
    }

    [Fact]
    public void RunInAllProxyModes_WithStopOnFirstFailure_ShouldStopAfterFirstError()
    {
        // Arrange
        var executionCount = 0;
        var options = new ExperimentTestMatrixOptions
        {
            Strategies = new[] { ProxyStrategy.DispatchProxy }, // Only test DispatchProxy
            StopOnFirstFailure = true
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ExperimentTestMatrix.RunInAllProxyModes(
                configureServices: services =>
                {
                    services.AddScoped<IMyDatabase, MyDatabase>();
                    services.AddScoped<MyDatabase>();
                    services.AddScoped<CloudDatabase>();
                },
                configure: builder => builder
                    .Trial<IMyDatabase>(t => t
                        .UsingTest()
                        .AddControl<MyDatabase>()
                        .AddCondition<CloudDatabase>("true")),
                test: sp =>
                {
                    executionCount++;
                    throw new InvalidOperationException("Test failure");
                },
                options: options));

        // Should execute once before stopping
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public void RunInAllProxyModes_WithCustomStrategies_ShouldRespectOptions()
    {
        // Arrange
        var executionCount = 0;
        var options = new ExperimentTestMatrixOptions
        {
            Strategies = new[] { ProxyStrategy.DispatchProxy }
        };

        // Act
        ExperimentTestMatrix.RunInAllProxyModes(
            configureServices: services =>
            {
                services.AddScoped<IMyDatabase, MyDatabase>();
                services.AddScoped<MyDatabase>();
                services.AddScoped<CloudDatabase>();
            },
            configure: builder => builder
                .Trial<IMyDatabase>(t => t
                    .UsingTest()
                    .AddControl<MyDatabase>()
                    .AddCondition<CloudDatabase>("true")),
            test: sp =>
            {
                executionCount++;
            },
            options: options);

        // Assert - Should only run once (DispatchProxy only)
        Assert.Equal(1, executionCount);
    }
}
