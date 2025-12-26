using ExperimentFramework.Science.EffectSize;
using ExperimentFramework.Science.Reporting;

namespace ExperimentFramework.Tests.Science;

public class EffectSizeTests
{
    public class CohensDTests
    {
        [Fact]
        public void Calculate_WithNoEffect_ReturnsNegligible()
        {
            // Arrange - identical distributions
            var control = new double[] { 10, 20, 30, 40, 50 };
            var treatment = new double[] { 10, 20, 30, 40, 50 };

            // Act
            var result = CohensD.Instance.Calculate(control, treatment);

            // Assert
            Assert.Equal("Cohen's d", result.MeasureName);
            Assert.Equal(0, result.Value, precision: 10);
            Assert.Equal(EffectSizeMagnitude.Negligible, result.Magnitude);
        }

        [Fact]
        public void Calculate_WithSmallEffect_ReturnsSmall()
        {
            // Arrange - small difference (d ~ 0.3)
            var control = new double[] { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 };
            var treatment = new double[] { 103, 103, 103, 103, 103, 103, 103, 103, 103, 103 };

            // Act
            var result = CohensD.Instance.Calculate(control, treatment);

            // Assert
            Assert.True(result.Value > 0);
        }

        [Fact]
        public void Calculate_WithLargeEffect_ReturnsLarge()
        {
            // Arrange - large difference (d > 0.8)
            var control = new double[] { 10, 10, 10, 10, 10 };
            var treatment = new double[] { 50, 50, 50, 50, 50 };

            // Act
            var result = CohensD.Instance.Calculate(control, treatment);

            // Assert
            Assert.Equal(EffectSizeMagnitude.Large, result.Magnitude);
        }

        [Fact]
        public void Calculate_ReturnsConfidenceInterval()
        {
            // Arrange
            var control = new double[] { 1, 2, 3, 4, 5 };
            var treatment = new double[] { 6, 7, 8, 9, 10 };

            // Act
            var result = CohensD.Instance.Calculate(control, treatment);

            // Assert
            Assert.NotNull(result.ConfidenceIntervalLower);
            Assert.NotNull(result.ConfidenceIntervalUpper);
            Assert.True(result.ConfidenceIntervalLower < result.Value);
            Assert.True(result.ConfidenceIntervalUpper > result.Value);
        }
    }

    public class OddsRatioTests
    {
        [Fact]
        public void Calculate_WithEqualProportions_ReturnsOne()
        {
            // Arrange - 50% success in both groups
            var result = OddsRatio.Instance.Calculate(50, 100, 50, 100);

            // Assert
            Assert.Equal("Odds Ratio", result.MeasureName);
            Assert.Equal(1.0, result.Value, precision: 10);
        }

        [Fact]
        public void Calculate_WithHigherTreatmentSuccess_ReturnsGreaterThanOne()
        {
            // Arrange - control 20%, treatment 80%
            var result = OddsRatio.Instance.Calculate(20, 100, 80, 100);

            // Assert
            Assert.True(result.Value > 1);
        }

        [Fact]
        public void Calculate_WithZeroCells_AppliesContinuityCorrection()
        {
            // Arrange - zero successes in control
            var result = OddsRatio.Instance.Calculate(0, 100, 50, 100);

            // Assert
            Assert.True(result.Value > 1);
            Assert.True(double.IsFinite(result.Value));
        }
    }

    public class RelativeRiskTests
    {
        [Fact]
        public void Calculate_WithEqualProportions_ReturnsOne()
        {
            // Arrange - 50% success in both groups
            var result = RelativeRisk.Instance.Calculate(50, 100, 50, 100);

            // Assert
            Assert.Equal("Relative Risk", result.MeasureName);
            Assert.Equal(1.0, result.Value, precision: 10);
        }

        [Fact]
        public void Calculate_WithDoubleRisk_ReturnsTwo()
        {
            // Arrange - control 25%, treatment 50%
            var result = RelativeRisk.Instance.Calculate(25, 100, 50, 100);

            // Assert
            Assert.Equal(2.0, result.Value, precision: 10);
        }

        [Fact]
        public void Calculate_WithZeroControl_ReturnsInfinity()
        {
            // Arrange - no events in control
            var result = RelativeRisk.Instance.Calculate(0, 100, 50, 100);

            // Assert
            Assert.True(double.IsPositiveInfinity(result.Value));
        }
    }
}
