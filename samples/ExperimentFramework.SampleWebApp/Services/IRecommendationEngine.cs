namespace ExperimentFramework.SampleWebApp.Services;

/// <summary>
/// Service that provides product recommendations using different algorithms.
/// This demonstrates A/B testing different recommendation strategies.
/// </summary>
public interface IRecommendationEngine
{
    /// <summary>
    /// Gets product recommendations for a user.
    /// </summary>
    Task<IEnumerable<ProductRecommendation>> GetRecommendationsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the name of the algorithm being used.
    /// </summary>
    string GetAlgorithmName();
}

public record ProductRecommendation(string ProductId, string Name, decimal Score);

/// <summary>
/// Simple popularity-based recommendations (Control).
/// </summary>
public class PopularityRecommendationEngine : IRecommendationEngine
{
    public async Task<IEnumerable<ProductRecommendation>> GetRecommendationsAsync(string userId, CancellationToken ct = default)
    {
        await Task.Delay(10, ct); // Simulate work

        return
        [
            new ProductRecommendation("P1", "Most Popular Product", 0.95m),
            new ProductRecommendation("P2", "Second Most Popular", 0.85m),
            new ProductRecommendation("P3", "Third Most Popular", 0.75m)
        ];
    }

    public string GetAlgorithmName() => "Popularity-Based";
}

/// <summary>
/// Machine learning-powered recommendations (Variant A).
/// </summary>
public class MLRecommendationEngine : IRecommendationEngine
{
    public async Task<IEnumerable<ProductRecommendation>> GetRecommendationsAsync(string userId, CancellationToken ct = default)
    {
        await Task.Delay(15, ct); // Simulate ML inference

        return
        [
            new ProductRecommendation("P4", "ML Predicted Product", 0.92m),
            new ProductRecommendation("P5", "AI Recommended", 0.88m),
            new ProductRecommendation("P1", "Fallback Popular", 0.70m)
        ];
    }

    public string GetAlgorithmName() => "Machine Learning";
}

/// <summary>
/// Collaborative filtering recommendations (Variant B).
/// </summary>
public class CollaborativeRecommendationEngine : IRecommendationEngine
{
    public async Task<IEnumerable<ProductRecommendation>> GetRecommendationsAsync(string userId, CancellationToken ct = default)
    {
        await Task.Delay(12, ct); // Simulate collaborative filtering

        return
        [
            new ProductRecommendation("P6", "Users Like You Bought", 0.90m),
            new ProductRecommendation("P7", "Similar Users Liked", 0.82m),
            new ProductRecommendation("P2", "Community Favorite", 0.76m)
        ];
    }

    public string GetAlgorithmName() => "Collaborative Filtering";
}
