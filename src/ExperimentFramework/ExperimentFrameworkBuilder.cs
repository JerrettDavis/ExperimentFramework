using ExperimentFramework.Decorators;
using ExperimentFramework.Models;
using ExperimentFramework.Naming;

namespace ExperimentFramework;

/// <summary>
/// Declarative composition root for configuring experiments, trials, and decorators.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ExperimentFrameworkBuilder"/> is the primary entry point for configuring the experiment framework.
/// It is intended to be used at application startup to declaratively define:
/// </para>
/// <list type="bullet">
/// <item><description>Which services participate in experiments.</description></item>
/// <item><description>How trials are selected (feature flags or configuration).</description></item>
/// <item><description>Which decorators are applied globally to experiment invocations.</description></item>
/// </list>
/// <para>
/// The builder itself does not interact with dependency injection directly. Instead, it produces
/// an immutable configuration object that is later consumed by the DI integration layer.
/// </para>
/// </remarks>
public sealed class ExperimentFrameworkBuilder
{
    private readonly List<IExperimentDecoratorFactory> _decoratorFactories = new();
    private readonly List<IExperimentDefinition> _definitions = new();
    private IExperimentNamingConvention _namingConvention = new DefaultExperimentNamingConvention();

    private ExperimentFrameworkBuilder() { }

    /// <summary>
    /// Creates a new experiment framework builder.
    /// </summary>
    /// <returns>A new <see cref="ExperimentFrameworkBuilder"/> instance.</returns>
    /// <remarks>
    /// This method enforces the use of a fluent, declarative configuration style.
    /// </remarks>
    public static ExperimentFrameworkBuilder Create() => new();

    /// <summary>
    /// Adds built-in logging-related decorators to the global decorator pipeline.
    /// </summary>
    /// <param name="configure">
    /// A configuration action used to enable specific logging behaviors such as benchmarks
    /// and error logging.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// This method is a convenience wrapper around <see cref="ExperimentLoggingBuilder"/> and is intended
    /// for common logging scenarios.
    /// </para>
    /// <para>
    /// Decorators added here are appended to the global decorator pipeline and are executed
    /// outer-to-inner in the order they are registered.
    /// </para>
    /// </remarks>
    public ExperimentFrameworkBuilder AddLogger(Action<ExperimentLoggingBuilder> configure)
    {
        var b = new ExperimentLoggingBuilder();
        configure(b);

        foreach (var f in b.Build())
            _decoratorFactories.Add(f);

        return this;
    }

    /// <summary>
    /// Adds a custom decorator factory to the global decorator pipeline.
    /// </summary>
    /// <param name="factory">The decorator factory to add.</param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// Decorators are invoked in registration order, with the first registered decorator
    /// acting as the outermost wrapper.
    /// </para>
    /// <para>
    /// Custom decorators can be used to integrate telemetry systems (OpenTelemetry, metrics),
    /// enforce policies, or inject cross-cutting behavior.
    /// </para>
    /// </remarks>
    public ExperimentFrameworkBuilder AddDecoratorFactory(IExperimentDecoratorFactory factory)
    {
        _decoratorFactories.Add(factory);
        return this;
    }

    /// <summary>
    /// Configures a custom naming convention for experiment selectors.
    /// </summary>
    /// <param name="convention">
    /// The naming convention to use when deriving selector names from service types.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// If not specified, the framework uses <see cref="DefaultExperimentNamingConvention"/>.
    /// </para>
    /// <para>
    /// Custom conventions can be used to implement organization-specific naming patterns
    /// (e.g., prefixes, suffixes, or mapping to external configuration systems).
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convention"/> is <see langword="null"/>.</exception>
    public ExperimentFrameworkBuilder UseNamingConvention(IExperimentNamingConvention convention)
    {
        _namingConvention = convention ?? throw new ArgumentNullException(nameof(convention));
        return this;
    }

    /// <summary>
    /// Defines an experiment for a specific service interface.
    /// </summary>
    /// <typeparam name="TService">
    /// The service interface type that will be proxied and routed to trial implementations.
    /// </typeparam>
    /// <param name="configure">
    /// A configuration action that defines trials, selection mode, and error handling behavior
    /// for the service.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// Each call to <see cref="Define{TService}"/> registers a single experiment definition.
    /// Multiple services can be defined within the same builder.
    /// </para>
    /// <para>
    /// The service must be registered in the DI container before calling AddExperimentFramework.
    /// </para>
    /// </remarks>
    public ExperimentFrameworkBuilder Define<TService>(Action<ServiceExperimentBuilder<TService>> configure)
        where TService : class
    {
        var b = new ServiceExperimentBuilder<TService>();
        configure(b);
        _definitions.Add(b.Build(_namingConvention));
        return this;
    }

    /// <summary>
    /// Triggers compile-time code generation for all defined experiments.
    /// This method marks the builder to use source-generated proxies.
    /// </summary>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// When this method is called, the ExperimentFramework source generator will analyze
    /// all .Define&lt;T&gt; calls in the builder chain and generate strongly-typed proxy
    /// classes at compile time.
    /// </para>
    /// <para>
    /// Generated proxies will be named by stripping the 'I' prefix and adding 'ExperimentProxy'
    /// suffix (e.g., IMyDatabase → DatabaseExperimentProxy).
    /// </para>
    /// <para>
    /// This is a marker method - the source generator detects calls to this method during
    /// compilation. The implementation simply returns the builder for fluent chaining.
    /// </para>
    /// </remarks>
    public ExperimentFrameworkBuilder UseSourceGenerators()
    {
        // Marker method - detected by source generator
        return this;
    }

    /// <summary>
    /// Registers a custom user-provided proxy implementation for a service type,
    /// bypassing automatic source generation.
    /// </summary>
    /// <typeparam name="TProxy">
    /// The custom proxy type that implements the experiment interface and provides
    /// trial selection logic.
    /// </typeparam>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// This method allows users to provide their own proxy implementations when the
    /// generated proxies don't meet their specific requirements. The custom proxy
    /// must implement the same interface as the experiment and handle trial selection,
    /// error policies, and decorator pipeline integration.
    /// </para>
    /// <para>
    /// When this method is used, the source generator will skip generating a proxy
    /// for the associated interface.
    /// </para>
    /// <para>
    /// This is a marker method - detected by source generator during compilation.
    /// </para>
    /// </remarks>
    public ExperimentFrameworkBuilder UseCustomProxy<TProxy>() where TProxy : class
    {
        // Marker method - detected by source generator
        // TODO: Phase 4 - Store custom proxy type in configuration for DI integration
        return this;
    }

    /// <summary>
    /// Builds the immutable framework configuration from the current builder state.
    /// </summary>
    /// <returns>An <see cref="ExperimentFrameworkConfiguration"/> instance.</returns>
    /// <remarks>
    /// This method is intended to be called by the DI integration layer. Once built,
    /// the configuration should be treated as immutable.
    /// </remarks>
    internal ExperimentFrameworkConfiguration Build()
        => new(_decoratorFactories.ToArray(), _definitions.ToArray());
}