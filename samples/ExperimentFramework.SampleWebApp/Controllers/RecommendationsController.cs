using ExperimentFramework.SampleWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExperimentFramework.SampleWebApp.Controllers;

/// <summary>
/// API controller demonstrating sticky A/B testing for recommendation algorithms.
/// Each user consistently sees recommendations from the same algorithm.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RecommendationsController(IRecommendationEngine recommendationEngine) : ControllerBase
{
    /// <summary>
    /// Gets personalized product recommendations.
    /// The recommendation algorithm is selected via sticky routing based on user/session identity.
    /// </summary>
    /// <param name="userId">User ID for personalized recommendations</param>
    [HttpGet]
    public async Task<IActionResult> GetRecommendations([FromQuery] string userId = "anonymous")
    {
        var recommendations = await recommendationEngine.GetRecommendationsAsync(userId);
        var algorithm = recommendationEngine.GetAlgorithmName();

        return Ok(new
        {
            Algorithm = algorithm,
            UserId = userId,
            Recommendations = recommendations,
            Message = $"Recommendations generated using {algorithm} algorithm. " +
                      "Refresh this page multiple times - you'll always see the same algorithm due to sticky routing!"
        });
    }

    /// <summary>
    /// Gets information about the current recommendation algorithm.
    /// </summary>
    [HttpGet("algorithm")]
    public IActionResult GetAlgorithmInfo()
    {
        var algorithm = recommendationEngine.GetAlgorithmName();

        return Ok(new
        {
            Algorithm = algorithm,
            Description = algorithm switch
            {
                "Popularity-Based" => "Shows the most popular products across all users (Control)",
                "Machine Learning" => "Uses ML models to predict what you'll like (Variant A)",
                "Collaborative Filtering" => "Recommends based on similar users' preferences (Variant B)",
                _ => "Unknown algorithm"
            },
            StickyRouting = true,
            Note = "This user will consistently see this algorithm across all sessions"
        });
    }
}
