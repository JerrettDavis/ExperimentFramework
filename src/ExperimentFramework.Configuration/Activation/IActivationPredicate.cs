namespace ExperimentFramework.Configuration.Activation;

/// <summary>
/// Interface for custom activation predicates that can be referenced in configuration.
/// Implement this interface to create reusable activation conditions that can be
/// referenced by type name in YAML/JSON configuration.
/// </summary>
/// <example>
/// <code>
/// public class ProductionOnlyPredicate : IActivationPredicate
/// {
///     public bool IsActive(IServiceProvider serviceProvider)
///     {
///         var env = serviceProvider.GetRequiredService&lt;IHostEnvironment&gt;();
///         return env.IsProduction();
///     }
/// }
/// </code>
///
/// In YAML:
/// <code>
/// activation:
///   predicate:
///     type: "MyApp.ProductionOnlyPredicate, MyApp"
/// </code>
/// </example>
public interface IActivationPredicate
{
    /// <summary>
    /// Determines if the experiment/trial should be active.
    /// </summary>
    /// <param name="serviceProvider">The service provider for accessing registered services.</param>
    /// <returns>True if the experiment/trial should be active; otherwise false.</returns>
    bool IsActive(IServiceProvider serviceProvider);
}
