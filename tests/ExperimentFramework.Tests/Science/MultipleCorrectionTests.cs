using ExperimentFramework.Science.Corrections;

namespace ExperimentFramework.Tests.Science;

public class MultipleCorrectionTests
{
    public class BonferroniCorrectionTests
    {
        [Fact]
        public void Instance_ReturnsSingleton()
        {
            var instance1 = BonferroniCorrection.Instance;
            var instance2 = BonferroniCorrection.Instance;

            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void Name_ReturnsBonferroniCorrection()
        {
            Assert.Equal("Bonferroni Correction", BonferroniCorrection.Instance.Name);
        }

        [Fact]
        public void ControlsFor_ReturnsFWER()
        {
            Assert.Equal("Family-wise Error Rate (FWER)", BonferroniCorrection.Instance.ControlsFor);
        }

        [Fact]
        public void AdjustPValues_MultipliesByNumberOfTests()
        {
            // Arrange
            var pValues = new[] { 0.01, 0.02, 0.03, 0.04, 0.05 };

            // Act
            var adjusted = BonferroniCorrection.Instance.AdjustPValues(pValues);

            // Assert
            Assert.Equal(0.05, adjusted[0]); // 0.01 * 5
            Assert.Equal(0.10, adjusted[1]); // 0.02 * 5
        }

        [Fact]
        public void AdjustPValues_CapsAtOne()
        {
            // Arrange
            var pValues = new[] { 0.30, 0.40, 0.50 };

            // Act
            var adjusted = BonferroniCorrection.Instance.AdjustPValues(pValues);

            // Assert
            Assert.True(adjusted.All(p => p <= 1.0));
        }

        [Fact]
        public void AdjustPValues_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BonferroniCorrection.Instance.AdjustPValues(null!));
        }

        [Fact]
        public void AdjustPValues_ReturnsEmptyForEmptyInput()
        {
            var adjusted = BonferroniCorrection.Instance.AdjustPValues(Array.Empty<double>());

            Assert.Empty(adjusted);
        }

        [Fact]
        public void AdjustThresholds_DividesAlphaByNumberOfTests()
        {
            var thresholds = BonferroniCorrection.Instance.AdjustThresholds(0.05, 5);

            Assert.Equal(5, thresholds.Count);
            Assert.All(thresholds, t => Assert.Equal(0.01, t));
        }

        [Fact]
        public void AdjustThresholds_ThrowsOnInvalidAlpha()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BonferroniCorrection.Instance.AdjustThresholds(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BonferroniCorrection.Instance.AdjustThresholds(1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BonferroniCorrection.Instance.AdjustThresholds(-0.1, 5));
        }

        [Fact]
        public void AdjustThresholds_ThrowsOnInvalidNumberOfTests()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BonferroniCorrection.Instance.AdjustThresholds(0.05, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BonferroniCorrection.Instance.AdjustThresholds(0.05, -1));
        }

        [Fact]
        public void DetermineSignificance_UsesDividedAlpha()
        {
            // Arrange
            var pValues = new[] { 0.005, 0.02, 0.03, 0.04, 0.05 };

            // Act - alpha/5 = 0.01
            var significant = BonferroniCorrection.Instance.DetermineSignificance(pValues, 0.05);

            // Assert - only p = 0.005 is significant (since threshold is 0.01)
            Assert.True(significant[0]); // 0.005 < 0.01
            Assert.False(significant[1]); // 0.02 >= 0.01
        }

        [Fact]
        public void DetermineSignificance_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BonferroniCorrection.Instance.DetermineSignificance(null!, 0.05));
        }

        [Fact]
        public void DetermineSignificance_ReturnsEmptyForEmptyInput()
        {
            var significant = BonferroniCorrection.Instance.DetermineSignificance(Array.Empty<double>(), 0.05);

            Assert.Empty(significant);
        }

        [Fact]
        public void DetermineSignificance_AllNotSignificant()
        {
            var pValues = new[] { 0.5, 0.6, 0.7 };

            var significant = BonferroniCorrection.Instance.DetermineSignificance(pValues, 0.05);

            Assert.All(significant, s => Assert.False(s));
        }
    }

    public class HolmBonferroniCorrectionTests
    {
        [Fact]
        public void Instance_ReturnsSingleton()
        {
            var instance1 = HolmBonferroniCorrection.Instance;
            var instance2 = HolmBonferroniCorrection.Instance;

            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void Name_ReturnsHolmBonferroniCorrection()
        {
            Assert.Equal("Holm-Bonferroni Correction", HolmBonferroniCorrection.Instance.Name);
        }

        [Fact]
        public void ControlsFor_ReturnsFWER()
        {
            Assert.Equal("Family-wise Error Rate (FWER)", HolmBonferroniCorrection.Instance.ControlsFor);
        }

        [Fact]
        public void AdjustPValues_AppliesStepDownProcedure()
        {
            // Arrange - sorted p-values
            var pValues = new[] { 0.01, 0.02, 0.03 };

            // Act
            var adjusted = HolmBonferroniCorrection.Instance.AdjustPValues(pValues);

            // Assert - first p-value multiplied by 3, second by 2, third by 1
            // But with monotonicity enforcement
            Assert.Equal(0.03, adjusted[0]); // 0.01 * 3
            Assert.Equal(0.04, adjusted[1]); // max(0.03, 0.02 * 2)
            Assert.Equal(0.04, adjusted[2]); // max(0.04, 0.03 * 1)
        }

        [Fact]
        public void AdjustPValues_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                HolmBonferroniCorrection.Instance.AdjustPValues(null!));
        }

        [Fact]
        public void AdjustPValues_ReturnsEmptyForEmptyInput()
        {
            var adjusted = HolmBonferroniCorrection.Instance.AdjustPValues(Array.Empty<double>());

            Assert.Empty(adjusted);
        }

        [Fact]
        public void AdjustPValues_HandlesUnsortedInput()
        {
            // Arrange - unsorted p-values
            var pValues = new[] { 0.03, 0.01, 0.02 };

            // Act
            var adjusted = HolmBonferroniCorrection.Instance.AdjustPValues(pValues);

            // Assert - should handle correctly regardless of order
            Assert.Equal(3, adjusted.Count);
            Assert.True(adjusted[1] <= adjusted[0]); // Index 1 (0.01) should have smallest adjusted
        }

        [Fact]
        public void AdjustPValues_CapsAtOne()
        {
            var pValues = new[] { 0.5, 0.6, 0.7 };

            var adjusted = HolmBonferroniCorrection.Instance.AdjustPValues(pValues);

            Assert.All(adjusted, p => Assert.True(p <= 1.0));
        }

        [Fact]
        public void AdjustThresholds_IncreasingThresholds()
        {
            var thresholds = HolmBonferroniCorrection.Instance.AdjustThresholds(0.05, 3);

            Assert.Equal(3, thresholds.Count);
            // 0.05/3, 0.05/2, 0.05/1
            Assert.True(thresholds[0] < thresholds[1]);
            Assert.True(thresholds[1] < thresholds[2]);
        }

        [Fact]
        public void AdjustThresholds_ThrowsOnInvalidAlpha()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HolmBonferroniCorrection.Instance.AdjustThresholds(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HolmBonferroniCorrection.Instance.AdjustThresholds(1, 5));
        }

        [Fact]
        public void AdjustThresholds_ThrowsOnInvalidNumberOfTests()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HolmBonferroniCorrection.Instance.AdjustThresholds(0.05, 0));
        }

        [Fact]
        public void DetermineSignificance_StopsAtFirstNonSignificant()
        {
            // Arrange
            var pValues = new[] { 0.001, 0.02, 0.03 };

            // Act
            var significant = HolmBonferroniCorrection.Instance.DetermineSignificance(pValues, 0.05);

            // Assert
            // Step 1: 0.001 < 0.05/3 = 0.0167? Yes
            // Step 2: 0.02 < 0.05/2 = 0.025? Yes
            // Step 3: 0.03 < 0.05/1 = 0.05? Yes
            Assert.True(significant[0]);
            Assert.True(significant[1]);
            Assert.True(significant[2]);
        }

        [Fact]
        public void DetermineSignificance_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                HolmBonferroniCorrection.Instance.DetermineSignificance(null!, 0.05));
        }

        [Fact]
        public void DetermineSignificance_ReturnsEmptyForEmptyInput()
        {
            var significant = HolmBonferroniCorrection.Instance.DetermineSignificance(Array.Empty<double>(), 0.05);

            Assert.Empty(significant);
        }

        [Fact]
        public void DetermineSignificance_StopsCorrectly()
        {
            // Arrange - second p-value will fail the threshold
            var pValues = new[] { 0.001, 0.03, 0.04 };

            // Act
            var significant = HolmBonferroniCorrection.Instance.DetermineSignificance(pValues, 0.05);

            // Step 1: 0.001 < 0.05/3 = 0.0167? Yes
            // Step 2: 0.03 < 0.05/2 = 0.025? No -> stop
            Assert.True(significant[0]);
            Assert.False(significant[1]);
            Assert.False(significant[2]);
        }
    }

    public class BenjaminiHochbergCorrectionTests
    {
        [Fact]
        public void Instance_ReturnsSingleton()
        {
            var instance1 = BenjaminiHochbergCorrection.Instance;
            var instance2 = BenjaminiHochbergCorrection.Instance;

            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void Name_ReturnsBenjaminiHochberg()
        {
            Assert.Equal("Benjamini-Hochberg Procedure", BenjaminiHochbergCorrection.Instance.Name);
        }

        [Fact]
        public void ControlsFor_ReturnsFDR()
        {
            Assert.Equal("False Discovery Rate (FDR)", BenjaminiHochbergCorrection.Instance.ControlsFor);
        }

        [Fact]
        public void AdjustPValues_ProducesQValues()
        {
            // Arrange
            var pValues = new[] { 0.01, 0.02, 0.03, 0.04, 0.05 };

            // Act
            var adjusted = BenjaminiHochbergCorrection.Instance.AdjustPValues(pValues);

            // Assert - should be less conservative than Bonferroni
            var bonferroni = BonferroniCorrection.Instance.AdjustPValues(pValues);
            Assert.True(adjusted[0] <= bonferroni[0]);
        }

        [Fact]
        public void AdjustPValues_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BenjaminiHochbergCorrection.Instance.AdjustPValues(null!));
        }

        [Fact]
        public void AdjustPValues_ReturnsEmptyForEmptyInput()
        {
            var adjusted = BenjaminiHochbergCorrection.Instance.AdjustPValues(Array.Empty<double>());

            Assert.Empty(adjusted);
        }

        [Fact]
        public void AdjustPValues_CapsAtOne()
        {
            var pValues = new[] { 0.5, 0.6, 0.7 };

            var adjusted = BenjaminiHochbergCorrection.Instance.AdjustPValues(pValues);

            Assert.All(adjusted, p => Assert.True(p <= 1.0));
        }

        [Fact]
        public void AdjustPValues_EnforcesMonotonicity()
        {
            // Adjusted p-values should be monotonically non-decreasing in rank order
            var pValues = new[] { 0.01, 0.015, 0.03, 0.04 };

            var adjusted = BenjaminiHochbergCorrection.Instance.AdjustPValues(pValues);

            // Verify monotonicity by checking sorted order
            Assert.True(adjusted[0] <= adjusted[1]);
            Assert.True(adjusted[1] <= adjusted[2]);
            Assert.True(adjusted[2] <= adjusted[3]);
        }

        [Fact]
        public void AdjustThresholds_IncreasingThresholds()
        {
            var thresholds = BenjaminiHochbergCorrection.Instance.AdjustThresholds(0.05, 5);

            Assert.Equal(5, thresholds.Count);
            // (1/5)*0.05, (2/5)*0.05, etc.
            // Use precision parameter to handle floating-point rounding
            Assert.Equal(0.01, thresholds[0], precision: 10);
            Assert.Equal(0.02, thresholds[1], precision: 10);
            Assert.Equal(0.03, thresholds[2], precision: 10);
            Assert.Equal(0.04, thresholds[3], precision: 10);
            Assert.Equal(0.05, thresholds[4], precision: 10);
        }

        [Fact]
        public void AdjustThresholds_ThrowsOnInvalidAlpha()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BenjaminiHochbergCorrection.Instance.AdjustThresholds(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BenjaminiHochbergCorrection.Instance.AdjustThresholds(1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BenjaminiHochbergCorrection.Instance.AdjustThresholds(1.5, 5));
        }

        [Fact]
        public void AdjustThresholds_ThrowsOnInvalidNumberOfTests()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BenjaminiHochbergCorrection.Instance.AdjustThresholds(0.05, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BenjaminiHochbergCorrection.Instance.AdjustThresholds(0.05, -1));
        }

        [Fact]
        public void DetermineSignificance_FindsLargestSignificantK()
        {
            // Arrange
            var pValues = new[] { 0.005, 0.02, 0.04 };

            // Act
            var significant = BenjaminiHochbergCorrection.Instance.DetermineSignificance(pValues, 0.05);

            // Assert
            // k=1: 0.005 <= 0.05 * 1/3 = 0.0167? Yes
            // k=2: 0.02 <= 0.05 * 2/3 = 0.0333? Yes
            // k=3: 0.04 <= 0.05 * 3/3 = 0.05? Yes
            Assert.True(significant[0]);
            Assert.True(significant[1]);
            Assert.True(significant[2]);
        }

        [Fact]
        public void DetermineSignificance_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BenjaminiHochbergCorrection.Instance.DetermineSignificance(null!, 0.05));
        }

        [Fact]
        public void DetermineSignificance_ReturnsEmptyForEmptyInput()
        {
            var significant = BenjaminiHochbergCorrection.Instance.DetermineSignificance(Array.Empty<double>(), 0.05);

            Assert.Empty(significant);
        }

        [Fact]
        public void DetermineSignificance_NoSignificantResults()
        {
            var pValues = new[] { 0.5, 0.6, 0.7 };

            var significant = BenjaminiHochbergCorrection.Instance.DetermineSignificance(pValues, 0.05);

            Assert.All(significant, s => Assert.False(s));
        }

        [Fact]
        public void DetermineSignificance_PartialSignificance()
        {
            // Only first two should be significant
            var pValues = new[] { 0.01, 0.03, 0.5 };

            var significant = BenjaminiHochbergCorrection.Instance.DetermineSignificance(pValues, 0.05);

            // k=1: 0.01 <= 0.0167? Yes
            // k=2: 0.03 <= 0.0333? Yes
            // k=3: 0.5 <= 0.05? No
            // Largest k is 2, so first two are significant
            Assert.True(significant[0]);
            Assert.True(significant[1]);
            Assert.False(significant[2]);
        }

        [Fact]
        public void DetermineSignificance_HandlesSinglePValue()
        {
            var pValues = new[] { 0.03 };

            var significant = BenjaminiHochbergCorrection.Instance.DetermineSignificance(pValues, 0.05);

            Assert.Single(significant);
            Assert.True(significant[0]); // 0.03 <= 0.05 * 1/1
        }

        [Fact]
        public void IsLessPowerfulThanBonferroni()
        {
            // BH should identify more significant results than Bonferroni
            var pValues = new[] { 0.005, 0.02, 0.03 };

            var bhSignificant = BenjaminiHochbergCorrection.Instance.DetermineSignificance(pValues, 0.05);
            var bonSignificant = BonferroniCorrection.Instance.DetermineSignificance(pValues, 0.05);

            var bhCount = bhSignificant.Count(s => s);
            var bonCount = bonSignificant.Count(s => s);

            Assert.True(bhCount >= bonCount);
        }
    }
}
