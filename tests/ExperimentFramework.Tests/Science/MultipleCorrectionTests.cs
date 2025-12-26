using ExperimentFramework.Science.Corrections;

namespace ExperimentFramework.Tests.Science;

public class MultipleCorrectionTests
{
    public class BonferroniCorrectionTests
    {
        [Fact]
        public void AdjustPValues_MultipliesByNumberOfTests()
        {
            // Arrange
            var pValues = new double[] { 0.01, 0.02, 0.03, 0.04, 0.05 };

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
            var pValues = new double[] { 0.30, 0.40, 0.50 };

            // Act
            var adjusted = BonferroniCorrection.Instance.AdjustPValues(pValues);

            // Assert
            Assert.True(adjusted.All(p => p <= 1.0));
        }

        [Fact]
        public void DetermineSignificance_UsesDividedAlpha()
        {
            // Arrange
            var pValues = new double[] { 0.005, 0.02, 0.03, 0.04, 0.05 };

            // Act - alpha/5 = 0.01
            var significant = BonferroniCorrection.Instance.DetermineSignificance(pValues, 0.05);

            // Assert - only p = 0.005 is significant (since threshold is 0.01)
            Assert.True(significant[0]); // 0.005 < 0.01
            Assert.False(significant[1]); // 0.02 >= 0.01
        }
    }

    public class HolmBonferroniCorrectionTests
    {
        [Fact]
        public void AdjustPValues_AppliesStepDownProcedure()
        {
            // Arrange - sorted p-values
            var pValues = new double[] { 0.01, 0.02, 0.03 };

            // Act
            var adjusted = HolmBonferroniCorrection.Instance.AdjustPValues(pValues);

            // Assert - first p-value multiplied by 3, second by 2, third by 1
            // But with monotonicity enforcement
            Assert.Equal(0.03, adjusted[0]); // 0.01 * 3
            Assert.Equal(0.04, adjusted[1]); // max(0.03, 0.02 * 2)
            Assert.Equal(0.04, adjusted[2]); // max(0.04, 0.03 * 1)
        }

        [Fact]
        public void DetermineSignificance_StopsAtFirstNonSignificant()
        {
            // Arrange
            var pValues = new double[] { 0.001, 0.02, 0.03 };

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
    }

    public class BenjaminiHochbergCorrectionTests
    {
        [Fact]
        public void AdjustPValues_ProducesQValues()
        {
            // Arrange
            var pValues = new double[] { 0.01, 0.02, 0.03, 0.04, 0.05 };

            // Act
            var adjusted = BenjaminiHochbergCorrection.Instance.AdjustPValues(pValues);

            // Assert - should be less conservative than Bonferroni
            var bonferroni = BonferroniCorrection.Instance.AdjustPValues(pValues);
            Assert.True(adjusted[0] <= bonferroni[0]);
        }

        [Fact]
        public void DetermineSignificance_FindsLargestSignificantK()
        {
            // Arrange
            var pValues = new double[] { 0.005, 0.02, 0.04 };

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
        public void Name_ReturnsBenjaminiHochberg()
        {
            Assert.Equal("Benjamini-Hochberg Procedure", BenjaminiHochbergCorrection.Instance.Name);
            Assert.Equal("False Discovery Rate (FDR)", BenjaminiHochbergCorrection.Instance.ControlsFor);
        }
    }
}
