using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.Governance.Persistence.Sql;

/// <summary>
/// Extension methods for registering SQL governance persistence.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQL-based governance persistence with the specified connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqlGovernancePersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<GovernanceDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.TryAddScoped<IGovernancePersistenceBackplane, SqlGovernancePersistenceBackplane>();

        return services;
    }

    /// <summary>
    /// Adds SQL-based governance persistence with custom DbContext configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDbContext">Action to configure the DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqlGovernancePersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        services.AddDbContext<GovernanceDbContext>(configureDbContext);

        services.TryAddScoped<IGovernancePersistenceBackplane, SqlGovernancePersistenceBackplane>();

        return services;
    }
}
