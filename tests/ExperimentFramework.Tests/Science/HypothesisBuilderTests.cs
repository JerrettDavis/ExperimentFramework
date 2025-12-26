using ExperimentFramework.Data.Models;
using ExperimentFramework.Science.Builders;
using ExperimentFramework.Science.Models.Hypothesis;

namespace ExperimentFramework.Tests.Science;

public class HypothesisBuilderTests
{
    [Fact]
    public void Build_CreatesValidHypothesis()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test-hypothesis")
            .NullHypothesis("No effect")
            .AlternativeHypothesis("Treatment improves outcome")
            .PrimaryEndpoint("conversion", OutcomeType.Binary)
            .ExpectedEffectSize(0.05)
            .Build();

        // Assert
        Assert.Equal("test-hypothesis", hypothesis.Name);
        Assert.Equal("No effect", hypothesis.NullHypothesis);
        Assert.Equal("Treatment improves outcome", hypothesis.AlternativeHypothesis);
        Assert.Equal("conversion", hypothesis.PrimaryEndpoint.Name);
        Assert.Equal(0.05, hypothesis.ExpectedEffectSize);
    }

    [Fact]
    public void Superiority_SetsCorrectType()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .Superiority()
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .Build();

        // Assert
        Assert.Equal(HypothesisType.Superiority, hypothesis.Type);
    }

    [Fact]
    public void NonInferiority_SetsCorrectType()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NonInferiority()
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .Build();

        // Assert
        Assert.Equal(HypothesisType.NonInferiority, hypothesis.Type);
    }

    [Fact]
    public void Equivalence_SetsCorrectType()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .Equivalence()
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .Build();

        // Assert
        Assert.Equal(HypothesisType.Equivalence, hypothesis.Type);
    }

    [Fact]
    public void TwoSided_SetsCorrectType()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .TwoSided()
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .Build();

        // Assert
        Assert.Equal(HypothesisType.TwoSided, hypothesis.Type);
    }

    [Fact]
    public void Description_SetsDescription()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .Description("This is a test hypothesis")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .Build();

        // Assert
        Assert.Equal("This is a test hypothesis", hypothesis.Description);
    }

    [Fact]
    public void PrimaryEndpoint_ConfiguresEndpoint()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("revenue", OutcomeType.Continuous, ep => ep
                .Description("Total revenue")
                .Unit("USD")
                .HigherIsBetter()
                .ExpectedBaseline(50.0)
                .ExpectedVariance(625.0)
                .MinimumImportantDifference(5.0))
            .Build();

        // Assert
        Assert.Equal("revenue", hypothesis.PrimaryEndpoint.Name);
        Assert.Equal(OutcomeType.Continuous, hypothesis.PrimaryEndpoint.OutcomeType);
        Assert.Equal("Total revenue", hypothesis.PrimaryEndpoint.Description);
        Assert.Equal("USD", hypothesis.PrimaryEndpoint.Unit);
        Assert.True(hypothesis.PrimaryEndpoint.HigherIsBetter);
        Assert.Equal(50.0, hypothesis.PrimaryEndpoint.ExpectedBaselineValue);
        Assert.Equal(625.0, hypothesis.PrimaryEndpoint.ExpectedVariance);
        Assert.Equal(5.0, hypothesis.PrimaryEndpoint.MinimumImportantDifference);
    }

    [Fact]
    public void SecondaryEndpoint_AddsMultipleEndpoints()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("primary", OutcomeType.Binary)
            .SecondaryEndpoint("secondary1", OutcomeType.Continuous)
            .SecondaryEndpoint("secondary2", OutcomeType.Duration, ep => ep.LowerIsBetter())
            .Build();

        // Assert
        Assert.NotNull(hypothesis.SecondaryEndpoints);
        Assert.Equal(2, hypothesis.SecondaryEndpoints.Count);
        Assert.Equal("secondary1", hypothesis.SecondaryEndpoints[0].Name);
        Assert.Equal("secondary2", hypothesis.SecondaryEndpoints[1].Name);
        Assert.False(hypothesis.SecondaryEndpoints[1].HigherIsBetter);
    }

    [Fact]
    public void WithSuccessCriteria_ConfiguresCriteria()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .WithSuccessCriteria(c => c
                .Alpha(0.01)
                .Power(0.90)
                .MinimumSampleSize(500)
                .MinimumEffectSize(0.03)
                .MinimumDuration(TimeSpan.FromDays(7))
                .RequirePositiveEffect())
            .Build();

        // Assert
        Assert.Equal(0.01, hypothesis.SuccessCriteria.Alpha);
        Assert.Equal(0.90, hypothesis.SuccessCriteria.Power);
        Assert.Equal(500, hypothesis.SuccessCriteria.MinimumSampleSize);
        Assert.Equal(0.03, hypothesis.SuccessCriteria.MinimumEffectSize);
        Assert.Equal(TimeSpan.FromDays(7), hypothesis.SuccessCriteria.MinimumDuration);
        Assert.True(hypothesis.SuccessCriteria.RequirePositiveEffect);
    }

    [Fact]
    public void WithSuccessCriteria_NonInferiorityMargin()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NonInferiority()
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Continuous)
            .WithSuccessCriteria(c => c.NonInferiorityMargin(50.0))
            .Build();

        // Assert
        Assert.Equal(50.0, hypothesis.SuccessCriteria.NonInferiorityMargin);
    }

    [Fact]
    public void WithSuccessCriteria_EquivalenceMargin()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .Equivalence()
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Continuous)
            .WithSuccessCriteria(c => c.EquivalenceMargin(0.02))
            .Build();

        // Assert
        Assert.Equal(0.02, hypothesis.SuccessCriteria.EquivalenceMargin);
    }

    [Fact]
    public void WithSuccessCriteria_PrimaryEndpointOnly()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .WithSuccessCriteria(c => c.PrimaryEndpointOnly())
            .Build();

        // Assert
        Assert.True(hypothesis.SuccessCriteria.PrimaryEndpointOnly);
    }

    [Fact]
    public void WithSuccessCriteria_AllEndpoints()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .WithSuccessCriteria(c => c.AllEndpoints())
            .Build();

        // Assert
        Assert.False(hypothesis.SuccessCriteria.PrimaryEndpointOnly);
    }

    [Fact]
    public void WithSuccessCriteria_MultipleComparisonCorrection()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .WithSuccessCriteria(c => c.WithMultipleComparisonCorrection())
            .Build();

        // Assert
        Assert.True(hypothesis.SuccessCriteria.ApplyMultipleComparisonCorrection);
    }

    [Fact]
    public void WithSuccessCriteria_NoMultipleComparisonCorrection()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .WithSuccessCriteria(c => c.NoMultipleComparisonCorrection())
            .Build();

        // Assert
        Assert.False(hypothesis.SuccessCriteria.ApplyMultipleComparisonCorrection);
    }

    [Fact]
    public void WithSuccessCriteria_AnySignificantEffect()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .WithSuccessCriteria(c => c.AnySignificantEffect())
            .Build();

        // Assert
        Assert.False(hypothesis.SuccessCriteria.RequirePositiveEffect);
    }

    [Fact]
    public void Control_SetsControlCondition()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .Control("baseline")
            .Build();

        // Assert
        Assert.Equal("baseline", hypothesis.ControlCondition);
    }

    [Fact]
    public void Treatment_AddsTreatmentConditions()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .Treatment("variant-a")
            .Treatment("variant-b")
            .Build();

        // Assert
        Assert.NotNull(hypothesis.TreatmentConditions);
        Assert.Equal(2, hypothesis.TreatmentConditions.Count);
        Assert.Contains("variant-a", hypothesis.TreatmentConditions);
        Assert.Contains("variant-b", hypothesis.TreatmentConditions);
    }

    [Fact]
    public void DefinedAt_SetsTimestamp()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .DefinedAt(timestamp)
            .Build();

        // Assert
        Assert.Equal(timestamp, hypothesis.DefinedAt);
    }

    [Fact]
    public void DefinedNow_SetsCurrentTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .DefinedNow()
            .Build();

        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(hypothesis.DefinedAt);
        Assert.True(hypothesis.DefinedAt >= before);
        Assert.True(hypothesis.DefinedAt <= after);
    }

    [Fact]
    public void Rationale_SetsRationale()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .Rationale("Based on prior research showing X")
            .Build();

        // Assert
        Assert.Equal("Based on prior research showing X", hypothesis.Rationale);
    }

    [Fact]
    public void WithMetadata_AddsMetadata()
    {
        // Act
        var hypothesis = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary)
            .WithMetadata("analyst", "data-team@company.com")
            .WithMetadata("ticket", "EXP-123")
            .Build();

        // Assert
        Assert.NotNull(hypothesis.Metadata);
        Assert.Equal("data-team@company.com", hypothesis.Metadata["analyst"]);
        Assert.Equal("EXP-123", hypothesis.Metadata["ticket"]);
    }

    [Fact]
    public void Build_ThrowsWithoutNullHypothesis()
    {
        // Arrange
        var builder = new HypothesisBuilder("test")
            .AlternativeHypothesis("H1")
            .PrimaryEndpoint("metric", OutcomeType.Binary);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_ThrowsWithoutAlternativeHypothesis()
    {
        // Arrange
        var builder = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .PrimaryEndpoint("metric", OutcomeType.Binary);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_ThrowsWithoutPrimaryEndpoint()
    {
        // Arrange
        var builder = new HypothesisBuilder("test")
            .NullHypothesis("H0")
            .AlternativeHypothesis("H1");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Constructor_ThrowsOnNullName()
    {
        // ArgumentNullException is thrown when null is passed
        Assert.Throws<ArgumentNullException>(() => new HypothesisBuilder(null!));
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyName()
    {
        Assert.Throws<ArgumentException>(() => new HypothesisBuilder(""));
    }

    [Fact]
    public void Constructor_ThrowsOnWhitespaceName()
    {
        Assert.Throws<ArgumentException>(() => new HypothesisBuilder("   "));
    }
}
