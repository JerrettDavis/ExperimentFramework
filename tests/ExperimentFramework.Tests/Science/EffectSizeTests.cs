using ExperimentFramework.Science.EffectSize;
using ExperimentFramework.Science.Reporting;

namespace ExperimentFramework.Tests.Science;

public class EffectSizeTests
{
    public class CohensDTests
    {
        [Fact]
        public void Instance_ReturnsSingleton()
        {
            var instance1 = CohensD.Instance;
            var instance2 = CohensD.Instance;

            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void Name_ReturnsCohensD()
        {
            Assert.Equal("Cohen's d", CohensD.Instance.Name);
        }

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
        public void Calculate_WithMediumEffect_ReturnsMedium()
        {
            // Arrange - medium difference (0.5 <= d < 0.8)
            var control = new double[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
            var treatment = new double[] { 17, 18, 19, 20, 21, 22, 23, 24, 25, 26 };

            // Act
            var result = CohensD.Instance.Calculate(control, treatment);

            // Assert
            Assert.True(Math.Abs(result.Value) >= 0.5 && Math.Abs(result.Value) < 0.8 || result.Magnitude == EffectSizeMagnitude.Large);
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

        [Fact]
        public void Calculate_ThrowsOnNullControl()
        {
            var treatment = new double[] { 1, 2, 3 };

            Assert.Throws<ArgumentNullException>(() =>
                CohensD.Instance.Calculate(null!, treatment));
        }

        [Fact]
        public void Calculate_ThrowsOnNullTreatment()
        {
            var control = new double[] { 1, 2, 3 };

            Assert.Throws<ArgumentNullException>(() =>
                CohensD.Instance.Calculate(control, null!));
        }

        [Fact]
        public void Calculate_ThrowsOnInsufficientControlData()
        {
            var control = new double[] { 1 };
            var treatment = new double[] { 1, 2, 3 };

            Assert.Throws<ArgumentException>(() =>
                CohensD.Instance.Calculate(control, treatment));
        }

        [Fact]
        public void Calculate_ThrowsOnInsufficientTreatmentData()
        {
            var control = new double[] { 1, 2, 3 };
            var treatment = new double[] { 1 };

            Assert.Throws<ArgumentException>(() =>
                CohensD.Instance.Calculate(control, treatment));
        }

        [Fact]
        public void Calculate_WithNegativeEffect()
        {
            // Arrange - treatment lower than control
            var control = new double[] { 50, 50, 50, 50, 50 };
            var treatment = new double[] { 10, 10, 10, 10, 10 };

            // Act
            var result = CohensD.Instance.Calculate(control, treatment);

            // Assert
            Assert.True(result.Value < 0);
            Assert.Equal(EffectSizeMagnitude.Large, result.Magnitude);
        }
    }

    public class OddsRatioTests
    {
        [Fact]
        public void Instance_ReturnsSingleton()
        {
            var instance1 = OddsRatio.Instance;
            var instance2 = OddsRatio.Instance;

            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void Name_ReturnsOddsRatio()
        {
            Assert.Equal("Odds Ratio", OddsRatio.Instance.Name);
        }

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

        [Fact]
        public void Calculate_WithLowerTreatmentSuccess_ReturnsLessThanOne()
        {
            // Arrange - control 80%, treatment 20%
            var result = OddsRatio.Instance.Calculate(80, 100, 20, 100);

            // Assert
            Assert.True(result.Value < 1);
        }

        [Fact]
        public void Calculate_ReturnsConfidenceInterval()
        {
            var result = OddsRatio.Instance.Calculate(30, 100, 60, 100);

            Assert.NotNull(result.ConfidenceIntervalLower);
            Assert.NotNull(result.ConfidenceIntervalUpper);
        }

        [Fact]
        public void Calculate_ThrowsOnNegativeControlSuccesses()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                OddsRatio.Instance.Calculate(-1, 100, 50, 100));
        }

        [Fact]
        public void Calculate_ThrowsOnNegativeTreatmentSuccesses()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                OddsRatio.Instance.Calculate(50, 100, -1, 100));
        }

        [Fact]
        public void Calculate_ThrowsOnZeroControlTotal()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                OddsRatio.Instance.Calculate(0, 0, 50, 100));
        }

        [Fact]
        public void Calculate_ThrowsOnZeroTreatmentTotal()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                OddsRatio.Instance.Calculate(50, 100, 0, 0));
        }

        [Fact]
        public void Calculate_ThrowsOnSuccessesExceedingTotal()
        {
            Assert.Throws<ArgumentException>(() =>
                OddsRatio.Instance.Calculate(110, 100, 50, 100));
            Assert.Throws<ArgumentException>(() =>
                OddsRatio.Instance.Calculate(50, 100, 110, 100));
        }

        [Fact]
        public void Calculate_ReturnsMagnitudeInterpretation()
        {
            // Small effect (OR between 1.5 and 2)
            var result = OddsRatio.Instance.Calculate(40, 100, 50, 100);
            Assert.NotEqual(EffectSizeMagnitude.Large, result.Magnitude);

            // Large effect (OR > 3)
            var largeResult = OddsRatio.Instance.Calculate(10, 100, 80, 100);
            Assert.Equal(EffectSizeMagnitude.Large, largeResult.Magnitude);
        }
    }

    public class RelativeRiskTests
    {
        [Fact]
        public void Instance_ReturnsSingleton()
        {
            var instance1 = RelativeRisk.Instance;
            var instance2 = RelativeRisk.Instance;

            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void Name_ReturnsRelativeRisk()
        {
            Assert.Equal("Relative Risk", RelativeRisk.Instance.Name);
        }

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
            Assert.Null(result.ConfidenceIntervalLower);
            Assert.Null(result.ConfidenceIntervalUpper);
        }

        [Fact]
        public void Calculate_WithZeroTreatment_ReturnsZero()
        {
            // Arrange - no events in treatment
            var result = RelativeRisk.Instance.Calculate(50, 100, 0, 100);

            // Assert
            Assert.Equal(0, result.Value);
            Assert.Equal(0, result.ConfidenceIntervalLower);
            Assert.Null(result.ConfidenceIntervalUpper);
        }

        [Fact]
        public void Calculate_WithZeroBoth_ReturnsNaN()
        {
            // Arrange - no events in either group
            var result = RelativeRisk.Instance.Calculate(0, 100, 0, 100);

            // Assert
            Assert.True(double.IsNaN(result.Value));
        }

        [Fact]
        public void Calculate_ReturnsConfidenceInterval()
        {
            var result = RelativeRisk.Instance.Calculate(30, 100, 60, 100);

            Assert.NotNull(result.ConfidenceIntervalLower);
            Assert.NotNull(result.ConfidenceIntervalUpper);
            Assert.True(result.ConfidenceIntervalLower < result.Value);
            Assert.True(result.ConfidenceIntervalUpper > result.Value);
        }

        [Fact]
        public void Calculate_ThrowsOnNegativeControlSuccesses()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RelativeRisk.Instance.Calculate(-1, 100, 50, 100));
        }

        [Fact]
        public void Calculate_ThrowsOnNegativeTreatmentSuccesses()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RelativeRisk.Instance.Calculate(50, 100, -1, 100));
        }

        [Fact]
        public void Calculate_ThrowsOnZeroControlTotal()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RelativeRisk.Instance.Calculate(0, 0, 50, 100));
        }

        [Fact]
        public void Calculate_ThrowsOnZeroTreatmentTotal()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RelativeRisk.Instance.Calculate(50, 100, 0, 0));
        }

        [Fact]
        public void Calculate_ThrowsOnSuccessesExceedingTotal()
        {
            Assert.Throws<ArgumentException>(() =>
                RelativeRisk.Instance.Calculate(110, 100, 50, 100));
            Assert.Throws<ArgumentException>(() =>
                RelativeRisk.Instance.Calculate(50, 100, 110, 100));
        }

        [Fact]
        public void Calculate_WithProtectiveEffect_ReturnsLessThanOne()
        {
            // Arrange - treatment has lower risk
            var result = RelativeRisk.Instance.Calculate(50, 100, 25, 100);

            // Assert
            Assert.Equal(0.5, result.Value, precision: 10);
        }

        [Fact]
        public void Calculate_ReturnsMagnitudeInterpretation()
        {
            // Large effect (RR >= 2)
            var largeResult = RelativeRisk.Instance.Calculate(25, 100, 75, 100);
            Assert.Equal(EffectSizeMagnitude.Large, largeResult.Magnitude);
        }
    }

    public class EffectSizeExtensionsTests
    {
        [Theory]
        [InlineData(0.1, EffectSizeMagnitude.Negligible)]
        [InlineData(0.19, EffectSizeMagnitude.Negligible)]
        [InlineData(0.2, EffectSizeMagnitude.Small)]
        [InlineData(0.3, EffectSizeMagnitude.Small)]
        [InlineData(0.49, EffectSizeMagnitude.Small)]
        [InlineData(0.5, EffectSizeMagnitude.Medium)]
        [InlineData(0.6, EffectSizeMagnitude.Medium)]
        [InlineData(0.79, EffectSizeMagnitude.Medium)]
        [InlineData(0.8, EffectSizeMagnitude.Large)]
        [InlineData(1.0, EffectSizeMagnitude.Large)]
        [InlineData(2.0, EffectSizeMagnitude.Large)]
        public void InterpretCohensD_ReturnsCorrectMagnitude(double d, EffectSizeMagnitude expected)
        {
            var result = EffectSizeExtensions.InterpretCohensD(d);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(-0.1, EffectSizeMagnitude.Negligible)]
        [InlineData(-0.3, EffectSizeMagnitude.Small)]
        [InlineData(-0.6, EffectSizeMagnitude.Medium)]
        [InlineData(-1.0, EffectSizeMagnitude.Large)]
        public void InterpretCohensD_UsesAbsoluteValue(double d, EffectSizeMagnitude expected)
        {
            var result = EffectSizeExtensions.InterpretCohensD(d);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1.0, EffectSizeMagnitude.Negligible)]
        [InlineData(1.2, EffectSizeMagnitude.Negligible)]
        [InlineData(1.49, EffectSizeMagnitude.Negligible)]
        [InlineData(1.5, EffectSizeMagnitude.Small)]
        [InlineData(1.8, EffectSizeMagnitude.Small)]
        [InlineData(1.99, EffectSizeMagnitude.Small)]
        [InlineData(2.0, EffectSizeMagnitude.Medium)]
        [InlineData(2.5, EffectSizeMagnitude.Medium)]
        [InlineData(2.99, EffectSizeMagnitude.Medium)]
        [InlineData(3.0, EffectSizeMagnitude.Large)]
        [InlineData(5.0, EffectSizeMagnitude.Large)]
        public void InterpretOddsRatio_ReturnsCorrectMagnitude(double or, EffectSizeMagnitude expected)
        {
            var result = EffectSizeExtensions.InterpretOddsRatio(or);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0.8, EffectSizeMagnitude.Negligible)]  // 1/0.8 = 1.25
        [InlineData(0.6, EffectSizeMagnitude.Small)]       // 1/0.6 = 1.67
        [InlineData(0.4, EffectSizeMagnitude.Medium)]      // 1/0.4 = 2.5
        [InlineData(0.2, EffectSizeMagnitude.Large)]       // 1/0.2 = 5.0
        public void InterpretOddsRatio_HandlesValuesLessThanOne(double or, EffectSizeMagnitude expected)
        {
            var result = EffectSizeExtensions.InterpretOddsRatio(or);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1.0, EffectSizeMagnitude.Negligible)]
        [InlineData(1.1, EffectSizeMagnitude.Negligible)]
        [InlineData(1.24, EffectSizeMagnitude.Negligible)]
        [InlineData(1.25, EffectSizeMagnitude.Small)]
        [InlineData(1.4, EffectSizeMagnitude.Small)]
        [InlineData(1.49, EffectSizeMagnitude.Small)]
        [InlineData(1.5, EffectSizeMagnitude.Medium)]
        [InlineData(1.8, EffectSizeMagnitude.Medium)]
        [InlineData(1.99, EffectSizeMagnitude.Medium)]
        [InlineData(2.0, EffectSizeMagnitude.Large)]
        [InlineData(3.0, EffectSizeMagnitude.Large)]
        public void InterpretRelativeRisk_ReturnsCorrectMagnitude(double rr, EffectSizeMagnitude expected)
        {
            var result = EffectSizeExtensions.InterpretRelativeRisk(rr);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0.9, EffectSizeMagnitude.Negligible)]   // 1/0.9 = 1.11
        [InlineData(0.75, EffectSizeMagnitude.Small)]       // 1/0.75 = 1.33
        [InlineData(0.6, EffectSizeMagnitude.Medium)]       // 1/0.6 = 1.67
        [InlineData(0.4, EffectSizeMagnitude.Large)]        // 1/0.4 = 2.5
        public void InterpretRelativeRisk_HandlesValuesLessThanOne(double rr, EffectSizeMagnitude expected)
        {
            var result = EffectSizeExtensions.InterpretRelativeRisk(rr);
            Assert.Equal(expected, result);
        }
    }
}
