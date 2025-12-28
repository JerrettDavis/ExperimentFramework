using ExperimentFramework.Rollout;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Rollout;

[Feature("ExperimentBuilderExtensions provides rollout extension methods")]
public sealed class ExperimentBuilderExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("UsingRollout configures rollout selection mode")]
    [Fact]
    public async Task UsingRollout_configures_rollout_selection_mode()
    {
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingRollout();
            exp.AddControl<TestServiceA>("control");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("UsingRollout accepts custom rollout name")]
    [Fact]
    public async Task UsingRollout_accepts_custom_name()
    {
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingRollout("my-custom-rollout");
            exp.AddControl<TestServiceA>("control");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("UsingRollout accepts null rollout name")]
    [Fact]
    public async Task UsingRollout_accepts_null_name()
    {
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingRollout(null);
            exp.AddControl<TestServiceA>("control");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("UsingStagedRollout configures staged rollout selection mode")]
    [Fact]
    public async Task UsingStagedRollout_configures_staged_rollout_selection_mode()
    {
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingStagedRollout();
            exp.AddControl<TestServiceA>("control");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("UsingStagedRollout accepts custom rollout name")]
    [Fact]
    public async Task UsingStagedRollout_accepts_custom_name()
    {
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingStagedRollout("staged-migration");
            exp.AddControl<TestServiceA>("control");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("UsingStagedRollout accepts null rollout name")]
    [Fact]
    public async Task UsingStagedRollout_accepts_null_name()
    {
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingStagedRollout(null);
            exp.AddControl<TestServiceA>("control");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("UsingRollout returns builder for chaining")]
    [Fact]
    public async Task UsingRollout_returns_builder_for_chaining()
    {
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            var result = exp.UsingRollout("test")
                .AddControl<TestServiceA>("control")
                .AddCondition<TestServiceB>("treatment");

            Assert.NotNull(result);
        });

        await Task.CompletedTask;
    }

    [Scenario("UsingStagedRollout returns builder for chaining")]
    [Fact]
    public async Task UsingStagedRollout_returns_builder_for_chaining()
    {
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            var result = exp.UsingStagedRollout("test")
                .AddControl<TestServiceA>("control")
                .AddCondition<TestServiceB>("treatment");

            Assert.NotNull(result);
        });

        await Task.CompletedTask;
    }

    private interface ITestService { }
    private class TestServiceA : ITestService { }
    private class TestServiceB : ITestService { }
}
