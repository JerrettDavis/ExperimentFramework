using ExperimentFramework;
using ExperimentFramework.SampleWebApp.Services;

namespace ExperimentFramework.SampleWebApp;

/// <summary>
/// Configures all experiments for the web application.
/// Demonstrates using .UseSourceGenerators() fluent API to trigger source generation.
/// </summary>
public static class ExperimentConfiguration
{
    /// <summary>
    /// Configures A/B experiments using fluent API with .UseSourceGenerators().
    /// This triggers compile-time source generation without requiring an attribute.
    /// </summary>
    public static ExperimentFrameworkBuilder ConfigureWebExperiments()
    {
        return ExperimentFrameworkBuilder.Create()
            // Experiment 1: Recommendation algorithms (sticky routing for consistent UX)
            .Trial<IRecommendationEngine>(t => t
                .UsingStickyRouting() // Same user always sees same algorithm
                .AddControl<PopularityRecommendationEngine>()
                .AddCondition<MLRecommendationEngine>("ml")
                .AddCondition<CollaborativeRecommendationEngine>("collaborative")
                .OnErrorFallbackToControl())

            // Experiment 2: Checkout flows (feature flag for gradual rollout)
            .Trial<ICheckoutFlow>(t => t
                .UsingFeatureFlag("EnableExpressCheckout")
                .AddControl<StandardCheckoutFlow>()
                .AddCondition<ExpressCheckoutFlow>("true")
                .OnErrorFallbackToControl())

            // Use fluent API to trigger source generation (no attribute needed!)
            .UseSourceGenerators();
    }
}
