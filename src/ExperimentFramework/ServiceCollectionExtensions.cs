using System.Reflection;
using ExperimentFramework.Models;
using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using ExperimentFramework.Selection.Providers;
using ExperimentFramework.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register the experiment framework.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the experiment framework with the specified configuration.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="builder">The experiment framework builder containing experiment definitions.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method performs the following steps:
    /// </para>
    /// <list type="number">
    /// <item><description>Builds the framework configuration from the provided builder.</description></item>
    /// <item><description>Registers the <see cref="ExperimentRegistry"/> as a singleton.</description></item>
    /// <item><description>For each experiment definition, configures the service interface with either a source-generated proxy or DispatchProxy-based runtime proxy.</description></item>
    /// </list>
    /// <para>
    /// <strong>Requirements:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Service implementations must be registered by their concrete type before calling this method.</description></item>
    /// <item><description>For source generators: The composition root method must either be decorated with <c>[ExperimentCompositionRoot]</c> attribute or call <c>.UseSourceGenerators()</c> on the builder.</description></item>
    /// <item><description>For runtime proxies: Call <c>.UseDispatchProxy()</c> on the builder.</description></item>
    /// </list>
    /// <para>
    /// The framework defaults to compile-time source generators for zero-overhead proxies (&lt;100ns).
    /// Runtime proxies (DispatchProxy) incur ~800ns overhead but provide more flexibility.
    /// All proxies are registered as singletons and create scopes internally per invocation.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddExperimentFramework(
        this IServiceCollection services,
        ExperimentFrameworkBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(builder);

        var config = builder.Build();

        // Register telemetry (default: noop)
        services.TryAddSingleton<IExperimentTelemetry>(NoopExperimentTelemetry.Instance);

        // Register the default naming convention
        services.TryAddSingleton<IExperimentNamingConvention>(DefaultExperimentNamingConvention.Instance);

        // Register the selection mode registry with built-in and external providers
        services.TryAddSingleton<SelectionModeRegistry>(sp =>
        {
            var registry = new SelectionModeRegistry();

            // Register built-in provider factories
            registry.Register(new BooleanFeatureFlagProviderFactory());
            registry.Register(new ConfigurationValueProviderFactory());

            // Auto-register any external provider factories from DI
            foreach (var factory in sp.GetServices<ISelectionModeProviderFactory>())
            {
                registry.Register(factory);
            }

            return registry;
        });

        // Register the experiment registry as singleton
        services.AddSingleton(sp => new ExperimentRegistry(config.Definitions, sp));

        // For each experiment definition, configure the service with a generated proxy
        foreach (var definition in config.Definitions)
        {
            var serviceType = definition.ServiceType;

            // Find existing registration for the service interface
            var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);

            if (existingDescriptor == null)
            {
                throw new InvalidOperationException(
                    $"Service type {serviceType.FullName} must be registered before calling AddExperimentFramework. " +
                    $"Register the service interface (e.g., services.AddScoped<{serviceType.Name}, ConcreteType>()) " +
                    $"before configuring experiments.");
            }

            // Remove the existing interface registration
            services.Remove(existingDescriptor);

            Func<IServiceProvider, object> proxyFactory;

            if (config.UseRuntimeProxies)
            {
                // Use DispatchProxy-based runtime proxy
                proxyFactory = CreateRuntimeProxyFactory(serviceType, config);
            }
            else
            {
                // Find the source-generated proxy (required)
                var generatedProxyType = TryFindGeneratedProxy(serviceType);

                if (generatedProxyType == null)
                {
                    throw new InvalidOperationException(
                        $"No source-generated proxy found for {serviceType.FullName}. " +
                        $"Ensure your composition root method is decorated with [ExperimentCompositionRoot] attribute " +
                        $"or calls .UseSourceGenerators() on the builder, " +
                        $"and the project references ExperimentFramework.Generators as an analyzer. " +
                        $"Alternatively, call .UseDispatchProxy() to use runtime proxies instead.");
                }

                // Use source-generated proxy (compile-time, zero reflection overhead)
                proxyFactory = CreateGeneratedProxyFactory(serviceType, generatedProxyType, config);
            }

            // All proxies are singleton (they create scopes internally)
            services.Add(new ServiceDescriptor(serviceType, proxyFactory, ServiceLifetime.Singleton));
        }

        return services;
    }


    /// <summary>
    /// Attempts to find a source-generated proxy for the given service type.
    /// </summary>
    /// <param name="serviceType">The service interface type.</param>
    /// <returns>The generated proxy type if found; otherwise null.</returns>
    private static Type? TryFindGeneratedProxy(Type serviceType)
    {
        // Generated proxy naming convention:
        // IMyDatabase → MyDatabaseExperimentProxy
        // IGenericRepository`1 → GenericRepositoryExperimentProxy`1
        // Namespace: ExperimentFramework.Generated

        var serviceName = serviceType.Name;

        // Strip 'I' prefix if present and followed by uppercase letter
        if (serviceName.StartsWith("I") && serviceName.Length > 1 && char.IsUpper(serviceName[1]))
        {
            serviceName = serviceName.Substring(1);
        }

        // Build proxy type name
        string baseProxyName;
        if (serviceType.IsGenericType)
        {
            // For generics: GenericRepository`1 -> GenericRepositoryExperimentProxy`1
            var backtickIndex = serviceName.IndexOf('`');
            if (backtickIndex > 0)
            {
                var baseName = serviceName.Substring(0, backtickIndex);
                var arity = serviceName.Substring(backtickIndex); // includes `1
                baseProxyName = $"{baseName}ExperimentProxy{arity}";
            }
            else
            {
                baseProxyName = $"{serviceName}ExperimentProxy";
            }
        }
        else
        {
            baseProxyName = $"{serviceName}ExperimentProxy";
        }

        var proxyTypeName = $"ExperimentFramework.Generated.{baseProxyName}";

        // Search in the calling assembly and service type assembly
        var assembly = serviceType.Assembly;
        var openProxyType = assembly.GetType(proxyTypeName);

        if (openProxyType == null)
            return null;

        // For generic types, we need to close the generic type with the same type arguments
        if (serviceType.IsGenericType && openProxyType.IsGenericTypeDefinition)
        {
            var typeArgs = serviceType.GetGenericArguments();
            return openProxyType.MakeGenericType(typeArgs);
        }

        return openProxyType;
    }

    /// <summary>
    /// Creates a factory for a source-generated proxy.
    /// </summary>
    /// <param name="serviceType">The service interface type.</param>
    /// <param name="proxyType">The generated proxy type.</param>
    /// <param name="config">The experiment framework configuration.</param>
    /// <returns>A factory function that creates the generated proxy.</returns>
    private static Func<IServiceProvider, object> CreateGeneratedProxyFactory(
        Type serviceType,
        Type proxyType,
        ExperimentFrameworkConfiguration config)
    {
        return sp =>
        {
            var registry = sp.GetRequiredService<ExperimentRegistry>();

            if (!registry.TryGet(serviceType, out var registration))
            {
                throw new InvalidOperationException($"No experiment registration found for {serviceType.FullName}");
            }

            var telemetry = sp.GetRequiredService<IExperimentTelemetry>();
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            // Generated proxy constructor: (IServiceScopeFactory, ExperimentRegistration, IExperimentDecoratorFactory[], IExperimentTelemetry)
            var proxy = Activator.CreateInstance(
                proxyType,
                scopeFactory,
                registration,
                config.DecoratorFactories,
                telemetry);

            return proxy ?? throw new InvalidOperationException($"Failed to create generated proxy for {serviceType.FullName}");
        };
    }


    /// <summary>
    /// Creates a factory for a runtime DispatchProxy-based proxy.
    /// </summary>
    /// <param name="serviceType">The service interface type.</param>
    /// <param name="config">The experiment framework configuration.</param>
    /// <returns>A factory function that creates the runtime proxy.</returns>
    private static Func<IServiceProvider, object> CreateRuntimeProxyFactory(
        Type serviceType,
        ExperimentFrameworkConfiguration config)
    {
        return sp =>
        {
            var registry = sp.GetRequiredService<ExperimentRegistry>();

            if (!registry.TryGet(serviceType, out var registration))
            {
                throw new InvalidOperationException($"No experiment registration found for {serviceType.FullName}");
            }

            var telemetry = sp.GetRequiredService<IExperimentTelemetry>();
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            // RuntimeExperimentProxy<TService>.Create(scopeFactory, registration, decoratorFactories, telemetry)
            var proxyType = typeof(RuntimeExperimentProxy<>).MakeGenericType(serviceType);
            var createMethod = proxyType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

            if (createMethod == null)
            {
                throw new InvalidOperationException($"Failed to find Create method on RuntimeExperimentProxy<{serviceType.FullName}>");
            }

            var proxy = createMethod.Invoke(null, [
                scopeFactory,
                registration,
                config.DecoratorFactories,
                telemetry
            ]);

            return proxy ?? throw new InvalidOperationException($"Failed to create runtime proxy for {serviceType.FullName}");
        };
    }


    /// <summary>
    /// Enables OpenTelemetry-based experiment tracing using <see cref="System.Diagnostics.Activity"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers <see cref="OpenTelemetryExperimentTelemetry"/> as the active telemetry provider,
    /// replacing the default no-op implementation.
    /// </para>
    /// <para>
    /// Activities are emitted with the source name <c>"ExperimentFramework"</c> and can be collected
    /// using OpenTelemetry SDK or any <see cref="System.Diagnostics.ActivityListener"/>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddOpenTelemetryExperimentTracking(this IServiceCollection services)
    {
        services.AddSingleton<IExperimentTelemetry, OpenTelemetryExperimentTelemetry>();
        return services;
    }

    /// <summary>
    /// Registers a custom selection mode provider using the generic factory.
    /// </summary>
    /// <typeparam name="TProvider">
    /// The selection mode provider type. Must have a <see cref="SelectionModeAttribute"/>
    /// applied to specify the mode identifier.
    /// </typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method eliminates the need for custom <see cref="ISelectionModeProviderFactory"/>
    /// implementations. The provider is created using <see cref="Microsoft.Extensions.DependencyInjection.ActivatorUtilities"/>
    /// with full dependency injection support.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// [SelectionMode("Redis")]
    /// public class RedisProvider : SelectionModeProviderBase
    /// {
    ///     public RedisProvider(IConnectionMultiplexer redis) { ... }
    /// }
    ///
    /// // Registration:
    /// services.AddSelectionModeProvider&lt;RedisProvider&gt;();
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddSelectionModeProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, ISelectionModeProvider
    {
        services.AddSingleton<ISelectionModeProviderFactory, SelectionModeProviderFactory<TProvider>>();
        return services;
    }

    /// <summary>
    /// Registers a custom selection mode provider with an explicit mode identifier.
    /// </summary>
    /// <typeparam name="TProvider">The selection mode provider type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="modeIdentifier">The mode identifier for this provider.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// Use this overload when the provider type does not have a <see cref="SelectionModeAttribute"/>
    /// or when you want to override the attribute value.
    /// </remarks>
    public static IServiceCollection AddSelectionModeProvider<TProvider>(
        this IServiceCollection services,
        string modeIdentifier)
        where TProvider : class, ISelectionModeProvider
    {
        services.AddSingleton<ISelectionModeProviderFactory>(
            new SelectionModeProviderFactory<TProvider>(modeIdentifier));
        return services;
    }
}
