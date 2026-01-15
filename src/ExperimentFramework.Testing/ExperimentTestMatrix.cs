namespace ExperimentFramework.Testing;

/// <summary>
/// Represents the proxy strategy to use for an experiment.
/// </summary>
public enum ProxyStrategy
{
    /// <summary>
    /// Use source-generated compile-time proxies (default, fastest).
    /// </summary>
    SourceGenerated,

    /// <summary>
    /// Use DispatchProxy-based runtime proxies (slower, more flexible).
    /// </summary>
    DispatchProxy
}

/// <summary>
/// Options for controlling test matrix execution.
/// </summary>
public sealed class ExperimentTestMatrixOptions
{
    /// <summary>
    /// Gets or sets the proxy strategies to test against.
    /// </summary>
    public ProxyStrategy[] Strategies { get; set; } = 
    {
        ProxyStrategy.SourceGenerated,
        ProxyStrategy.DispatchProxy
    };

    /// <summary>
    /// Gets or sets whether to stop on first failure or continue testing all strategies.
    /// </summary>
    public bool StopOnFirstFailure { get; set; } = false;
}

/// <summary>
/// Utilities for running experiments across multiple proxy strategies.
/// </summary>
public static class ExperimentTestMatrix
{
    /// <summary>
    /// Runs the same test logic across all configured proxy strategies.
    /// </summary>
    /// <param name="configure">Action to configure the experiment framework builder.</param>
    /// <param name="test">The test action to execute with the service provider.</param>
    /// <param name="options">Optional matrix test options.</param>
    /// <exception cref="AggregateException">
    /// Thrown if any strategy test fails (when StopOnFirstFailure is false).
    /// </exception>
    /// <example>
    /// <code>
    /// ExperimentTestMatrix.RunInAllProxyModes(
    ///     builder => builder
    ///         .Trial&lt;IMyService&gt;(t => t
    ///             .UsingTest()
    ///             .AddControl&lt;DefaultImpl&gt;()
    ///             .AddCondition&lt;TestImpl&gt;("test")),
    ///     sp =>
    ///     {
    ///         using var scope = ExperimentTestScope.Begin().ForceCondition&lt;IMyService&gt;("test");
    ///         var svc = sp.GetRequiredService&lt;IMyService&gt;();
    ///         Assert.Equal(42, svc.GetValue());
    ///     });
    /// </code>
    /// </example>
    public static void RunInAllProxyModes(
        Action<ExperimentFrameworkBuilder> configure,
        Action<IServiceProvider> test,
        ExperimentTestMatrixOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(test);

        options ??= new ExperimentTestMatrixOptions();
        var exceptions = new List<Exception>();

        foreach (var strategy in options.Strategies)
        {
            try
            {
                RunTestWithStrategy(strategy, configure, test);
            }
            catch (Exception ex)
            {
                var wrappedException = new InvalidOperationException(
                    $"Test failed with {strategy} proxy strategy", ex);
                
                if (options.StopOnFirstFailure)
                {
                    throw wrappedException;
                }

                exceptions.Add(wrappedException);
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException(
                $"Test failed for {exceptions.Count} proxy strategy/strategies",
                exceptions);
        }
    }

    private static void RunTestWithStrategy(
        ProxyStrategy strategy,
        Action<ExperimentFrameworkBuilder> configure,
        Action<IServiceProvider> test)
    {
        var host = ExperimentTestHost.Create(services => { })
            .WithExperiments(builder =>
            {
                // Apply strategy
                if (strategy == ProxyStrategy.DispatchProxy)
                {
                    builder.UseDispatchProxy();
                }
                else
                {
                    builder.UseSourceGenerators();
                }

                // Apply user configuration
                configure(builder);
            })
            .Build();

        test(host.Services);
    }
}
