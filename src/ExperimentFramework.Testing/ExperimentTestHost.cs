using ExperimentFramework.Selection;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Testing;

/// <summary>
/// Test host for easily setting up and testing experiments.
/// </summary>
/// <example>
/// <code>
/// var host = ExperimentTestHost.Create(services =>
/// {
///     services.AddScoped&lt;IMyDatabase, MyDatabase&gt;();
///     services.AddScoped&lt;CloudDatabase&gt;();
/// })
/// .WithExperiments(experiments => experiments
///     .Trial&lt;IMyDatabase&gt;(trial => trial
///         .UsingTest()
///         .AddControl&lt;MyDatabase&gt;()
///         .AddCondition&lt;CloudDatabase&gt;("true")))
/// .Build();
/// 
/// using var scope = ExperimentTestScope.Begin()
///     .ForceCondition&lt;IMyDatabase&gt;("true");
/// 
/// var db = host.Services.GetRequiredService&lt;IMyDatabase&gt;();
/// await db.PingAsync();
/// 
/// Assert.True(host.Trace.ExpectRouted&lt;IMyDatabase&gt;("true"));
/// </code>
/// </example>
public sealed class ExperimentTestHost
{
    private ExperimentTestHost()
    {
    }

    /// <summary>
    /// Gets the configured service provider.
    /// </summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Gets the trace assertions helper for verifying experiment behavior.
    /// </summary>
    public ExperimentTraceAssertions Trace { get; private set; } = null!;

    /// <summary>
    /// Gets the raw event sink for advanced scenarios.
    /// </summary>
    public InMemoryExperimentEventSink EventSink { get; private set; } = null!;

    /// <summary>
    /// Creates a new test host builder.
    /// </summary>
    /// <param name="configureServices">Action to configure services.</param>
    /// <returns>A new test host builder.</returns>
    public static ExperimentTestHostBuilder Create(Action<IServiceCollection> configureServices)
    {
        return new ExperimentTestHostBuilder(configureServices);
    }

    /// <summary>
    /// Builder for configuring the experiment test host.
    /// </summary>
    public sealed class ExperimentTestHostBuilder
    {
        private readonly Action<IServiceCollection> _configureServices;
        private Action<ExperimentFrameworkBuilder>? _configureExperiments;

        internal ExperimentTestHostBuilder(Action<IServiceCollection> configureServices)
        {
            _configureServices = configureServices ?? throw new ArgumentNullException(nameof(configureServices));
        }

        /// <summary>
        /// Configures experiments for the test host.
        /// </summary>
        /// <param name="configureExperiments">Action to configure experiments.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public ExperimentTestHostBuilder WithExperiments(Action<ExperimentFrameworkBuilder> configureExperiments)
        {
            _configureExperiments = configureExperiments ?? throw new ArgumentNullException(nameof(configureExperiments));
            return this;
        }

        /// <summary>
        /// Builds the test host.
        /// </summary>
        /// <returns>The configured test host.</returns>
        public ExperimentTestHost Build()
        {
            var services = new ServiceCollection();

            // Configure user services
            _configureServices(services);

            // Create event sink and trace
            var eventSink = new InMemoryExperimentEventSink();
            var trace = new ExperimentTraceAssertions(eventSink);

            // Register event sink as singleton
            services.AddSingleton(eventSink);
            services.AddSingleton(trace);

            // Register test selection provider factory
            services.AddSingleton<ISelectionModeProviderFactory, TestSelectionProviderFactory>();

            // Configure experiments if provided
            if (_configureExperiments != null)
            {
                var experimentBuilder = ExperimentFrameworkBuilder.Create();

                // Add trace capturing decorator
                experimentBuilder.AddDecoratorFactory(new TraceCapturingDecoratorFactory(eventSink));

                // Apply user experiment configuration
                _configureExperiments(experimentBuilder);

                // Register with DI
                services.AddExperimentFramework(experimentBuilder);
            }

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            return new ExperimentTestHost
            {
                Services = serviceProvider,
                Trace = trace,
                EventSink = eventSink
            };
        }
    }
}
