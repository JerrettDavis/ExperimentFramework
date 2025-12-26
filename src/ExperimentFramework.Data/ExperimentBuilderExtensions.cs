using ExperimentFramework.Data.Decorators;
using ExperimentFramework.Data.Recording;

namespace ExperimentFramework.Data;

/// <summary>
/// Extension methods for configuring outcome collection in the experiment framework.
/// </summary>
public static class ExperimentBuilderExtensions
{
    /// <summary>
    /// Enables automatic outcome collection for experiments.
    /// </summary>
    /// <param name="builder">The experiment framework builder.</param>
    /// <param name="configure">Optional configuration for outcome collection.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// When enabled, the framework automatically records:
    /// <list type="bullet">
    /// <item><description>Duration of each invocation</description></item>
    /// <item><description>Success/failure outcomes</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Make sure to also call <see cref="ServiceCollectionExtensions.AddExperimentDataCollection"/>
    /// to register the required services.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var experiments = ExperimentFrameworkBuilder.Create()
    ///     .WithOutcomeCollection(opts =>
    ///     {
    ///         opts.CollectDuration = true;
    ///         opts.CollectErrors = true;
    ///     })
    ///     .Trial&lt;IMyService&gt;(...)
    ///     .UseSourceGenerators();
    /// </code>
    /// </example>
    public static ExperimentFrameworkBuilder WithOutcomeCollection(
        this ExperimentFrameworkBuilder builder,
        Action<OutcomeRecorderOptions>? configure = null)
    {
        var options = new OutcomeRecorderOptions();
        configure?.Invoke(options);

        var factory = new OutcomeCollectionDecoratorFactory(options);
        return builder.AddDecoratorFactory(factory);
    }

    /// <summary>
    /// Enables automatic outcome collection with a custom experiment name resolver.
    /// </summary>
    /// <param name="builder">The experiment framework builder.</param>
    /// <param name="experimentNameResolver">
    /// A function that resolves the experiment name from the service type name.
    /// </param>
    /// <param name="configure">Optional configuration for outcome collection.</param>
    /// <returns>The builder for chaining.</returns>
    public static ExperimentFrameworkBuilder WithOutcomeCollection(
        this ExperimentFrameworkBuilder builder,
        Func<string, string> experimentNameResolver,
        Action<OutcomeRecorderOptions>? configure = null)
    {
        var options = new OutcomeRecorderOptions();
        configure?.Invoke(options);

        var factory = new OutcomeCollectionDecoratorFactory(options, experimentNameResolver);
        return builder.AddDecoratorFactory(factory);
    }
}
