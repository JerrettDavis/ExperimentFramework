using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Testing.Tests;

public class ExperimentTestMatrixTests
{
    [Fact]
    public void RunInAllProxyModes_ShouldExecuteTestForEachStrategy()
    {
        // Arrange
        var executionCount = 0;

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
            });

        // Assert - Should run twice (SourceGenerated + DispatchProxy)
        Assert.Equal(2, executionCount);
    }

    [Fact]
    public void RunInAllProxyModes_WithFailure_ShouldThrowAggregateException()
    {
        // Act & Assert
        var exception = Assert.Throws<AggregateException>(() =>
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
                    throw new InvalidOperationException("Test failure");
                }));

        Assert.Equal(2, exception.InnerExceptions.Count);
    }

    [Fact]
    public void RunInAllProxyModes_WithStopOnFirstFailure_ShouldStopAfterFirstError()
    {
        // Arrange
        var executionCount = 0;
        var options = new ExperimentTestMatrixOptions
        {
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

        // Should only execute once before stopping
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
