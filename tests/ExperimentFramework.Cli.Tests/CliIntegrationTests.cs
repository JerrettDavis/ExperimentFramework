using System.CommandLine;
using System.Text.Json;
using ExperimentFramework.Configuration.Models;
using Xunit;

namespace ExperimentFramework.Cli.Tests;

public class CliIntegrationTests
{
    private readonly string _testConfigsPath;

    public CliIntegrationTests()
    {
        _testConfigsPath = Path.Combine(Path.GetTempPath(), "cli-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testConfigsPath);
    }

    [Fact]
    public async Task ConfigValidate_WithValidConfig_ReturnsSuccess()
    {
        // Arrange
        var configPath = CreateValidConfigFile();

        // Act
        var exitCode = await Program.Main(new[] { "config", "validate", configPath });

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigValidate_WithInvalidConfig_ReturnsError()
    {
        // Arrange
        var configPath = CreateInvalidConfigFile();

        // Act
        var exitCode = await Program.Main(new[] { "config", "validate", configPath });

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ConfigValidate_WithNonExistentFile_ReturnsError()
    {
        // Act
        var exitCode = await Program.Main(new[] { "config", "validate", "non-existent-file.json" });

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Doctor_WithValidConfig_ReturnsSuccess()
    {
        // Arrange
        var configPath = CreateValidConfigFile();

        // Act
        var exitCode = await Program.Main(new[] { "doctor", "--config", configPath });

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Doctor_WithInvalidConfig_ReturnsError()
    {
        // Arrange
        var configPath = CreateInvalidConfigFile();

        // Act
        var exitCode = await Program.Main(new[] { "doctor", "--config", configPath });

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task PlanExport_WithValidConfig_ReturnsSuccess()
    {
        // Arrange
        var configPath = CreateValidConfigFile();
        var outputPath = Path.Combine(_testConfigsPath, "plan.txt");

        // Act
        var exitCode = await Program.Main(new[] { "plan", "export", "--config", configPath, "--format", "text", "--out", outputPath });

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("ExperimentFramework Configuration Plan", content);
    }

    [Fact]
    public async Task PlanExport_JsonFormat_ProducesValidJson()
    {
        // Arrange
        var configPath = CreateValidConfigFile();
        var outputPath = Path.Combine(_testConfigsPath, "plan.json");

        // Act
        var exitCode = await Program.Main(new[] { "plan", "export", "--config", configPath, "--format", "json", "--out", outputPath });

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputPath));
        
        var jsonContent = await File.ReadAllTextAsync(outputPath);
        var doc = JsonDocument.Parse(jsonContent); // This will throw if invalid JSON
        
        Assert.True(doc.RootElement.TryGetProperty("generatedAt", out _));
        Assert.True(doc.RootElement.TryGetProperty("trials", out _));
    }

    [Fact]
    public async Task PlanExport_TextFormat_ContainsExpectedSections()
    {
        // Arrange
        var configPath = CreateValidConfigFile();
        var outputPath = Path.Combine(_testConfigsPath, "plan.txt");

        // Act
        var exitCode = await Program.Main(new[] { "plan", "export", "--config", configPath, "--format", "text", "--out", outputPath });

        // Assert
        Assert.Equal(0, exitCode);
        var content = await File.ReadAllTextAsync(outputPath);
        
        Assert.Contains("ExperimentFramework Configuration Plan", content);
        Assert.Contains("Global Decorators", content);
        Assert.Contains("Standalone Trials", content);
    }

    private string CreateValidConfigFile()
    {
        var config = new ExperimentFrameworkConfigurationRoot
        {
            Decorators = new List<DecoratorConfig>
            {
                new() { Type = "logging" },
                new() { Type = "metrics" }
            },
            Trials = new List<TrialConfig>
            {
                new()
                {
                    ServiceType = "ITestService",
                    SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                    Control = new ConditionConfig
                    {
                        Key = "control",
                        ImplementationType = "TestControlService"
                    },
                    Conditions = new List<ConditionConfig>
                    {
                        new() { Key = "variant-a", ImplementationType = "TestVariantAService" }
                    }
                }
            }
        };

        var path = Path.Combine(_testConfigsPath, $"valid-{Guid.NewGuid()}.json");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        return path;
    }

    private string CreateInvalidConfigFile()
    {
        var config = new
        {
            trials = new[]
            {
                new
                {
                    serviceType = "",
                    selectionMode = new { type = "invalid-mode" },
                    control = new { key = "control", implementationType = "" }
                }
            }
        };

        var path = Path.Combine(_testConfigsPath, $"invalid-{Guid.NewGuid()}.json");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        return path;
    }
}
