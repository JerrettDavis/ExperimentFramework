using ExperimentFramework.SampleWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExperimentFramework.SampleWebApp.Controllers;

/// <summary>
/// API controller demonstrating feature flag-based A/B testing for checkout flows.
/// Uses feature management to control which checkout experience users see.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CheckoutController(ICheckoutFlow checkoutFlow) : ControllerBase
{
    /// <summary>
    /// Gets the checkout flow information.
    /// The flow is selected based on the EnableExpressCheckout feature flag.
    /// </summary>
    [HttpGet("flow")]
    public IActionResult GetCheckoutFlow()
    {
        var flowInfo = checkoutFlow.GetCheckoutInfo();

        return Ok(new
        {
            flowInfo.FlowName,
            flowInfo.Steps,
            EstimatedSeconds = flowInfo.EstimatedTimeSeconds,
            Message = $"Using {flowInfo.FlowName} with {flowInfo.Steps.Length} steps. " +
                      "Toggle the 'EnableExpressCheckout' feature flag in appsettings.json to see different flows."
        });
    }

    /// <summary>
    /// Simulates completing the checkout process.
    /// </summary>
    [HttpPost("complete")]
    public IActionResult CompleteCheckout([FromBody] CheckoutRequest request)
    {
        var flowInfo = checkoutFlow.GetCheckoutInfo();

        return Ok(new
        {
            Success = true,
            FlowUsed = flowInfo.FlowName,
            StepsCompleted = flowInfo.Steps.Length,
            TimeSpent = flowInfo.EstimatedTimeSeconds,
            OrderId = Guid.NewGuid().ToString("N")[..8],
            Message = $"Order completed using {flowInfo.FlowName}!"
        });
    }
}

public record CheckoutRequest(string[] Items, decimal Total);
