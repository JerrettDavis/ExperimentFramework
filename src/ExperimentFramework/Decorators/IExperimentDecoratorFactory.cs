namespace ExperimentFramework.Decorators;

/// <summary>
/// Defines a factory responsible for creating an <see cref="IExperimentDecorator"/> for a given <see cref="IServiceProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// Factories are used so decorators can be created with access to scoped services at the time the pipeline is composed.
/// </para>
/// <para>
/// Implementations should avoid caching the <see cref="IServiceProvider"/> beyond the scope they were created in.
/// </para>
/// </remarks>
public interface IExperimentDecoratorFactory
{
    /// <summary>
    /// Creates a new <see cref="IExperimentDecorator"/> instance using the provided service provider.
    /// </summary>
    /// <param name="sp">The service provider used to resolve dependencies required by the decorator.</param>
    /// <returns>A new decorator instance.</returns>
    IExperimentDecorator Create(IServiceProvider sp);
}