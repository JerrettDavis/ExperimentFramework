using ExperimentFramework.Bandit;
using ExperimentFramework.Bandit.Algorithms;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Bandit;

[Feature("ServiceCollectionExtensions registers bandit services")]
public sealed class BanditServiceCollectionExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("AddExperimentBanditEpsilonGreedy registers epsilon greedy algorithm")]
    [Fact]
    public async Task AddExperimentBanditEpsilonGreedy_registers_algorithm()
    {
        var services = new ServiceCollection();
        services.AddExperimentBanditEpsilonGreedy();
        var sp = services.BuildServiceProvider();

        var algorithm = sp.GetService<IBanditAlgorithm>();

        Assert.NotNull(algorithm);
        Assert.IsType<EpsilonGreedy>(algorithm);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentBanditEpsilonGreedy with custom epsilon")]
    [Fact]
    public async Task AddExperimentBanditEpsilonGreedy_with_custom_epsilon()
    {
        var services = new ServiceCollection();
        services.AddExperimentBanditEpsilonGreedy(epsilon: 0.2);
        var sp = services.BuildServiceProvider();

        var algorithm = sp.GetService<IBanditAlgorithm>();

        Assert.NotNull(algorithm);
        Assert.IsType<EpsilonGreedy>(algorithm);
        Assert.Equal("EpsilonGreedy", algorithm.Name);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentBanditEpsilonGreedy returns service collection for chaining")]
    [Fact]
    public Task AddExperimentBanditEpsilonGreedy_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding epsilon greedy", services => services.AddExperimentBanditEpsilonGreedy())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentBanditThompsonSampling registers Thompson Sampling algorithm")]
    [Fact]
    public async Task AddExperimentBanditThompsonSampling_registers_algorithm()
    {
        var services = new ServiceCollection();
        services.AddExperimentBanditThompsonSampling();
        var sp = services.BuildServiceProvider();

        var algorithm = sp.GetService<IBanditAlgorithm>();

        Assert.NotNull(algorithm);
        Assert.IsType<ThompsonSampling>(algorithm);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentBanditThompsonSampling returns service collection for chaining")]
    [Fact]
    public Task AddExperimentBanditThompsonSampling_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding Thompson Sampling", services => services.AddExperimentBanditThompsonSampling())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentBanditUcb registers UCB algorithm")]
    [Fact]
    public async Task AddExperimentBanditUcb_registers_algorithm()
    {
        var services = new ServiceCollection();
        services.AddExperimentBanditUcb();
        var sp = services.BuildServiceProvider();

        var algorithm = sp.GetService<IBanditAlgorithm>();

        Assert.NotNull(algorithm);
        Assert.IsType<UpperConfidenceBound>(algorithm);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentBanditUcb with custom exploration parameter")]
    [Fact]
    public async Task AddExperimentBanditUcb_with_custom_parameter()
    {
        var services = new ServiceCollection();
        services.AddExperimentBanditUcb(explorationParameter: 2.0);
        var sp = services.BuildServiceProvider();

        var algorithm = sp.GetService<IBanditAlgorithm>();

        Assert.NotNull(algorithm);
        Assert.IsType<UpperConfidenceBound>(algorithm);
        Assert.Equal("UCB1", algorithm.Name);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentBanditUcb returns service collection for chaining")]
    [Fact]
    public Task AddExperimentBanditUcb_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding UCB", services => services.AddExperimentBanditUcb())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentBandit registers custom algorithm")]
    [Fact]
    public async Task AddExperimentBandit_registers_custom_algorithm()
    {
        var services = new ServiceCollection();
        services.AddExperimentBandit<CustomBanditAlgorithm>();
        var sp = services.BuildServiceProvider();

        var algorithm = sp.GetService<IBanditAlgorithm>();

        Assert.NotNull(algorithm);
        Assert.IsType<CustomBanditAlgorithm>(algorithm);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentBandit returns service collection for chaining")]
    [Fact]
    public Task AddExperimentBandit_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding custom algorithm", services => services.AddExperimentBandit<CustomBanditAlgorithm>())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("Bandit registration is idempotent")]
    [Fact]
    public async Task Bandit_registration_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddExperimentBanditEpsilonGreedy();
        services.AddExperimentBanditThompsonSampling(); // Should not replace

        var sp = services.BuildServiceProvider();
        var algorithm = sp.GetService<IBanditAlgorithm>();

        // TryAddSingleton keeps first registration
        Assert.IsType<EpsilonGreedy>(algorithm);

        await Task.CompletedTask;
    }

    [Scenario("Different bandit registrations are idempotent")]
    [Fact]
    public async Task Different_bandit_registrations_are_idempotent()
    {
        var services = new ServiceCollection();
        services.AddExperimentBanditUcb();
        services.AddExperimentBandit<CustomBanditAlgorithm>(); // Should not replace

        var sp = services.BuildServiceProvider();
        var algorithm = sp.GetService<IBanditAlgorithm>();

        // TryAddSingleton keeps first registration
        Assert.IsType<UpperConfidenceBound>(algorithm);

        await Task.CompletedTask;
    }

    private sealed class CustomBanditAlgorithm : IBanditAlgorithm
    {
        public string Name => "Custom";

        public int SelectArm(IReadOnlyList<ArmStatistics> arms) => 0;

        public void UpdateArm(ArmStatistics arm, double reward)
        {
            arm.Pulls++;
            arm.TotalReward += reward;
        }
    }
}
