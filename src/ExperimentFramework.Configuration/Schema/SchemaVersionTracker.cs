using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExperimentFramework.Configuration.Schema;

/// <summary>
/// Manages schema version history and determines when version increments are needed based on hash changes.
/// </summary>
public sealed class SchemaVersionTracker
{
    private readonly string _historyFilePath;
    private SchemaVersionHistory _history = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaVersionTracker"/> class.
    /// </summary>
    /// <param name="historyFilePath">Path to the schema version history file.</param>
    public SchemaVersionTracker(string historyFilePath)
    {
        _historyFilePath = historyFilePath ?? throw new ArgumentNullException(nameof(historyFilePath));
        LoadHistory();
    }

    /// <summary>
    /// Determines the appropriate version for a schema based on its hash.
    /// If the hash has changed, the version is incremented.
    /// </summary>
    /// <param name="extensionName">Name of the extension/module.</param>
    /// <param name="currentHash">The current hash of the schema.</param>
    /// <returns>The appropriate semantic version string.</returns>
    public string GetVersionForHash(string extensionName, string currentHash)
    {
        if (!_history.Extensions.TryGetValue(extensionName, out var extensionHistory))
        {
            // First time seeing this extension
            extensionHistory = new ExtensionVersionHistory
            {
                ExtensionName = extensionName,
                CurrentVersion = "1.0.0",
                CurrentHash = currentHash
            };
            _history.Extensions[extensionName] = extensionHistory;
            return "1.0.0";
        }

        // Check if hash has changed
        if (extensionHistory.CurrentHash == currentHash)
        {
            // No change, return current version
            return extensionHistory.CurrentVersion;
        }

        // Hash changed, increment version
        var newVersion = IncrementVersion(extensionHistory.CurrentVersion);
        
        // Record the change
        extensionHistory.VersionHistory.Add(new VersionHistoryEntry
        {
            Version = extensionHistory.CurrentVersion,
            Hash = extensionHistory.CurrentHash,
            Timestamp = DateTimeOffset.UtcNow
        });

        extensionHistory.CurrentVersion = newVersion;
        extensionHistory.CurrentHash = currentHash;

        return newVersion;
    }

    /// <summary>
    /// Saves the current version history to disk.
    /// </summary>
    public void SaveHistory()
    {
        var directory = Path.GetDirectoryName(_historyFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        File.WriteAllText(_historyFilePath, json);
    }

    /// <summary>
    /// Gets the complete version history.
    /// </summary>
    public SchemaVersionHistory GetHistory() => _history;

    private void LoadHistory()
    {
        if (!File.Exists(_historyFilePath))
        {
            _history = new SchemaVersionHistory();
            return;
        }

        try
        {
            var json = File.ReadAllText(_historyFilePath);
            _history = JsonSerializer.Deserialize<SchemaVersionHistory>(json) ?? new SchemaVersionHistory();
        }
        catch
        {
            // If we can't read the history, start fresh
            _history = new SchemaVersionHistory();
        }
    }

    private static string IncrementVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length != 3)
        {
            return "1.0.0";
        }

        if (!int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
        {
            return "1.0.0";
        }

        // Increment patch version by default
        // Future enhancement: Could analyze schema changes to determine major/minor/patch
        patch++;

        return $"{major}.{minor}.{patch}";
    }
}

/// <summary>
/// Represents the complete version history for all schema extensions.
/// </summary>
public sealed class SchemaVersionHistory
{
    /// <summary>
    /// Gets or sets the version history for each extension, keyed by extension name.
    /// </summary>
    public Dictionary<string, ExtensionVersionHistory> Extensions { get; set; } = new();
}

/// <summary>
/// Represents the version history for a single extension.
/// </summary>
public sealed class ExtensionVersionHistory
{
    /// <summary>
    /// Gets or sets the name of the extension.
    /// </summary>
    public string ExtensionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current version of the schema.
    /// </summary>
    public string CurrentVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the current hash of the schema.
    /// </summary>
    public string CurrentHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the historical versions and their hashes.
    /// </summary>
    public List<VersionHistoryEntry> VersionHistory { get; set; } = new();
}

/// <summary>
/// Represents a single entry in the version history.
/// </summary>
public sealed class VersionHistoryEntry
{
    /// <summary>
    /// Gets or sets the version string.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema hash for this version.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this version was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
