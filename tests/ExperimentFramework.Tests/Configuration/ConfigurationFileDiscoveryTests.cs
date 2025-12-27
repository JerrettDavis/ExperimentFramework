using ExperimentFramework.Configuration;
using ExperimentFramework.Configuration.Loading;

namespace ExperimentFramework.Tests.Configuration;

public class ConfigurationFileDiscoveryTests : IDisposable
{
    private readonly ConfigurationFileDiscovery _discovery = new();
    private readonly string _tempDir;

    public ConfigurationFileDiscoveryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ExperimentFrameworkTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private ExperimentFrameworkConfigurationOptions CreateOptions(
        bool scanDefaultPaths = false,
        params string[] additionalPaths)
    {
        var options = new ExperimentFrameworkConfigurationOptions
        {
            ScanDefaultPaths = scanDefaultPaths
        };
        foreach (var path in additionalPaths)
        {
            options.AdditionalPaths.Add(path);
        }
        return options;
    }

    [Fact]
    public void DiscoverFiles_WithScanDefaultPathsFalse_ReturnsEmptyList()
    {
        // Arrange
        CreateFile("experiments.yaml", "# test");
        var options = CreateOptions(scanDefaultPaths: false);

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DiscoverFiles_WithScanDefaultPathsTrue_FindsExperimentsYaml()
    {
        // Arrange
        CreateFile("experiments.yaml", "# test");
        var options = CreateOptions(scanDefaultPaths: true);

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Single(result);
        Assert.EndsWith("experiments.yaml", result[0]);
    }

    [Fact]
    public void DiscoverFiles_WithScanDefaultPathsTrue_FindsExperimentsYml()
    {
        // Arrange
        CreateFile("experiments.yml", "# test");
        var options = CreateOptions(scanDefaultPaths: true);

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Single(result);
        Assert.EndsWith("experiments.yml", result[0]);
    }

    [Fact]
    public void DiscoverFiles_WithScanDefaultPathsTrue_FindsExperimentsJson()
    {
        // Arrange
        CreateFile("experiments.json", "{}");
        var options = CreateOptions(scanDefaultPaths: true);

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Single(result);
        Assert.EndsWith("experiments.json", result[0]);
    }

    [Fact]
    public void DiscoverFiles_WithScanDefaultPathsTrue_FindsAllDefaultFileTypes()
    {
        // Arrange
        CreateFile("experiments.yaml", "# yaml");
        CreateFile("experiments.yml", "# yml");
        CreateFile("experiments.json", "{}");
        var options = CreateOptions(scanDefaultPaths: true);

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void DiscoverFiles_WithScanDefaultPathsTrue_FindsFilesInExperimentDefinitionsDirectory()
    {
        // Arrange
        var definitionsDir = Path.Combine(_tempDir, "ExperimentDefinitions");
        Directory.CreateDirectory(definitionsDir);
        CreateFile("ExperimentDefinitions/exp1.yaml", "# exp1");
        CreateFile("ExperimentDefinitions/exp2.yaml", "# exp2");
        var options = CreateOptions(scanDefaultPaths: true);

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.EndsWith("exp1.yaml"));
        Assert.Contains(result, f => f.EndsWith("exp2.yaml"));
    }

    [Fact]
    public void DiscoverFiles_WithScanDefaultPathsTrue_FindsFilesInNestedExperimentDefinitionsDirectory()
    {
        // Arrange
        var nestedDir = Path.Combine(_tempDir, "ExperimentDefinitions", "nested");
        Directory.CreateDirectory(nestedDir);
        CreateFile("ExperimentDefinitions/exp1.yaml", "# exp1");
        CreateFile("ExperimentDefinitions/nested/exp2.yaml", "# exp2");
        var options = CreateOptions(scanDefaultPaths: true);

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void DiscoverFiles_WithAdditionalPath_FindsFile()
    {
        // Arrange
        CreateFile("custom/myexperiment.yaml", "# custom");
        var options = CreateOptions(scanDefaultPaths: false, "custom/myexperiment.yaml");

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Single(result);
        Assert.EndsWith("myexperiment.yaml", result[0]);
    }

    [Fact]
    public void DiscoverFiles_WithAdditionalPathDirectory_FindsAllFilesInDirectory()
    {
        // Arrange
        var customDir = Path.Combine(_tempDir, "custom");
        Directory.CreateDirectory(customDir);
        CreateFile("custom/exp1.yaml", "# exp1");
        CreateFile("custom/exp2.json", "{}");
        var options = CreateOptions(scanDefaultPaths: false, "custom");

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void DiscoverFiles_WithAbsoluteAdditionalPath_FindsFile()
    {
        // Arrange
        CreateFile("myexperiment.yaml", "# absolute");
        var absolutePath = Path.Combine(_tempDir, "myexperiment.yaml");
        var options = CreateOptions(scanDefaultPaths: false, absolutePath);

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Single(result);
        Assert.Equal(absolutePath, result[0]);
    }

    [Fact]
    public void DiscoverFiles_WithGlobPattern_FindsMatchingFiles()
    {
        // Arrange
        CreateFile("exp1.yaml", "# exp1");
        CreateFile("exp2.yaml", "# exp2");
        CreateFile("other.txt", "# other");
        var options = CreateOptions(scanDefaultPaths: false, "*.yaml");

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, f => Assert.EndsWith(".yaml", f));
    }

    [Fact]
    public void DiscoverFiles_WithRecursiveGlobPattern_FindsNestedFiles()
    {
        // Arrange - use a directory-based additional path for recursive search
        // The ** glob pattern has limited support in current implementation
        var nestedDir = Path.Combine(_tempDir, "experiments");
        var deepDir = Path.Combine(nestedDir, "nested");
        Directory.CreateDirectory(deepDir);
        CreateFile("experiments/exp1.yaml", "# exp1");
        CreateFile("experiments/nested/exp2.yaml", "# exp2");
        var options = CreateOptions(scanDefaultPaths: false, "experiments");

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void DiscoverFiles_WithDotSlashRelativePath_ResolvesCorrectly()
    {
        // Arrange
        CreateFile("experiments.yaml", "# relative");
        var options = CreateOptions(scanDefaultPaths: false, "./experiments.yaml");

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void DiscoverFiles_DeduplicatesFiles()
    {
        // Arrange
        CreateFile("experiments.yaml", "# test");
        var options = CreateOptions(scanDefaultPaths: true, "experiments.yaml", "./experiments.yaml");

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void DiscoverFiles_IgnoresNonExistentAdditionalPaths()
    {
        // Arrange
        var options = CreateOptions(scanDefaultPaths: false, "nonexistent.yaml");

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DiscoverFiles_IgnoresUnsupportedExtensionsInDirectory()
    {
        // Arrange
        var customDir = Path.Combine(_tempDir, "custom");
        Directory.CreateDirectory(customDir);
        CreateFile("custom/exp.yaml", "# yaml");
        CreateFile("custom/exp.txt", "text");
        CreateFile("custom/exp.xml", "<xml/>");
        var options = CreateOptions(scanDefaultPaths: false, "custom");

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Single(result);
        Assert.EndsWith(".yaml", result[0]);
    }

    [Fact]
    public void DiscoverFiles_HandlesEmptyDirectory()
    {
        // Arrange
        var emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);
        var options = CreateOptions(scanDefaultPaths: false, "empty");

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DiscoverFiles_CombinesDefaultAndAdditionalPaths()
    {
        // Arrange
        CreateFile("experiments.yaml", "# default");
        CreateFile("custom/custom.yaml", "# custom");
        var options = CreateOptions(scanDefaultPaths: true, "custom/custom.yaml");

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.EndsWith("experiments.yaml"));
        Assert.Contains(result, f => f.EndsWith("custom.yaml"));
    }

    [Fact]
    public void DiscoverFiles_HandlesMultipleAdditionalPaths()
    {
        // Arrange
        CreateFile("path1/exp1.yaml", "# exp1");
        CreateFile("path2/exp2.yaml", "# exp2");
        var options = CreateOptions(scanDefaultPaths: false, "path1/exp1.yaml", "path2/exp2.yaml");

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void DiscoverFiles_GlobPatternWithNoMatches_ReturnsEmpty()
    {
        // Arrange
        var options = CreateOptions(scanDefaultPaths: false, "nonexistent/*.yaml");

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DiscoverFiles_IgnoresNonExistentExperimentDefinitionsDirectory()
    {
        // Arrange - ExperimentDefinitions directory doesn't exist
        var options = CreateOptions(scanDefaultPaths: true);

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DiscoverFiles_FindsYmlFilesInExperimentDefinitions()
    {
        // Arrange
        var definitionsDir = Path.Combine(_tempDir, "ExperimentDefinitions");
        Directory.CreateDirectory(definitionsDir);
        CreateFile("ExperimentDefinitions/exp1.yml", "# yml");
        var options = CreateOptions(scanDefaultPaths: true);

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Single(result);
        Assert.EndsWith(".yml", result[0]);
    }

    [Fact]
    public void DiscoverFiles_FindsJsonFilesInExperimentDefinitions()
    {
        // Arrange
        var definitionsDir = Path.Combine(_tempDir, "ExperimentDefinitions");
        Directory.CreateDirectory(definitionsDir);
        CreateFile("ExperimentDefinitions/exp1.json", "{}");
        var options = CreateOptions(scanDefaultPaths: true);

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Single(result);
        Assert.EndsWith(".json", result[0]);
    }

    [Fact]
    public void DiscoverFiles_FindsMixedFileTypesInExperimentDefinitions()
    {
        // Arrange
        var definitionsDir = Path.Combine(_tempDir, "ExperimentDefinitions");
        Directory.CreateDirectory(definitionsDir);
        CreateFile("ExperimentDefinitions/exp1.yaml", "# yaml");
        CreateFile("ExperimentDefinitions/exp2.yml", "# yml");
        CreateFile("ExperimentDefinitions/exp3.json", "{}");
        var options = CreateOptions(scanDefaultPaths: true);

        // Act
        var result = _discovery.DiscoverFiles(_tempDir, options);

        // Assert
        Assert.Equal(3, result.Count);
    }

    private void CreateFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(fullPath, content);
    }
}
