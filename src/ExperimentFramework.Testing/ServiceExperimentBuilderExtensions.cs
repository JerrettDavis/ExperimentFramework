namespace ExperimentFramework.Testing;

/// <summary>
/// Extension methods for integrating test selection mode with ExperimentFramework builders.
/// </summary>
public static class ServiceExperimentBuilderExtensions
{
    /// <summary>
    /// Configures trial selection to use the test selection provider for deterministic testing.
    /// </summary>
    /// <typeparam name="TService">The service interface being experimented on.</typeparam>
    /// <param name="builder">The service experiment builder.</param>
    /// <param name="selectorName">
    /// Optional selector name. If null, a default name will be derived from the service type.
    /// </param>
    /// <returns>The builder for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// The test selection mode allows you to force specific trial keys using
    /// <see cref="ExperimentTestScope"/> without mocking or replacing services.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// .Trial&lt;IMyService&gt;(t => t
    ///     .UsingTest()
    ///     .AddControl&lt;DefaultImpl&gt;()
    ///     .AddCondition&lt;TestImpl&gt;("test"))
    /// </code>
    /// </para>
    /// </remarks>
    public static ServiceExperimentBuilder<TService> UsingTest<TService>(
        this ServiceExperimentBuilder<TService> builder,
        string? selectorName = null)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UsingCustomMode(TestSelectionProvider.ModeId, selectorName);
    }
}
