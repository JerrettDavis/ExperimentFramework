namespace ExperimentFramework.DataPlane.SqlServer;

/// <summary>
/// Configuration options for the SQL Server data backplane.
/// </summary>
public sealed class SqlServerDataBackplaneOptions
{
    /// <summary>
    /// Gets or sets the SQL Server connection string.
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the schema name for experiment tables.
    /// </summary>
    public string Schema { get; set; } = "dbo";

    /// <summary>
    /// Gets or sets the table name for storing events.
    /// </summary>
    public string TableName { get; set; } = "ExperimentEvents";

    /// <summary>
    /// Gets or sets the batch size for bulk inserts.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to enable idempotency checks using event IDs.
    /// </summary>
    public bool EnableIdempotency { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to automatically apply migrations on startup.
    /// </summary>
    public bool AutoMigrate { get; set; } = false;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}
