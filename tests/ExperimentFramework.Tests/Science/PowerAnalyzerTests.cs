using ExperimentFramework.Science.Power;

namespace ExperimentFramework.Tests.Science;

public class PowerAnalyzerTests
{
    [Fact]
    public void CalculateSampleSize_WithMediumEffect_ReturnsReasonableSize()
    {
        // Arrange - medium effect size (d = 0.5)
        var analyzer = PowerAnalyzer.Instance;

        // Act
        var sampleSize = analyzer.CalculateSampleSize(0.5, 0.80, 0.05);

        // Assert - for d=0.5 with 80% power at alpha=0.05, approximately 64 per group
        Assert.True(sampleSize is >= 60 and <= 70);
    }

    [Fact]
    public void CalculateSampleSize_WithSmallEffect_RequiresLargerSample()
    {
        // Arrange
        var analyzer = PowerAnalyzer.Instance;

        // Act
        var smallEffect = analyzer.CalculateSampleSize(0.2, 0.80, 0.05);
        var largeEffect = analyzer.CalculateSampleSize(0.8, 0.80, 0.05);

        // Assert
        Assert.True(smallEffect > largeEffect);
    }

    [Fact]
    public void CalculatePower_WithLargeSample_ReturnsHighPower()
    {
        // Arrange
        var analyzer = PowerAnalyzer.Instance;

        // Act
        var power = analyzer.CalculatePower(1000, 0.5, 0.05);

        // Assert
        Assert.True(power > 0.99);
    }

    [Fact]
    public void CalculatePower_WithSmallSample_ReturnsLowPower()
    {
        // Arrange
        var analyzer = PowerAnalyzer.Instance;

        // Act
        var power = analyzer.CalculatePower(10, 0.2, 0.05);

        // Assert
        Assert.True(power < 0.5);
    }

    [Fact]
    public void CalculateMinimumDetectableEffect_DecreasesWithSampleSize()
    {
        // Arrange
        var analyzer = PowerAnalyzer.Instance;

        // Act
        var mdeSmall = analyzer.CalculateMinimumDetectableEffect(50, 0.80, 0.05);
        var mdeLarge = analyzer.CalculateMinimumDetectableEffect(500, 0.80, 0.05);

        // Assert
        Assert.True(mdeSmall > mdeLarge);
    }

    [Fact]
    public void Analyze_ReturnsComprehensiveResult()
    {
        // Arrange
        var analyzer = PowerAnalyzer.Instance;

        // Act
        var result = analyzer.Analyze(100, 0.5, 0.80, 0.05);

        // Assert
        Assert.True(result.AchievedPower > 0);
        Assert.True(result.AchievedPower <= 1);
        Assert.True(result.RequiredSampleSize > 0);
        Assert.Equal(100, result.CurrentSampleSize);
        Assert.Equal(0.05, result.Alpha);
        Assert.Equal(0.80, result.TargetPower);
    }

    [Fact]
    public void Analyze_IdentifiesUnderpoweredExperiment()
    {
        // Arrange
        var analyzer = PowerAnalyzer.Instance;

        // Act - small sample with small effect
        var result = analyzer.Analyze(20, 0.2, 0.80, 0.05);

        // Assert
        Assert.False(result.IsAdequatelyPowered);
    }

    [Fact]
    public void CalculateSampleSize_OneSided_RequiresSmallerSample()
    {
        // Arrange
        var analyzer = PowerAnalyzer.Instance;
        var options = new PowerOptions { OneSided = true };

        // Act
        var oneSided = analyzer.CalculateSampleSize(0.5, 0.80, 0.05, options);
        var twoSided = analyzer.CalculateSampleSize(0.5, 0.80, 0.05);

        // Assert
        Assert.True(oneSided < twoSided);
    }
}
