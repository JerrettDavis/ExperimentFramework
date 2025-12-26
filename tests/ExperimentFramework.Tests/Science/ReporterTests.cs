using ExperimentFramework.Science.Models.Results;
using ExperimentFramework.Science.Reporting;

namespace ExperimentFramework.Tests.Science;

public class ReporterTests
{
    private static ExperimentReport CreateSampleReport(
        ExperimentStatus status = ExperimentStatus.Completed,
        ExperimentConclusion conclusion = ExperimentConclusion.TreatmentWins,
        StatisticalTestResult? primaryResult = null,
        EffectSizeResult? effectSize = null,
        PowerAnalysisResult? powerAnalysis = null,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? recommendations = null,
        IReadOnlyDictionary<string, ConditionSummary>? conditionSummaries = null,
        bool useDefaultOptional = true)
    {
        return new ExperimentReport
        {
            ExperimentName = "test-experiment",
            AnalyzedAt = DateTimeOffset.UtcNow,
            Status = status,
            Conclusion = conclusion,
            SampleSizes = new Dictionary<string, int>
            {
                ["control"] = 100,
                ["treatment"] = 100
            },
            PrimaryResult = useDefaultOptional ? (primaryResult ?? new StatisticalTestResult
            {
                TestName = "Welch's Two-Sample t-Test",
                TestStatistic = 2.5,
                PValue = 0.01,
                Alpha = 0.05,
                PointEstimate = 0.15,
                ConfidenceIntervalLower = 0.05,
                ConfidenceIntervalUpper = 0.25,
                DegreesOfFreedom = 198,
                SampleSizes = new Dictionary<string, int>
                {
                    ["control"] = 100,
                    ["treatment"] = 100
                }
            }) : primaryResult,
            EffectSize = useDefaultOptional ? (effectSize ?? new EffectSizeResult
            {
                MeasureName = "Cohen's d",
                Value = 0.5,
                Magnitude = EffectSizeMagnitude.Medium,
                ConfidenceIntervalLower = 0.2,
                ConfidenceIntervalUpper = 0.8
            }) : effectSize,
            PowerAnalysis = useDefaultOptional ? (powerAnalysis ?? new PowerAnalysisResult
            {
                AchievedPower = 0.85,
                CurrentSampleSize = 200,
                RequiredSampleSize = 150,
                IsAdequatelyPowered = true,
                Alpha = 0.05,
                TargetPower = 0.80
            }) : powerAnalysis,
            Warnings = warnings,
            Recommendations = recommendations,
            ConditionSummaries = conditionSummaries
        };
    }

    public class MarkdownReporterTests
    {
        private readonly MarkdownReporter _reporter = new();

        [Fact]
        public async Task GenerateAsync_ReturnsMarkdown()
        {
            // Arrange
            var report = CreateSampleReport();

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.NotEmpty(markdown);
            Assert.Contains("test-experiment", markdown);
        }

        [Fact]
        public async Task GenerateAsync_IncludesExperimentName()
        {
            // Arrange
            var report = CreateSampleReport();

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.Contains("# Experiment Report: test-experiment", markdown);
        }

        [Fact]
        public async Task GenerateAsync_IncludesStatus()
        {
            // Arrange
            var report = CreateSampleReport(ExperimentStatus.Completed);

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.Contains("Completed", markdown);
        }

        [Fact]
        public async Task GenerateAsync_IncludesConclusion()
        {
            // Arrange
            var report = CreateSampleReport(conclusion: ExperimentConclusion.TreatmentWins);

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.Contains("Treatment wins", markdown);
        }

        [Fact]
        public async Task GenerateAsync_IncludesPrimaryResult()
        {
            // Arrange
            var report = CreateSampleReport();

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.Contains("Primary Analysis", markdown);
            Assert.Contains("p-value", markdown);
        }

        [Fact]
        public async Task GenerateAsync_IncludesEffectSize()
        {
            // Arrange
            var report = CreateSampleReport();

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.Contains("Effect Size", markdown);
            Assert.Contains("Cohen's d", markdown);
        }

        [Fact]
        public async Task GenerateAsync_IncludesPowerAnalysis()
        {
            // Arrange
            var report = CreateSampleReport();

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.Contains("Power Analysis", markdown);
            Assert.Contains("85", markdown); // 85% power
        }

        [Fact]
        public async Task GenerateAsync_HandlesNoSignificantDifference()
        {
            // Arrange
            var report = CreateSampleReport(conclusion: ExperimentConclusion.NoSignificantDifference);

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.Contains("No significant difference", markdown);
        }

        [Fact]
        public async Task GenerateAsync_HandlesInconclusive()
        {
            // Arrange
            var report = CreateSampleReport(
                status: ExperimentStatus.Running,
                conclusion: ExperimentConclusion.Inconclusive);

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.Contains("Running", markdown);
            Assert.Contains("Inconclusive", markdown);
        }

        [Fact]
        public async Task GenerateAsync_IncludesWarnings()
        {
            // Arrange
            var report = CreateSampleReport(
                warnings: new List<string> { "Sample size is small", "Effect may be inflated" });

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.Contains("Warnings", markdown);
            Assert.Contains("Sample size is small", markdown);
        }

        [Fact]
        public async Task GenerateAsync_IncludesRecommendations()
        {
            // Arrange
            var report = CreateSampleReport(
                recommendations: new List<string> { "Consider rolling out", "Monitor for regression" });

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.Contains("Recommendations", markdown);
            Assert.Contains("Consider rolling out", markdown);
        }

        [Fact]
        public async Task GenerateAsync_HandlesNullPrimaryResult()
        {
            // Arrange
            var report = CreateSampleReport(primaryResult: null, useDefaultOptional: false);

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.NotEmpty(markdown);
            Assert.Contains("test-experiment", markdown);
        }

        [Fact]
        public async Task GenerateAsync_HandlesNullEffectSize()
        {
            // Arrange
            var report = CreateSampleReport(effectSize: null, useDefaultOptional: false);

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.NotEmpty(markdown);
        }

        [Fact]
        public async Task GenerateAsync_HandlesNullPowerAnalysis()
        {
            // Arrange
            var report = CreateSampleReport(powerAnalysis: null, useDefaultOptional: false);

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.NotEmpty(markdown);
        }

        [Fact]
        public async Task GenerateAsync_IncludesConditionSummaries()
        {
            // Arrange
            var report = CreateSampleReport(
                conditionSummaries: new Dictionary<string, ConditionSummary>
                {
                    ["control"] = new ConditionSummary
                    {
                        Condition = "control",
                        SampleSize = 100,
                        Mean = 50.0,
                        StandardDeviation = 10.0,
                        SuccessRate = 0.25
                    },
                    ["treatment"] = new ConditionSummary
                    {
                        Condition = "treatment",
                        SampleSize = 100,
                        Mean = 55.0,
                        StandardDeviation = 12.0,
                        SuccessRate = 0.35
                    }
                });

            // Act
            var markdown = await _reporter.GenerateAsync(report);

            // Assert
            Assert.Contains("control", markdown);
            Assert.Contains("treatment", markdown);
        }
    }

    public class JsonReporterTests
    {
        private readonly JsonReporter _reporter = new();

        [Fact]
        public async Task GenerateAsync_ReturnsValidJson()
        {
            // Arrange
            var report = CreateSampleReport();

            // Act
            var json = await _reporter.GenerateAsync(report);

            // Assert
            Assert.NotEmpty(json);
            Assert.StartsWith("{", json.Trim());
            Assert.EndsWith("}", json.Trim());
        }

        [Fact]
        public async Task GenerateAsync_IncludesExperimentName()
        {
            // Arrange
            var report = CreateSampleReport();

            // Act
            var json = await _reporter.GenerateAsync(report);

            // Assert - camelCase in JSON output
            Assert.Contains("\"experimentName\"", json);
            Assert.Contains("test-experiment", json);
        }

        [Fact]
        public async Task GenerateAsync_IncludesSampleSizes()
        {
            // Arrange
            var report = CreateSampleReport();

            // Act
            var json = await _reporter.GenerateAsync(report);

            // Assert - camelCase in JSON output
            Assert.Contains("\"sampleSizes\"", json);
            Assert.Contains("\"control\"", json);
            Assert.Contains("\"treatment\"", json);
        }

        [Fact]
        public async Task GenerateAsync_IsDeserializable()
        {
            // Arrange
            var report = CreateSampleReport();

            // Act
            var json = await _reporter.GenerateAsync(report);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<ExperimentReport>(json, options);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("test-experiment", deserialized.ExperimentName);
        }

        [Fact]
        public async Task GenerateAsync_HandlesNullOptionalFields()
        {
            // Arrange
            var report = CreateSampleReport(
                primaryResult: null,
                effectSize: null,
                powerAnalysis: null,
                warnings: null,
                recommendations: null,
                useDefaultOptional: false);

            // Act
            var json = await _reporter.GenerateAsync(report);

            // Assert
            Assert.NotEmpty(json);
            // Verify JSON structure is valid
            Assert.StartsWith("{", json.Trim());
            Assert.EndsWith("}", json.Trim());
            // Deserialize with case-insensitive options and enum converter
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<ExperimentReport>(json, options);
            Assert.NotNull(deserialized);
        }
    }
}
