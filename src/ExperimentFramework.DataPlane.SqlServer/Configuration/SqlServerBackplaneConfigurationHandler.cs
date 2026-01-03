using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.DataPlane.SqlServer.Configuration;

public sealed class SqlServerBackplaneConfigurationHandler : IConfigurationBackplaneHandler
{
    public string BackplaneType => "sqlServer";

    public void ConfigureServices(IServiceCollection services, DataPlaneBackplaneConfig config, ILogger? logger)
    {
        logger?.LogInformation("Configuring SQL Server data backplane from configuration");

        var options = new SqlServerDataBackplaneOptions
        {
            ConnectionString = string.Empty
        };

        // Extract connection string
        if (config.Options?.TryGetValue("connectionString", out var connStrObj) == true && connStrObj != null)
        {
            options.ConnectionString = connStrObj.ToString() ?? string.Empty;
        }

        // Extract schema
        if (config.Options?.TryGetValue("schema", out var schemaObj) == true && schemaObj != null)
        {
            options.Schema = schemaObj.ToString() ?? "dbo";
        }

        // Extract table name
        if (config.Options?.TryGetValue("tableName", out var tableObj) == true && tableObj != null)
        {
            options.TableName = tableObj.ToString() ?? "ExperimentEvents";
        }

        // Extract batch size
        if (config.Options?.TryGetValue("batchSize", out var batchSizeObj) == true && batchSizeObj != null)
        {
            if (int.TryParse(batchSizeObj.ToString(), out var batchSize))
            {
                options.BatchSize = batchSize;
            }
        }

        // Extract enable idempotency
        if (config.Options?.TryGetValue("enableIdempotency", out var idempotencyObj) == true && idempotencyObj != null)
        {
            if (bool.TryParse(idempotencyObj.ToString(), out var enableIdempotency))
            {
                options.EnableIdempotency = enableIdempotency;
            }
        }

        // Extract auto migrate
        if (config.Options?.TryGetValue("autoMigrate", out var migrateObj) == true && migrateObj != null)
        {
            if (bool.TryParse(migrateObj.ToString(), out var autoMigrate))
            {
                options.AutoMigrate = autoMigrate;
            }
        }

        services.AddSqlServerDataBackplane(options);

        logger?.LogInformation("SQL Server data backplane configured");
    }

    public IEnumerable<ConfigurationValidationError> Validate(DataPlaneBackplaneConfig config, string path)
    {
        var errors = new List<ConfigurationValidationError>();

        // Validate connection string
        if (config.Options == null || !config.Options.ContainsKey("connectionString"))
        {
            errors.Add(new ConfigurationValidationError(
                path,
                "SQL Server backplane requires 'connectionString' configuration",
                ValidationSeverity.Error));
        }

        return errors;
    }
}
