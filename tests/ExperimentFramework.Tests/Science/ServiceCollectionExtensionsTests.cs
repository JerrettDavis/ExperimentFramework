using ExperimentFramework.Data;
using ExperimentFramework.Science;
using ExperimentFramework.Science.Analysis;
using ExperimentFramework.Science.Corrections;
using ExperimentFramework.Science.EffectSize;
using ExperimentFramework.Science.Power;
using ExperimentFramework.Science.Reporting;
using ExperimentFramework.Science.Snapshots;
using ExperimentFramework.Science.Statistics;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Tests.Science;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddExperimentScience_RegistersStatisticalTests()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IStatisticalTest>());
        Assert.NotNull(provider.GetService<IPairedStatisticalTest>());
        Assert.NotNull(provider.GetService<IMultiGroupStatisticalTest>());
    }

    [Fact]
    public void AddExperimentScience_RegistersEffectSizeCalculators()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IEffectSizeCalculator>());
        Assert.NotNull(provider.GetService<IBinaryEffectSizeCalculator>());
    }

    [Fact]
    public void AddExperimentScience_RegistersPowerAnalyzer()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IPowerAnalyzer>());
    }

    [Fact]
    public void AddExperimentScience_RegistersCorrections()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IMultipleComparisonCorrection>());
    }

    [Fact]
    public void AddExperimentScience_RegistersSnapshotStore()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ISnapshotStore>());
        Assert.IsType<InMemorySnapshotStore>(provider.GetRequiredService<ISnapshotStore>());
    }

    [Fact]
    public void AddExperimentScience_RegistersReporters()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<MarkdownReporter>());
        Assert.NotNull(provider.GetService<JsonReporter>());
        Assert.NotNull(provider.GetService<IExperimentReporter>());
    }

    [Fact]
    public void AddExperimentScience_RegistersExperimentAnalyzer()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IExperimentAnalyzer>());
        Assert.IsType<ExperimentAnalyzer>(provider.GetRequiredService<IExperimentAnalyzer>());
    }

    [Fact]
    public void AddExperimentScience_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        var result = services.AddExperimentScience();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddExperimentScience_WithOptions_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();
        var customOptions = new ReporterOptions { IncludeDetailedStatistics = false };

        services.AddExperimentScience(opts =>
        {
            opts.ReporterOptions = customOptions;
        });

        var provider = services.BuildServiceProvider();

        // Should have both the default and custom reporter options
        Assert.NotNull(provider.GetService<ReporterOptions>());
    }

    [Fact]
    public void AddExperimentScience_WithOptions_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        var result = services.AddExperimentScience(opts => { });

        Assert.Same(services, result);
    }

    [Fact]
    public void AddExperimentScience_RegistersSingletons()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();

        var test1 = provider.GetRequiredService<IStatisticalTest>();
        var test2 = provider.GetRequiredService<IStatisticalTest>();
        var power1 = provider.GetRequiredService<IPowerAnalyzer>();
        var power2 = provider.GetRequiredService<IPowerAnalyzer>();

        Assert.Same(test1, test2);
        Assert.Same(power1, power2);
    }

    [Fact]
    public void AddExperimentScience_RegistersTwoSampleTTest()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();
        var test = provider.GetRequiredService<IStatisticalTest>();

        Assert.Same(TwoSampleTTest.Instance, test);
    }

    [Fact]
    public void AddExperimentScience_RegistersPairedTTest()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();
        var test = provider.GetRequiredService<IPairedStatisticalTest>();

        Assert.Same(PairedTTest.Instance, test);
    }

    [Fact]
    public void AddExperimentScience_RegistersOneWayAnova()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();
        var test = provider.GetRequiredService<IMultiGroupStatisticalTest>();

        Assert.Same(OneWayAnova.Instance, test);
    }

    [Fact]
    public void AddExperimentScience_RegistersCohensD()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();
        var calculator = provider.GetRequiredService<IEffectSizeCalculator>();

        Assert.Same(CohensD.Instance, calculator);
    }

    [Fact]
    public void AddExperimentScience_RegistersOddsRatio()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();
        var calculator = provider.GetRequiredService<IBinaryEffectSizeCalculator>();

        Assert.Same(OddsRatio.Instance, calculator);
    }

    [Fact]
    public void AddExperimentScience_RegistersBenjaminiHochbergCorrection()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();
        var correction = provider.GetRequiredService<IMultipleComparisonCorrection>();

        Assert.Same(BenjaminiHochbergCorrection.Instance, correction);
    }

    [Fact]
    public void AddExperimentScience_RegistersPowerAnalyzerInstance()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();
        var analyzer = provider.GetRequiredService<IPowerAnalyzer>();

        Assert.Same(PowerAnalyzer.Instance, analyzer);
    }

    [Fact]
    public void AddExperimentScience_MarkdownReporterAsDefaultReporter()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();

        services.AddExperimentScience();

        var provider = services.BuildServiceProvider();
        var reporter = provider.GetRequiredService<IExperimentReporter>();

        Assert.IsType<MarkdownReporter>(reporter);
    }

    [Fact]
    public void AddExperimentScience_CanResolveAllServicesIndependently()
    {
        var services = new ServiceCollection();
        services.AddExperimentDataCollection();
        services.AddExperimentScience();
        var provider = services.BuildServiceProvider();

        // Verify each service can be resolved independently
        Assert.NotNull(provider.GetRequiredService<IStatisticalTest>());
        Assert.NotNull(provider.GetRequiredService<IPairedStatisticalTest>());
        Assert.NotNull(provider.GetRequiredService<IMultiGroupStatisticalTest>());
        Assert.NotNull(provider.GetRequiredService<IEffectSizeCalculator>());
        Assert.NotNull(provider.GetRequiredService<IBinaryEffectSizeCalculator>());
        Assert.NotNull(provider.GetRequiredService<IPowerAnalyzer>());
        Assert.NotNull(provider.GetRequiredService<IMultipleComparisonCorrection>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotStore>());
        Assert.NotNull(provider.GetRequiredService<MarkdownReporter>());
        Assert.NotNull(provider.GetRequiredService<JsonReporter>());
        Assert.NotNull(provider.GetRequiredService<IExperimentReporter>());
        Assert.NotNull(provider.GetRequiredService<IExperimentAnalyzer>());
    }
}
