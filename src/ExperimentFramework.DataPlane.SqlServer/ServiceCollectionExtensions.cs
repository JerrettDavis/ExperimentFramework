using ExperimentFramework.DataPlane.Abstractions;
using ExperimentFramework.DataPlane.SqlServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.DataPlane.SqlServer;

/// <summary>
/// Extension methods for registering SQL Server data backplane services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a SQL Server data backplane with the specified configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for SQL Server options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqlServerDataBackplane(
        this IServiceCollection services,
        Action<SqlServerDataBackplaneOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        services.Configure(configure);

        var options = new SqlServerDataBackplaneOptions { ConnectionString = string.Empty };
        configure(options);

        services.AddDbContext<ExperimentDataContext>(dbOptions =>
        {
            dbOptions.UseSqlServer(options.ConnectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(options.CommandTimeoutSeconds);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        services.TryAddSingleton<IDataBackplane, SqlServerDataBackplane>();

        return services;
    }

    /// <summary>
    /// Adds a SQL Server data backplane with options from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">SQL Server backplane options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqlServerDataBackplane(
        this IServiceCollection services,
        SqlServerDataBackplaneOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        services.Configure<SqlServerDataBackplaneOptions>(opts =>
        {
            opts.ConnectionString = options.ConnectionString;
            opts.Schema = options.Schema;
            opts.TableName = options.TableName;
            opts.BatchSize = options.BatchSize;
            opts.EnableIdempotency = options.EnableIdempotency;
            opts.AutoMigrate = options.AutoMigrate;
            opts.CommandTimeoutSeconds = options.CommandTimeoutSeconds;
        });

        services.AddDbContext<ExperimentDataContext>(dbOptions =>
        {
            dbOptions.UseSqlServer(options.ConnectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(options.CommandTimeoutSeconds);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        services.TryAddSingleton<IDataBackplane, SqlServerDataBackplane>();

        return services;
    }
}
