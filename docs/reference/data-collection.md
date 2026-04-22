# Data Collection

Collect experiment outcomes to enable statistical analysis and publication-ready reports. The `ExperimentFramework.Data` package provides thread-safe, high-performance outcome recording.

## Overview

Data collection captures:

- **Binary outcomes**: Success/failure events (conversions, clicks, purchases)
- **Continuous outcomes**: Numeric measurements (revenue, scores, ratings)
- **Count outcomes**: Integer counts (page views, items added, errors)
- **Duration outcomes**: Time measurements (load time, session duration)

## Installation

```bash
dotnet add package ExperimentFramework.Data
```

## Quick Start

### Basic Setup

```csharp
using ExperimentFramework.Data;

var builder = WebApplication.CreateBuilder(args);

// Register data collection services
builder.Services.AddExperimentDataCollection();

var app = builder.Build();
```

### Recording Outcomes

```csharp
public class CheckoutController : ControllerBase
{
    private readonly IOutcomeRecorder _recorder;

    public CheckoutController(IOutcomeRecorder recorder)
    {
        _recorder = recorder;
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(string userId, string experimentTrial)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Process checkout...
            var success = await ProcessCheckout(userId);
            stopwatch.Stop();

            // Record binary outcome (conversion)
            await _recorder.RecordBinaryAsync(
                experimentName: "checkout-v2",
                trialKey: experimentTrial,
                subjectId: userId,
                metricName: "purchase_completed",
                success: success);

            // Record duration
            await _recorder.RecordDurationAsync(
                experimentName: "checkout-v2",
                trialKey: experimentTrial,
                subjectId: userId,
                metricName: "checkout_duration",
                duration: stopwatch.Elapsed);

            return Ok();
        }
        catch (Exception)
        {
            // Record failure
            await _recorder.RecordBinaryAsync(
                experimentName: "checkout-v2",
                trialKey: experimentTrial,
                subjectId: userId,
                metricName: "purchase_completed",
                success: false);
            throw;
        }
    }
}
```

## Outcome Types

### Binary Outcomes

For success/failure events:

```csharp
// Conversion tracking
await recorder.RecordBinaryAsync(
    "pricing-test",
    "variant-a",
    userId,
    "converted",
    success: true);

// Click tracking
await recorder.RecordBinaryAsync(
    "cta-test",
    "red-button",
    userId,
    "button_clicked",
    success: true);

// Error tracking
await recorder.RecordBinaryAsync(
    "api-migration",
    "new-api",
    userId,
    "request_succeeded",
    success: response.IsSuccessStatusCode);
```

### Continuous Outcomes

For numeric measurements:

```csharp
// Revenue
await recorder.RecordContinuousAsync(
    "checkout-v2",
    "streamlined",
    userId,
    "order_value",
    value: 149.99);

// Scores
await recorder.RecordContinuousAsync(
    "recommendation-test",
    "ml-model-v2",
    userId,
    "relevance_score",
    value: 0.87);

// Ratings
await recorder.RecordContinuousAsync(
    "onboarding-test",
    "interactive",
    userId,
    "satisfaction_rating",
    value: 4.5);
```

### Count Outcomes

For integer counts:

```csharp
// Items in cart
await recorder.RecordCountAsync(
    "cart-optimization",
    "suggested-items",
    userId,
    "items_added",
    count: 3);

// Page views
await recorder.RecordCountAsync(
    "layout-test",
    "single-page",
    userId,
    "pages_viewed",
    count: 1);

// Errors encountered
await recorder.RecordCountAsync(
    "error-handling-test",
    "retry-strategy",
    userId,
    "retry_attempts",
    count: 2);
```

### Duration Outcomes

For time measurements:

```csharp
// Response time
await recorder.RecordDurationAsync(
    "api-optimization",
    "cached",
    userId,
    "response_time",
    duration: TimeSpan.FromMilliseconds(45));

// Session duration
await recorder.RecordDurationAsync(
    "engagement-test",
    "gamified",
    userId,
    "session_length",
    duration: TimeSpan.FromMinutes(12.5));

// Time to first interaction
await recorder.RecordDurationAsync(
    "onboarding-test",
    "tutorial",
    userId,
    "time_to_first_action",
    duration: TimeSpan.FromSeconds(8.3));
```

## Adding Metadata

Include additional context for segmented analysis:

```csharp
await recorder.RecordBinaryAsync(
    "checkout-v2",
    "streamlined",
    userId,
    "purchase_completed",
    success: true,
    metadata: new Dictionary<string, object>
    {
        ["device"] = "mobile",
        ["platform"] = "ios",
        ["region"] = "us-west",
        ["user_segment"] = "power_user",
        ["cart_value_tier"] = "high"
    });
```

Metadata enables:

- **Segmented analysis**: Compare results by user type, device, region
- **Covariate adjustment**: Control for confounding variables
- **Debugging**: Trace issues in specific conditions

## Storage Options

### In-Memory Storage (Default)

Thread-safe storage for development and testing:

```csharp
services.AddExperimentDataCollection();
```

### Custom Storage

Implement `IOutcomeStore` for production:

```csharp
public class PostgresOutcomeStore : IOutcomeStore
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresOutcomeStore(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async ValueTask RecordAsync(
        ExperimentOutcome outcome,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            INSERT INTO experiment_outcomes
            (id, experiment_name, trial_key, subject_id, outcome_type,
             metric_name, value, timestamp, metadata)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
            """;

        cmd.Parameters.AddWithValue(outcome.Id);
        cmd.Parameters.AddWithValue(outcome.ExperimentName);
        cmd.Parameters.AddWithValue(outcome.TrialKey);
        cmd.Parameters.AddWithValue(outcome.SubjectId);
        cmd.Parameters.AddWithValue(outcome.OutcomeType.ToString());
        cmd.Parameters.AddWithValue(outcome.MetricName);
        cmd.Parameters.AddWithValue(outcome.Value);
        cmd.Parameters.AddWithValue(outcome.Timestamp);
        cmd.Parameters.AddWithValue(
            outcome.Metadata != null
                ? JsonSerializer.Serialize(outcome.Metadata)
                : DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<ExperimentOutcome>> QueryAsync(
        OutcomeQuery query,
        CancellationToken cancellationToken = default)
    {
        // Implementation...
    }

    public async ValueTask<IReadOnlyDictionary<string, OutcomeAggregation>> GetAggregationsAsync(
        string experimentName,
        string metricName,
        CancellationToken cancellationToken = default)
    {
        // Implementation...
    }
}

// Register custom store
services.AddExperimentDataCollection<PostgresOutcomeStore>();
```

### No-Op Storage

Zero-overhead when data collection should be disabled:

```csharp
// Production environment without analysis
if (env.IsProduction() && !config.GetValue<bool>("EnableExperimentAnalysis"))
{
    services.AddExperimentDataCollectionNoop();
}
else
{
    services.AddExperimentDataCollection();
}
```

## Querying Outcomes

### Basic Query

```csharp
public class AnalysisService
{
    private readonly IOutcomeStore _store;

    public AnalysisService(IOutcomeStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<ExperimentOutcome>> GetConversions(
        string experimentName)
    {
        var query = new OutcomeQuery
        {
            ExperimentName = experimentName,
            MetricName = "purchase_completed"
        };

        return await _store.QueryAsync(query);
    }
}
```

### Filtered Query

```csharp
var query = new OutcomeQuery
{
    ExperimentName = "checkout-v2",
    TrialKey = "streamlined",
    MetricName = "purchase_completed",
    FromTimestamp = DateTimeOffset.UtcNow.AddDays(-7),
    ToTimestamp = DateTimeOffset.UtcNow
};

var recentOutcomes = await store.QueryAsync(query);
```

### Aggregations

Get summary statistics per trial:

```csharp
var aggregations = await store.GetAggregationsAsync(
    "checkout-v2",
    "purchase_completed");

foreach (var (trialKey, agg) in aggregations)
{
    Console.WriteLine($"{trialKey}: {agg.SuccessCount}/{agg.Count} = {agg.ConversionRate:P1}");
}

// Output:
// control: 145/500 = 29.0%
// streamlined: 187/500 = 37.4%
```

## Configuration Options

```csharp
services.AddExperimentDataCollection(options =>
{
    // Auto-generate IDs if not provided (default: true)
    options.AutoGenerateIds = true;

    // Automatically set timestamps if not provided (default: true)
    options.AutoSetTimestamps = true;

    // Collect invocation duration automatically via decorator (default: true)
    options.CollectDuration = true;

    // Collect error information automatically via decorator (default: true)
    options.CollectErrors = true;

    // Custom metric names
    options.DurationMetricName = "duration_seconds";
    options.SuccessMetricName = "success";
    options.ErrorMetricName = "error";

    // Enable batching for high-throughput scenarios (default: false)
    options.EnableBatching = false;
    options.MaxBatchSize = 100;
    options.BatchFlushInterval = TimeSpan.FromSeconds(5);
});
```

## Best Practices

### 1. Record at Point of Action

Record outcomes immediately when the action occurs:

```csharp
// Good - record immediately
public async Task<IActionResult> Purchase(string userId)
{
    var result = await _checkout.ProcessAsync(userId);

    await _recorder.RecordBinaryAsync(
        "checkout-v2", trialKey, userId,
        "purchase_completed", result.Success);

    return Ok(result);
}

// Bad - deferred recording can lose data
public async Task<IActionResult> Purchase(string userId)
{
    var result = await _checkout.ProcessAsync(userId);
    _pendingOutcomes.Add(new PendingOutcome(...)); // May be lost
    return Ok(result);
}
```

### 2. Use Consistent Subject IDs

Use stable identifiers for subjects:

```csharp
// Good - stable user ID
await recorder.RecordBinaryAsync("test", trial, user.Id, "converted", true);

// Bad - session ID changes between visits
await recorder.RecordBinaryAsync("test", trial, HttpContext.Session.Id, "converted", true);
```

### 3. Name Metrics Clearly

Use descriptive, hierarchical metric names:

```csharp
// Good
"checkout.purchase_completed"
"checkout.cart_abandoned"
"search.results_clicked"

// Bad
"converted"
"success"
"metric1"
```

### 4. Handle Errors

Always record outcomes, even on failure:

```csharp
try
{
    await ProcessPayment();
    await recorder.RecordBinaryAsync(..., success: true);
}
catch (PaymentException)
{
    await recorder.RecordBinaryAsync(..., success: false);
    throw;
}
```

### 5. Test Data Collection

Verify outcomes are recorded correctly:

```csharp
[Fact]
public async Task Checkout_RecordsPurchaseOutcome()
{
    // Arrange
    var store = new InMemoryOutcomeStore();
    var recorder = new OutcomeRecorder(store);
    var controller = new CheckoutController(recorder);

    // Act
    await controller.Checkout("user-123", "variant-a");

    // Assert
    var outcomes = await store.QueryAsync(new OutcomeQuery
    {
        ExperimentName = "checkout-v2",
        SubjectId = "user-123"
    });

    Assert.Single(outcomes);
    Assert.Equal(1.0, outcomes[0].Value); // Success
}
```

## Integration with Analysis

Data collection feeds into the Science package for analysis:

```csharp
// Collect data
services.AddExperimentDataCollection();
services.AddExperimentScience();

// Later, analyze
var analyzer = serviceProvider.GetRequiredService<IExperimentAnalyzer>();
var report = await analyzer.AnalyzeAsync("checkout-v2", new AnalysisOptions
{
    Alpha = 0.05,
    TargetPower = 0.80,
    ApplyMultipleComparisonCorrection = true,
    CorrectionMethod = MultipleComparisonMethod.BenjaminiHochberg
});

// Generate report
var reporter = serviceProvider.GetRequiredService<IExperimentReporter>();
var markdown = await reporter.GenerateAsync(report);
```

## See Also

- [Statistical Analysis](statistical-analysis.md) - Analyze collected data
- [Hypothesis Testing](hypothesis-testing.md) - Define and test hypotheses
- [Power Analysis](power-analysis.md) - Calculate required sample sizes
- [Metrics](metrics.md) - Real-time operational metrics
