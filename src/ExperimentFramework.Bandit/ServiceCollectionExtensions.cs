using ExperimentFramework.Bandit.Algorithms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.Bandit;

/// <summary>
/// Extension methods for registering bandit experiment services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds multi-armed bandit support with epsilon-greedy algorithm.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="epsilon">The exploration probability (0-1).</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExperimentBanditEpsilonGreedy(
        this IServiceCollection services,
        double epsilon = 0.1)
    {
        services.TryAddSingleton<IBanditAlgorithm>(new EpsilonGreedy(epsilon));
        return services;
    }

    /// <summary>
    /// Adds multi-armed bandit support with Thompson Sampling algorithm.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExperimentBanditThompsonSampling(this IServiceCollection services)
    {
        services.TryAddSingleton<IBanditAlgorithm, ThompsonSampling>();
        return services;
    }

    /// <summary>
    /// Adds multi-armed bandit support with UCB1 algorithm.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="explorationParameter">The exploration parameter.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExperimentBanditUcb(
        this IServiceCollection services,
        double explorationParameter = 1.41421356)
    {
        services.TryAddSingleton<IBanditAlgorithm>(new UpperConfidenceBound(explorationParameter));
        return services;
    }

    /// <summary>
    /// Adds a custom bandit algorithm.
    /// </summary>
    /// <typeparam name="TAlgorithm">The algorithm type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExperimentBandit<TAlgorithm>(this IServiceCollection services)
        where TAlgorithm : class, IBanditAlgorithm
    {
        services.TryAddSingleton<IBanditAlgorithm, TAlgorithm>();
        return services;
    }
}
