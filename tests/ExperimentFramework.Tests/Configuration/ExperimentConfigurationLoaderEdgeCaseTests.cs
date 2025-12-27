using ExperimentFramework.Configuration;
using ExperimentFramework.Configuration.Exceptions;
using ExperimentFramework.Configuration.Loading;
using Microsoft.Extensions.Configuration;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration;

[Feature("ExperimentConfigurationLoader loads experiment configurations from files")]
public class ExperimentConfigurationLoaderEdgeCaseTests : TinyBddXunitBase, IDisposable
{
    private readonly string _tempDir;

    public ExperimentConfigurationLoaderEdgeCaseTests(ITestOutputHelper output) : base(output)
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LoaderEdgeCaseTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public new void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region State Records

    private sealed record FileState(string FilePath, ExperimentConfigurationLoader Loader);
    private sealed record LoadState(IConfiguration Configuration, ExperimentFrameworkConfigurationOptions Options, ExperimentConfigurationLoader Loader);

    #endregion

    #region Helper Methods

    private FileState CreateYamlFile(string yaml, string fileName = "config.yaml")
    {
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, yaml);
        return new FileState(filePath, new ExperimentConfigurationLoader());
    }

    private FileState CreateJsonFile(string json, string fileName = "config.json")
    {
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, json);
        return new FileState(filePath, new ExperimentConfigurationLoader());
    }

    #endregion

    #region LoadFromFile Edge Cases

    [Scenario("Empty YAML returns empty root")]
    [Fact]
    public Task Empty_yaml_returns_empty_root()
        => Given("an empty YAML file", () => CreateYamlFile("", "empty.yaml"))
            .When("loading from file", state => state.Loader.LoadFromFile(state.FilePath))
            .Then("result is not null", result => result != null)
            .AssertPassed();

    [Scenario("YAML with only comments returns empty root")]
    [Fact]
    public Task Yaml_with_only_comments_returns_empty_root()
        => Given("a YAML file with only comments", () => CreateYamlFile("""
            # This is a comment
            # Another comment
            """, "comments.yaml"))
            .When("loading from file", state => state.Loader.LoadFromFile(state.FilePath))
            .Then("result is not null", result => result != null)
            .AssertPassed();

    [Scenario("JSON with comments parses correctly")]
    [Fact]
    public Task Json_with_comments_parses_correctly()
        => Given("a JSON file with comments", () => CreateJsonFile("""
            {
              // This is a comment
              "experimentFramework": {
                "settings": {
                  "proxyStrategy": "dispatchProxy"
                }
              }
            }
            """, "comments.json"))
            .When("loading from file", state => state.Loader.LoadFromFile(state.FilePath))
            .Then("result is not null", result => result != null)
            .And("settings is not null", result => result.Settings != null)
            .And("proxy strategy is correct", result => result.Settings!.ProxyStrategy == "dispatchProxy")
            .AssertPassed();

    [Scenario("JSON with trailing commas parses correctly")]
    [Fact]
    public Task Json_with_trailing_commas_parses_correctly()
        => Given("a JSON file with trailing commas", () => CreateJsonFile("""
            {
              "experimentFramework": {
                "settings": {
                  "proxyStrategy": "dispatchProxy",
                },
              },
            }
            """, "trailing.json"))
            .When("loading from file", state => state.Loader.LoadFromFile(state.FilePath))
            .Then("result is not null", result => result != null)
            .AssertPassed();

    [Scenario("Invalid YAML throws ConfigurationLoadException")]
    [Fact]
    public Task Invalid_yaml_throws_exception()
        => Given("an invalid YAML file", () => CreateYamlFile("""
            experimentFramework:
              settings:
                proxyStrategy: "unclosed quote
            """, "invalid.yaml"))
            .Then("throws ConfigurationLoadException", state =>
            {
                try
                {
                    state.Loader.LoadFromFile(state.FilePath);
                    return false;
                }
                catch (ConfigurationLoadException)
                {
                    return true;
                }
            })
            .AssertPassed();

    [Scenario("Invalid JSON throws ConfigurationLoadException")]
    [Fact]
    public Task Invalid_json_throws_exception()
        => Given("an invalid JSON file", () => CreateJsonFile("""{ "invalid": "json""", "invalid.json"))
            .Then("throws ConfigurationLoadException", state =>
            {
                try
                {
                    state.Loader.LoadFromFile(state.FilePath);
                    return false;
                }
                catch (ConfigurationLoadException)
                {
                    return true;
                }
            })
            .AssertPassed();

    [Scenario("Non-existent file throws ConfigurationLoadException")]
    [Fact]
    public Task Non_existent_file_throws_exception()
        => Given("a loader", () => new ExperimentConfigurationLoader())
            .Then("throws ConfigurationLoadException", loader =>
            {
                try
                {
                    loader.LoadFromFile("/nonexistent/path/file.yaml");
                    return false;
                }
                catch (ConfigurationLoadException ex)
                {
                    return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase);
                }
            })
            .AssertPassed();

    [Scenario("Unsupported extension throws ConfigurationLoadException")]
    [Fact]
    public Task Unsupported_extension_throws_exception()
        => Given("a text file", () =>
            {
                var txtPath = Path.Combine(_tempDir, "config.txt");
                File.WriteAllText(txtPath, "content");
                return new FileState(txtPath, new ExperimentConfigurationLoader());
            })
            .Then("throws ConfigurationLoadException", state =>
            {
                try
                {
                    state.Loader.LoadFromFile(state.FilePath);
                    return false;
                }
                catch (ConfigurationLoadException ex)
                {
                    return ex.Message.Contains("extension", StringComparison.OrdinalIgnoreCase);
                }
            })
            .AssertPassed();

    [Scenario("YML extension parses as YAML")]
    [Fact]
    public Task Yml_extension_parses_as_yaml()
        => Given("a YML file", () => CreateYamlFile("""
            experimentFramework:
              settings:
                proxyStrategy: dispatchProxy
            """, "config.yml"))
            .When("loading from file", state => state.Loader.LoadFromFile(state.FilePath))
            .Then("result is not null", result => result != null)
            .And("proxy strategy is correct", result => result.Settings?.ProxyStrategy == "dispatchProxy")
            .AssertPassed();

    [Scenario("Uppercase extension parses correctly")]
    [Fact]
    public Task Uppercase_extension_parses_correctly()
        => Given("a YAML file with uppercase extension", () => CreateYamlFile("""
            experimentFramework:
              settings:
                proxyStrategy: dispatchProxy
            """, "config.YAML"))
            .When("loading from file", state => state.Loader.LoadFromFile(state.FilePath))
            .Then("result is not null", result => result != null)
            .AssertPassed();

    [Scenario("YAML without wrapper throws exception")]
    [Fact]
    public Task Yaml_without_wrapper_throws_exception()
        => Given("a YAML file without experimentFramework wrapper", () => CreateYamlFile("""
            settings:
              proxyStrategy: dispatchProxy
            trials:
              - serviceType: IService
                selectionMode:
                  type: featureFlag
                control:
                  key: control
                  implementationType: Service
            """, "direct.yaml"))
            .Then("throws ConfigurationLoadException", state =>
            {
                try
                {
                    state.Loader.LoadFromFile(state.FilePath);
                    return false;
                }
                catch (ConfigurationLoadException)
                {
                    return true;
                }
            })
            .AssertPassed();

    [Scenario("JSON with PascalCase parses correctly")]
    [Fact]
    public Task Json_with_pascal_case_parses_correctly()
        => Given("a JSON file with PascalCase properties", () => CreateJsonFile("""
            {
              "ExperimentFramework": {
                "Settings": {
                  "ProxyStrategy": "dispatchProxy"
                }
              }
            }
            """, "pascal.json"))
            .When("loading from file", state => state.Loader.LoadFromFile(state.FilePath))
            .Then("result is not null", result => result != null)
            .And("proxy strategy is correct", result => result.Settings?.ProxyStrategy == "dispatchProxy")
            .AssertPassed();

    #endregion

    #region Load Edge Cases

    [Scenario("Load with null configuration does not throw")]
    [Fact]
    public Task Load_with_null_configuration_does_not_throw()
        => Given("empty configuration and options", () => new LoadState(
            new ConfigurationBuilder().Build(),
            new ExperimentFrameworkConfigurationOptions { ScanDefaultPaths = false },
            new ExperimentConfigurationLoader()))
            .When("loading", state => state.Loader.Load(state.Configuration, state.Options))
            .Then("result is not null", result => result != null)
            .AssertPassed();

    [Scenario("Load with non-existent base path does not throw")]
    [Fact]
    public Task Load_with_non_existent_base_path_does_not_throw()
        => Given("options with non-existent base path", () => new LoadState(
            new ConfigurationBuilder().Build(),
            new ExperimentFrameworkConfigurationOptions
            {
                BasePath = "/nonexistent/path",
                ScanDefaultPaths = true
            },
            new ExperimentConfigurationLoader()))
            .When("loading", state => state.Loader.Load(state.Configuration, state.Options))
            .Then("result is not null", result => result != null)
            .AssertPassed();

    [Scenario("Load merges multiple files in correct order")]
    [Fact]
    public Task Load_merges_multiple_files_in_correct_order()
        => Given("two YAML files with overlapping settings", () =>
            {
                var yaml1 = """
                    experimentFramework:
                      settings:
                        namingConvention: convention1
                    """;
                var yaml2 = """
                    experimentFramework:
                      settings:
                        namingConvention: convention2
                    """;

                var definitionsDir = Path.Combine(_tempDir, "ExperimentDefinitions");
                Directory.CreateDirectory(definitionsDir);
                File.WriteAllText(Path.Combine(definitionsDir, "01-first.yaml"), yaml1);
                File.WriteAllText(Path.Combine(definitionsDir, "02-second.yaml"), yaml2);

                return new LoadState(
                    new ConfigurationBuilder().Build(),
                    new ExperimentFrameworkConfigurationOptions
                    {
                        BasePath = _tempDir,
                        ScanDefaultPaths = true
                    },
                    new ExperimentConfigurationLoader());
            })
            .When("loading", state => state.Loader.Load(state.Configuration, state.Options))
            .Then("settings is not null", result => result.Settings != null)
            .And("last file wins", result => result.Settings!.NamingConvention == "convention2")
            .AssertPassed();

    [Scenario("Load merges trials from multiple files")]
    [Fact]
    public Task Load_merges_trials_from_multiple_files()
        => Given("two YAML files with trials", () =>
            {
                var yaml1 = """
                    experimentFramework:
                      trials:
                        - serviceType: IService1
                          selectionMode:
                            type: featureFlag
                          control:
                            key: control
                            implementationType: Service1
                    """;
                var yaml2 = """
                    experimentFramework:
                      trials:
                        - serviceType: IService2
                          selectionMode:
                            type: featureFlag
                          control:
                            key: control
                            implementationType: Service2
                    """;

                var definitionsDir = Path.Combine(_tempDir, "ExperimentDefinitions");
                Directory.CreateDirectory(definitionsDir);
                File.WriteAllText(Path.Combine(definitionsDir, "first.yaml"), yaml1);
                File.WriteAllText(Path.Combine(definitionsDir, "second.yaml"), yaml2);

                return new LoadState(
                    new ConfigurationBuilder().Build(),
                    new ExperimentFrameworkConfigurationOptions
                    {
                        BasePath = _tempDir,
                        ScanDefaultPaths = true
                    },
                    new ExperimentConfigurationLoader());
            })
            .When("loading", state => state.Loader.Load(state.Configuration, state.Options))
            .Then("trials is not null", result => result.Trials != null)
            .And("has 2 trials", result => result.Trials!.Count == 2)
            .AssertPassed();

    [Scenario("Load with configuration paths adds to additional paths")]
    [Fact]
    public Task Load_with_configuration_paths_adds_to_additional_paths()
        => Given("custom configuration path in appsettings", () =>
            {
                var yaml = """
                    experimentFramework:
                      settings:
                        proxyStrategy: dispatchProxy
                    """;
                var customPath = Path.Combine(_tempDir, "custom", "experiments.yaml");
                Directory.CreateDirectory(Path.GetDirectoryName(customPath)!);
                File.WriteAllText(customPath, yaml);

                var json = """
                    {
                      "ExperimentFramework": {
                        "ConfigurationPaths": ["custom/experiments.yaml"]
                      }
                    }
                    """;
                var jsonPath = Path.Combine(_tempDir, "appsettings.json");
                File.WriteAllText(jsonPath, json);

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(_tempDir)
                    .AddJsonFile("appsettings.json")
                    .Build();

                return new LoadState(
                    configuration,
                    new ExperimentFrameworkConfigurationOptions
                    {
                        BasePath = _tempDir,
                        ScanDefaultPaths = false
                    },
                    new ExperimentConfigurationLoader());
            })
            .When("loading", state => state.Loader.Load(state.Configuration, state.Options))
            .Then("settings is not null", result => result.Settings != null)
            .And("proxy strategy is correct", result => result.Settings!.ProxyStrategy == "dispatchProxy")
            .AssertPassed();

    [Scenario("Load ignores empty files")]
    [Fact]
    public Task Load_ignores_empty_files()
        => Given("an empty YAML file", () =>
            {
                File.WriteAllText(Path.Combine(_tempDir, "experiments.yaml"), "");
                return new LoadState(
                    new ConfigurationBuilder().Build(),
                    new ExperimentFrameworkConfigurationOptions
                    {
                        BasePath = _tempDir,
                        ScanDefaultPaths = true
                    },
                    new ExperimentConfigurationLoader());
            })
            .When("loading", state => state.Loader.Load(state.Configuration, state.Options))
            .Then("result is not null", result => result != null)
            .AssertPassed();

    #endregion

    #region IExperimentConfigurationLoader Interface

    [Scenario("ExperimentConfigurationLoader implements IExperimentConfigurationLoader")]
    [Fact]
    public Task Loader_implements_interface()
        => Given("a loader", () => new ExperimentConfigurationLoader())
            .Then("implements IExperimentConfigurationLoader", loader => loader is IExperimentConfigurationLoader)
            .AssertPassed();

    #endregion

    #region YAML Features

    [Scenario("YAML with anchors and aliases parses correctly")]
    [Fact]
    public Task Yaml_with_anchors_and_aliases_parses_correctly()
        => Given("a YAML file with anchors and aliases", () => CreateYamlFile("""
            experimentFramework:
              defaults: &defaults
                key: control
                implementationType: DefaultService
              trials:
                - serviceType: IService
                  selectionMode:
                    type: featureFlag
                  control:
                    <<: *defaults
            """, "anchors.yaml"))
            .When("loading from file", state => state.Loader.LoadFromFile(state.FilePath))
            .Then("result is not null", result => result != null)
            .AssertPassed();

    [Scenario("YAML with multiline strings parses correctly")]
    [Fact]
    public Task Yaml_with_multiline_strings_parses_correctly()
        => Given("a YAML file with multiline strings", () => CreateYamlFile("""
            experimentFramework:
              experiments:
                - name: test-experiment
                  hypothesis:
                    nullHypothesis: |
                      There is no significant difference
                      between the control and treatment groups
                    alternativeHypothesis: >
                      The treatment group shows
                      improved performance
                  trials: []
            """, "multiline.yaml"))
            .When("loading from file", state => state.Loader.LoadFromFile(state.FilePath))
            .Then("result is not null", result => result != null)
            .And("experiments is not null", result => result.Experiments != null)
            .And("has one experiment", result => result.Experiments!.Count == 1)
            .AssertPassed();

    #endregion

    #region Complex Scenarios

    [Scenario("Complete YAML with all features parses correctly")]
    [Fact]
    public Task Complete_yaml_with_all_features_parses_correctly()
        => Given("a complete YAML file", () => CreateYamlFile("""
            experimentFramework:
              settings:
                proxyStrategy: dispatchProxy
                namingConvention: camelCase

              decorators:
                - type: logging
                  options:
                    benchmarks: true
                - type: timeout
                  options:
                    timeout: "00:00:30"

              trials:
                - serviceType: ITestService
                  selectionMode:
                    type: featureFlag
                    flagName: TestFlag
                  control:
                    key: control
                    implementationType: TestService
                  conditions:
                    - key: variant1
                      implementationType: TestServiceVariant1
                    - key: variant2
                      implementationType: TestServiceVariant2
                  errorPolicy:
                    type: tryInOrder
                    fallbackKeys:
                      - variant1
                      - control
                  activation:
                    from: "2024-01-01T00:00:00Z"
                    until: "2024-12-31T23:59:59Z"

              experiments:
                - name: performance-experiment
                  metadata:
                    owner: platform-team
                    ticket: PLAT-1234
                  hypothesis:
                    name: latency-improvement
                    type: superiority
                    nullHypothesis: No difference in latency
                    alternativeHypothesis: Treatment improves latency
                    primaryEndpoint:
                      name: response_time_ms
                      outcomeType: continuous
                      lowerIsBetter: true
                    expectedEffectSize: 0.2
                    successCriteria:
                      alpha: 0.05
                      power: 0.80
                  trials:
                    - serviceType: ICacheService
                      selectionMode:
                        type: configurationKey
                        key: "Cache:Provider"
                      control:
                        key: memory
                        implementationType: MemoryCache
                      conditions:
                        - key: redis
                          implementationType: RedisCache
            """, "complete.yaml"))
            .When("loading from file", state => state.Loader.LoadFromFile(state.FilePath))
            .Then("result is not null", result => result != null)
            .And("settings is not null", result => result.Settings != null)
            .And("proxy strategy is dispatchProxy", result => result.Settings!.ProxyStrategy == "dispatchProxy")
            .And("decorators has 2 items", result => result.Decorators?.Count == 2)
            .And("trials has 1 item", result => result.Trials?.Count == 1)
            .And("experiments has 1 item", result => result.Experiments?.Count == 1)
            .And("experiment name is correct", result => result.Experiments![0].Name == "performance-experiment")
            .AssertPassed();

    #endregion
}
