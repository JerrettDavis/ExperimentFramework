using ExperimentFramework.Configuration;
using ExperimentFramework.Configuration.Extensions.Handlers;
using ExperimentFramework.Configuration.Loading;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Policy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration.Governance;

[Feature("Governance configuration from YAML/JSON")]
public class GovernanceConfigurationTests : TinyBddXunitBase, IDisposable
{
    private readonly string _tempDir;
    private readonly ExperimentConfigurationLoader _loader = new();

    public GovernanceConfigurationTests(ITestOutputHelper output) : base(output)
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GovernanceConfigTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public new void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Scenario("Null governance config does not register services")]
    [Fact]
    public async Task NullConfig_does_not_register_governance()
    {
        var services = new ServiceCollection();
        var handler = new GovernanceConfigurationHandler();

        handler.ApplyGovernanceConfiguration(services, null);
        var provider = services.BuildServiceProvider();

        Assert.Null(provider.GetService<ILifecycleManager>());
        
        await Task.CompletedTask;
    }

    [Scenario("Empty governance config registers base services")]
    [Fact]
    public async Task EmptyConfig_registers_base_governance_services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var handler = new GovernanceConfigurationHandler();
        var config = new GovernanceConfig();

        handler.ApplyGovernanceConfiguration(services, config);
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ILifecycleManager>());
        Assert.NotNull(provider.GetService<IApprovalManager>());
        Assert.NotNull(provider.GetService<IPolicyEvaluator>());
        
        await Task.CompletedTask;
    }

    [Scenario("Automatic approval gate is configured from YAML")]
    [Fact]
    public async Task AutomaticGate_is_configured()
    {
        var config = new GovernanceConfig
        {
            ApprovalGates = new List<ApprovalGateConfig>
            {
                new()
                {
                    Type = "automatic",
                    ToState = "Running"
                }
            }
        };

        var services = new ServiceCollection();
        services.AddLogging();
        var handler = new GovernanceConfigurationHandler();
        handler.ApplyGovernanceConfiguration(services, config);
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IApprovalManager>());
        
        await Task.CompletedTask;
    }

    [Scenario("Role-based approval gate is configured from YAML")]
    [Fact]
    public async Task RoleBasedGate_is_configured()
    {
        var config = new GovernanceConfig
        {
            ApprovalGates = new List<ApprovalGateConfig>
            {
                new()
                {
                    Type = "roleBased",
                    FromState = "Approved",
                    ToState = "Running",
                    AllowedRoles = new List<string> { "operator", "sre" }
                }
            }
        };

        var services = new ServiceCollection();
        services.AddLogging();
        var handler = new GovernanceConfigurationHandler();
        handler.ApplyGovernanceConfiguration(services, config);
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IApprovalManager>());
        
        await Task.CompletedTask;
    }

    [Scenario("Traffic limit policy is configured from YAML")]
    [Fact]
    public async Task TrafficLimitPolicy_is_configured()
    {
        var config = new GovernanceConfig
        {
            Policies = new List<PolicyConfig>
            {
                new()
                {
                    Type = "trafficLimit",
                    MaxTrafficPercentage = 10.0,
                    MinStableTime = "00:30:00"
                }
            }
        };

        var services = new ServiceCollection();
        services.AddLogging();
        var handler = new GovernanceConfigurationHandler();
        handler.ApplyGovernanceConfiguration(services, config);
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IPolicyEvaluator>());
        
        await Task.CompletedTask;
    }

    [Scenario("Complete governance configuration loads from YAML")]
    [Fact]
    public async Task Complete_governance_loads_from_yaml()
    {
        var yamlContent = @"
experimentFramework:
  governance:
    enableAutoVersioning: true
    approvalGates:
      - type: automatic
        fromState: Draft
        toState: PendingApproval
      - type: roleBased
        fromState: Approved
        toState: Running
        allowedRoles:
          - operator
          - sre
    policies:
      - type: trafficLimit
        maxTrafficPercentage: 10.0
        minStableTime: '00:30:00'
      - type: errorRate
        maxErrorRate: 0.05
";

        var configFilePath = Path.Combine(_tempDir, "governance-test.yaml");
        await File.WriteAllTextAsync(configFilePath, yamlContent);

        // Load configuration using the loader
        var config = _loader.LoadFromFile(configFilePath);
        
        Assert.NotNull(config);
        Assert.NotNull(config.Governance);
        Assert.True(config.Governance.EnableAutoVersioning);
        Assert.NotNull(config.Governance.ApprovalGates);
        Assert.Equal(2, config.Governance.ApprovalGates.Count);
        Assert.NotNull(config.Governance.Policies);
        Assert.Equal(2, config.Governance.Policies.Count);

        // Test that services can be configured from loaded config
        var services = new ServiceCollection();
        services.AddLogging();

        var handler = new GovernanceConfigurationHandler();
        handler.ApplyGovernanceConfiguration(services, config.Governance);
        
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ILifecycleManager>());
        Assert.NotNull(provider.GetService<IApprovalManager>());
        Assert.NotNull(provider.GetService<IPolicyEvaluator>());

        // Test lifecycle transition
        var lifecycleManager = provider.GetRequiredService<ILifecycleManager>();
        await lifecycleManager.TransitionAsync(
            "test-experiment",
            ExperimentLifecycleState.PendingApproval,
            actor: "test-user");
        
        var state = lifecycleManager.GetState("test-experiment");
        Assert.Equal(ExperimentLifecycleState.PendingApproval, state);

        // Test policy evaluation - verify policies can be retrieved
        var policyEvaluator = provider.GetRequiredService<IPolicyEvaluator>();
        
        // Policy evaluator should be functional (even if no violations)
        var policyContext = new PolicyContext
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Running,
            Telemetry = new Dictionary<string, object>
            {
                ["trafficPercentage"] = 5.0,
                ["errorRate"] = 0.03
            }
        };

        // Should not throw - policies are registered and can evaluate
        var results = await policyEvaluator.EvaluateAllAsync(policyContext);
        // Results may be empty if no violations, which is valid
        Assert.NotNull(results);
    }

    [Scenario("Minimal governance configuration loads from JSON")]
    [Fact]
    public async Task Minimal_governance_loads_from_json()
    {
        var jsonContent = @"
{
  ""experimentFramework"": {
    ""governance"": {
      ""approvalGates"": [
        {
          ""type"": ""automatic"",
          ""toState"": ""PendingApproval""
        }
      ],
      ""policies"": [
        {
          ""type"": ""trafficLimit"",
          ""maxTrafficPercentage"": 25.0
        }
      ]
    }
  }
}
";

        var configFilePath = Path.Combine(_tempDir, "governance-test.json");
        await File.WriteAllTextAsync(configFilePath, jsonContent);

        // Load configuration using the loader
        var config = _loader.LoadFromFile(configFilePath);
        
        Assert.NotNull(config);
        Assert.NotNull(config.Governance);
        Assert.NotNull(config.Governance.ApprovalGates);
        Assert.Single(config.Governance.ApprovalGates);
        Assert.Equal("automatic", config.Governance.ApprovalGates[0].Type);
        Assert.NotNull(config.Governance.Policies);
        Assert.Single(config.Governance.Policies);
        Assert.Equal("trafficLimit", config.Governance.Policies[0].Type);

        // Test that services can be configured from loaded config
        var services = new ServiceCollection();
        services.AddLogging();

        var handler = new GovernanceConfigurationHandler();
        handler.ApplyGovernanceConfiguration(services, config.Governance);
        
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ILifecycleManager>());
        Assert.NotNull(provider.GetService<IPolicyEvaluator>());

        await Task.CompletedTask;
    }
}
