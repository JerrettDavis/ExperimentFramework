using ExperimentFramework.Configuration;
using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration;

[Feature("ServiceCollectionExtensions registers experiment framework from configuration")]
public class ServiceCollectionExtensionsTests : TinyBddXunitBase, IDisposable
{
    private readonly string _tempDir;

    public ServiceCollectionExtensionsTests(ITestOutputHelper output) : base(output)
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ServiceCollectionExtensionsTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public new void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Test Services

    public interface ITestService
    {
        string GetValue();
    }

    public class TestServiceA : ITestService
    {
        public string GetValue() => "A";
    }

    public class TestServiceB : ITestService
    {
        public string GetValue() => "B";
    }

    #endregion

    #region State Records

    private sealed record ConfigState(string YamlContent, string YamlPath, IConfiguration Configuration, IServiceCollection Services);
    private sealed record BuildResult(ConfigState State, ServiceProvider Provider);

    #endregion

    #region Helper Methods

    private ConfigState CreateYamlConfig(string yaml)
    {
        var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
        File.WriteAllText(yamlPath, yaml);
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();
        return new ConfigState(yaml, yamlPath, configuration, services);
    }

    #endregion

    [Scenario("Configuration with valid YAML registers services")]
    [Fact]
    public Task Valid_yaml_registers_services()
        => Given("valid YAML configuration", () => CreateYamlConfig($"""
            experimentFramework:
              settings:
                proxyStrategy: dispatchProxy
              trials:
                - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                  selectionMode:
                    type: featureFlag
                    flagName: TestFlag
                  control:
                    key: control
                    implementationType: "{typeof(TestServiceA).AssemblyQualifiedName}"
            """))
            .When("registering from configuration", state =>
            {
                state.Services.AddExperimentFrameworkFromConfiguration(state.Configuration, opts =>
                {
                    opts.BasePath = _tempDir;
                    opts.ScanDefaultPaths = true;
                });
                return new BuildResult(state, state.Services.BuildServiceProvider());
            })
            .Then("service provider is not null", result => result.Provider != null)
            .And("type resolver is registered", result => result.Provider.GetService<ITypeResolver>() != null)
            .AssertPassed();

    [Scenario("Configuration with no options uses defaults")]
    [Fact]
    public Task No_options_uses_defaults()
        => Given("empty configuration", () =>
            {
                var configuration = new ConfigurationBuilder().Build();
                var services = new ServiceCollection();
                return (configuration, services);
            })
            .When("registering without options", state =>
            {
                state.services.AddExperimentFrameworkFromConfiguration(state.configuration);
                return state.services.BuildServiceProvider();
            })
            .Then("type resolver is registered", provider => provider.GetService<ITypeResolver>() != null)
            .AssertPassed();

    [Scenario("Configuration with type aliases resolves types")]
    [Fact]
    public Task Type_aliases_resolve_types()
        => Given("YAML with simple type names", () => CreateYamlConfig("""
            experimentFramework:
              settings:
                proxyStrategy: dispatchProxy
              trials:
                - serviceType: "ITestService"
                  selectionMode:
                    type: featureFlag
                    flagName: TestFlag
                  control:
                    key: control
                    implementationType: "TestServiceA"
            """))
            .When("registering with type aliases", state =>
            {
                state.Services.AddExperimentFrameworkFromConfiguration(state.Configuration, opts =>
                {
                    opts.BasePath = _tempDir;
                    opts.ScanDefaultPaths = true;
                    opts.TypeAliases.Add("ITestService", typeof(ITestService));
                    opts.TypeAliases.Add("TestServiceA", typeof(TestServiceA));
                });
                return state.Services.BuildServiceProvider();
            })
            .Then("provider is not null", provider => provider != null)
            .AssertPassed();

    [Scenario("Invalid configuration throws when configured")]
    [Fact]
    public Task Invalid_config_throws_when_configured()
        => Given("invalid YAML configuration", () =>
            {
                var yaml = """
                    experimentFramework:
                      trials:
                        - selectionMode:
                            type: featureFlag
                          control:
                            key: control
                            implementationType: "SomeType"
                    """;
                var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
                File.WriteAllText(yamlPath, yaml);
                var configuration = new ConfigurationBuilder().Build();
                var services = new ServiceCollection();
                return (configuration, services, _tempDir);
            })
            .Then("throws ExperimentConfigurationException", state =>
            {
                var thrown = false;
                try
                {
                    state.services.AddExperimentFrameworkFromConfiguration(state.configuration, opts =>
                    {
                        opts.BasePath = state._tempDir;
                        opts.ScanDefaultPaths = true;
                        opts.ThrowOnValidationErrors = true;
                    });
                }
                catch (ExperimentConfigurationException)
                {
                    thrown = true;
                }
                return thrown;
            })
            .AssertPassed();

    [Scenario("Empty configuration succeeds")]
    [Fact]
    public Task Empty_config_succeeds()
        => Given("empty YAML configuration", () => CreateYamlConfig("""
            experimentFramework:
              settings:
                proxyStrategy: dispatchProxy
            """))
            .When("registering configuration", state =>
            {
                state.Services.AddExperimentFrameworkFromConfiguration(state.Configuration, opts =>
                {
                    opts.BasePath = _tempDir;
                    opts.ScanDefaultPaths = true;
                    opts.ThrowOnValidationErrors = false;
                });
                return state.Services.BuildServiceProvider();
            })
            .Then("provider is not null", provider => provider != null)
            .AssertPassed();

    [Scenario("Hot reload enabled does not throw")]
    [Fact]
    public Task Hot_reload_enabled_does_not_throw()
        => Given("valid YAML configuration", () => CreateYamlConfig($"""
            experimentFramework:
              trials:
                - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                  selectionMode:
                    type: featureFlag
                    flagName: test-feature
                  control:
                    key: control
                    implementationType: "{typeof(TestServiceA).AssemblyQualifiedName}"
            """))
            .When("registering with hot reload", state =>
            {
                state.Services.AddExperimentFrameworkFromConfiguration(state.Configuration, opts =>
                {
                    opts.BasePath = _tempDir;
                    opts.ScanDefaultPaths = true;
                    opts.EnableHotReload = true;
                });
                return state.Services.BuildServiceProvider();
            })
            .Then("provider is not null", provider => provider != null)
            .And("ITestService is registered", provider =>
                provider.GetServices<IServiceProvider>() != null)
            .AssertPassed();

    [Scenario("Hybrid mode merges configurations")]
    [Fact]
    public Task Hybrid_mode_merges_configurations()
        => Given("YAML configuration and builder", () =>
            {
                var yaml = $"""
                    experimentFramework:
                      trials:
                        - serviceType: "{typeof(ITestService).AssemblyQualifiedName}"
                          selectionMode:
                            type: featureFlag
                            flagName: FileFlag
                          control:
                            key: control
                            implementationType: "{typeof(TestServiceA).AssemblyQualifiedName}"
                    """;
                var yamlPath = Path.Combine(_tempDir, "experiments.yaml");
                File.WriteAllText(yamlPath, yaml);
                var builder = ExperimentFrameworkBuilder.Create().UseDispatchProxy();
                var configuration = new ConfigurationBuilder().Build();
                var services = new ServiceCollection();
                services.AddScoped<ITestService, TestServiceA>();
                return (builder, configuration, services, _tempDir);
            })
            .When("registering hybrid mode", state =>
            {
                state.services.AddExperimentFramework(state.builder, state.configuration, opts =>
                {
                    opts.BasePath = state._tempDir;
                    opts.ScanDefaultPaths = true;
                });
                return state.services.BuildServiceProvider();
            })
            .Then("provider is not null", provider => provider != null)
            .AssertPassed();

    [Scenario("Hybrid mode with no options uses defaults")]
    [Fact]
    public Task Hybrid_mode_no_options_uses_defaults()
        => Given("builder and empty configuration", () =>
            {
                var builder = ExperimentFrameworkBuilder.Create().UseDispatchProxy();
                var configuration = new ConfigurationBuilder().Build();
                var services = new ServiceCollection();
                return (builder, configuration, services);
            })
            .When("registering hybrid mode", state =>
            {
                state.services.AddExperimentFramework(state.builder, state.configuration);
                return state.services.BuildServiceProvider();
            })
            .Then("type resolver is registered", provider => provider.GetService<ITypeResolver>() != null)
            .AssertPassed();

    [Scenario("Type resolver is registered as singleton")]
    [Fact]
    public Task Type_resolver_is_singleton()
        => Given("empty configuration", () =>
            {
                var configuration = new ConfigurationBuilder().Build();
                var services = new ServiceCollection();
                return (configuration, services);
            })
            .When("registering configuration", state =>
            {
                state.services.AddExperimentFrameworkFromConfiguration(state.configuration, opts =>
                {
                    opts.ScanDefaultPaths = false;
                });
                return state.services.BuildServiceProvider();
            })
            .Then("same resolver instance", provider =>
            {
                var resolver1 = provider.GetService<ITypeResolver>();
                var resolver2 = provider.GetService<ITypeResolver>();
                return ReferenceEquals(resolver1, resolver2);
            })
            .AssertPassed();

    [Scenario("Custom section name loads settings")]
    [Fact]
    public Task Custom_section_name_loads_settings()
        => Given("custom section in appsettings", () =>
            {
                var json = """
                    {
                      "MyExperiments": {
                        "Settings": {
                          "ProxyStrategy": "dispatchProxy"
                        }
                      }
                    }
                    """;
                var jsonPath = Path.Combine(_tempDir, "appsettings.json");
                File.WriteAllText(jsonPath, json);
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(_tempDir)
                    .AddJsonFile("appsettings.json")
                    .Build();
                var services = new ServiceCollection();
                return (configuration, services, _tempDir);
            })
            .When("registering with custom section", state =>
            {
                state.services.AddExperimentFrameworkFromConfiguration(state.configuration, opts =>
                {
                    opts.ConfigurationSectionName = "MyExperiments";
                    opts.ScanDefaultPaths = false;
                });
                return state.services.BuildServiceProvider();
            })
            .Then("provider is not null", provider => provider != null)
            .AssertPassed();

    [Scenario("Chaining works")]
    [Fact]
    public Task Chaining_works()
        => Given("service collection", () =>
            {
                var configuration = new ConfigurationBuilder().Build();
                var services = new ServiceCollection();
                return (configuration, services);
            })
            .When("chaining calls", state =>
            {
                var result = state.services
                    .AddExperimentFrameworkFromConfiguration(state.configuration, opts => opts.ScanDefaultPaths = false)
                    .AddSingleton<TestServiceA>();
                return (state.services, result);
            })
            .Then("returns same collection", t => ReferenceEquals(t.services, t.result))
            .AssertPassed();
}
