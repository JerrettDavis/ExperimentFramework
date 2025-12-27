namespace ExperimentFramework.Configuration.Loading;

/// <summary>
/// Discovers configuration files from default and custom paths.
/// </summary>
public sealed class ConfigurationFileDiscovery
{
    private static readonly string[] DefaultFileNames =
    [
        "experiments.yaml",
        "experiments.yml",
        "experiments.json"
    ];

    private static readonly string DefaultDirectoryName = "ExperimentDefinitions";

    private static readonly string[] SupportedExtensions = [".yaml", ".yml", ".json"];

    /// <summary>
    /// Discovers all configuration files based on options.
    /// </summary>
    /// <param name="basePath">The base path to search from.</param>
    /// <param name="options">Loading options.</param>
    /// <returns>List of discovered file paths.</returns>
    public IReadOnlyList<string> DiscoverFiles(
        string basePath,
        ExperimentFrameworkConfigurationOptions options)
    {
        var files = new List<string>();

        if (options.ScanDefaultPaths)
        {
            // Check for default file names in base path
            foreach (var fileName in DefaultFileNames)
            {
                var filePath = Path.Combine(basePath, fileName);
                if (File.Exists(filePath))
                {
                    files.Add(filePath);
                }
            }

            // Check for ExperimentDefinitions directory
            var definitionsDir = Path.Combine(basePath, DefaultDirectoryName);
            if (Directory.Exists(definitionsDir))
            {
                files.AddRange(DiscoverFilesInDirectory(definitionsDir));
            }
        }

        // Add custom paths
        foreach (var customPath in options.AdditionalPaths)
        {
            var resolvedPath = ResolvePath(basePath, customPath);

            if (Directory.Exists(resolvedPath))
            {
                files.AddRange(DiscoverFilesInDirectory(resolvedPath));
            }
            else if (File.Exists(resolvedPath))
            {
                files.Add(resolvedPath);
            }
            else if (customPath.Contains('*'))
            {
                // Handle glob patterns
                files.AddRange(ExpandGlobPattern(basePath, customPath));
            }
        }

        return files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IEnumerable<string> DiscoverFilesInDirectory(string directoryPath)
    {
        foreach (var extension in SupportedExtensions)
        {
            foreach (var file in Directory.GetFiles(directoryPath, $"*{extension}", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }
    }

    private static string ResolvePath(string basePath, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        // Handle relative paths
        if (path.StartsWith("./") || path.StartsWith(".\\"))
        {
            return Path.GetFullPath(Path.Combine(basePath, path[2..]));
        }

        return Path.GetFullPath(Path.Combine(basePath, path));
    }

    private static IEnumerable<string> ExpandGlobPattern(string basePath, string pattern)
    {
        // Simple glob pattern support for *.yaml, **/*.yaml, etc.
        var resolvedPattern = ResolvePath(basePath, pattern);
        var directory = Path.GetDirectoryName(resolvedPattern) ?? basePath;
        var filePattern = Path.GetFileName(resolvedPattern);

        if (!Directory.Exists(directory))
        {
            yield break;
        }

        var searchOption = pattern.Contains("**")
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        // Replace ** with * for Directory.GetFiles
        var normalizedPattern = filePattern.Replace("**", "*");

        foreach (var file in Directory.GetFiles(directory, normalizedPattern, searchOption))
        {
            var extension = Path.GetExtension(file);
            if (SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }
    }
}
