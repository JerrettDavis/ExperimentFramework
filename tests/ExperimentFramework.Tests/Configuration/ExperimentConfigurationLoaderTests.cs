using ExperimentFramework.Configuration;
using ExperimentFramework.Configuration.Exceptions;
using ExperimentFramework.Configuration.Loading;
using Microsoft.Extensions.Configuration;

namespace ExperimentFramework.Tests.Configuration;

public class ExperimentConfigurationLoaderTests
{
    private readonly ExperimentConfigurationLoader _loader = new();

    [Fact]
    public void Load_WithEmptyConfiguration_ReturnsEmptyRoot()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var options = new ExperimentFrameworkConfigurationOptions
        {
            ScanDefaultPaths = false // Don't scan for files
        };

        // Act
        var result = _loader.Load(configuration, options);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Trials ?? []);
        Assert.Empty(result.Experiments ?? []);
    }

    [Fact]
    public void Load_WithConfigurationSection_BindsSettings()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["ExperimentFramework:Settings:ProxyStrategy"] = "dispatchProxy",
            ["ExperimentFramework:Settings:NamingConvention"] = "custom"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var options = new ExperimentFrameworkConfigurationOptions
        {
            ScanDefaultPaths = false
        };

        // Act
        var result = _loader.Load(configuration, options);

        // Assert
        Assert.NotNull(result.Settings);
        Assert.Equal("dispatchProxy", result.Settings.ProxyStrategy);
        Assert.Equal("custom", result.Settings.NamingConvention);
    }

    [Fact]
    public void Load_WithCustomSectionName_BindsCorrectly()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["MyExperiments:Settings:ProxyStrategy"] = "sourceGenerators"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var options = new ExperimentFrameworkConfigurationOptions
        {
            ConfigurationSectionName = "MyExperiments",
            ScanDefaultPaths = false
        };

        // Act
        var result = _loader.Load(configuration, options);

        // Assert
        Assert.NotNull(result.Settings);
        Assert.Equal("sourceGenerators", result.Settings.ProxyStrategy);
    }

    [Fact]
    public void LoadFromFile_NonExistentFile_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ConfigurationLoadException>(() =>
            _loader.LoadFromFile("nonexistent-file.yaml"));
    }

    [Fact]
    public void LoadFromFile_UnsupportedExtension_ThrowsException()
    {
        // Arrange - create a temp file with unsupported extension
        var tempFile = Path.GetTempFileName() + ".txt";
        File.WriteAllText(tempFile, "test content");

        try
        {
            // Act & Assert
            Assert.Throws<ConfigurationLoadException>(() =>
                _loader.LoadFromFile(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadFromFile_ValidYaml_ParsesCorrectly()
    {
        // Arrange
        var yaml = """
            experimentFramework:
              settings:
                proxyStrategy: dispatchProxy
              trials:
                - serviceType: ITestService
                  selectionMode:
                    type: featureFlag
                    flagName: TestFlag
                  control:
                    key: control
                    implementationType: TestService
            """;

        var tempFile = Path.GetTempFileName();
        var yamlFile = Path.ChangeExtension(tempFile, ".yaml");
        File.Move(tempFile, yamlFile);
        File.WriteAllText(yamlFile, yaml);

        try
        {
            // Act
            var result = _loader.LoadFromFile(yamlFile);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Settings);
            Assert.Equal("dispatchProxy", result.Settings.ProxyStrategy);
            Assert.NotNull(result.Trials);
            Assert.Single(result.Trials);
            Assert.Equal("ITestService", result.Trials[0].ServiceType);
        }
        finally
        {
            File.Delete(yamlFile);
        }
    }

    [Fact]
    public void LoadFromFile_ValidJson_ParsesCorrectly()
    {
        // Arrange
        var json = """
            {
              "experimentFramework": {
                "settings": {
                  "proxyStrategy": "sourceGenerators"
                },
                "trials": [
                  {
                    "serviceType": "ITestService",
                    "selectionMode": {
                      "type": "configurationKey",
                      "key": "TestKey"
                    },
                    "control": {
                      "key": "default",
                      "implementationType": "DefaultService"
                    }
                  }
                ]
              }
            }
            """;

        var tempFile = Path.GetTempFileName();
        var jsonFile = Path.ChangeExtension(tempFile, ".json");
        File.Move(tempFile, jsonFile);
        File.WriteAllText(jsonFile, json);

        try
        {
            // Act
            var result = _loader.LoadFromFile(jsonFile);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Settings);
            Assert.Equal("sourceGenerators", result.Settings.ProxyStrategy);
            Assert.NotNull(result.Trials);
            Assert.Single(result.Trials);
            Assert.Equal("configurationKey", result.Trials[0].SelectionMode.Type);
        }
        finally
        {
            File.Delete(jsonFile);
        }
    }

    [Fact]
    public void LoadFromFile_YamlWithExperiment_ParsesCorrectly()
    {
        // Arrange
        var yaml = """
            experimentFramework:
              experiments:
                - name: test-experiment
                  metadata:
                    owner: test-team
                  trials:
                    - serviceType: IService
                      selectionMode:
                        type: featureFlag
                      control:
                        key: control
                        implementationType: Service
            """;

        var tempFile = Path.GetTempFileName();
        var yamlFile = Path.ChangeExtension(tempFile, ".yaml");
        File.Move(tempFile, yamlFile);
        File.WriteAllText(yamlFile, yaml);

        try
        {
            // Act
            var result = _loader.LoadFromFile(yamlFile);

            // Assert
            Assert.NotNull(result.Experiments);
            Assert.Single(result.Experiments);
            Assert.Equal("test-experiment", result.Experiments[0].Name);
            Assert.NotNull(result.Experiments[0].Metadata);
            Assert.Equal("test-team", result.Experiments[0].Metadata!["owner"]);
        }
        finally
        {
            File.Delete(yamlFile);
        }
    }
}
