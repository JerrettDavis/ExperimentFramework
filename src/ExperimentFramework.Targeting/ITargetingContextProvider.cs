namespace ExperimentFramework.Targeting;

/// <summary>
/// Provides the targeting context for the current request or scope.
/// </summary>
/// <remarks>
/// <para>
/// Register an implementation of this interface to provide targeting context
/// to experiments. The context is typically derived from HTTP context, user claims,
/// or other request-scoped data.
/// </para>
/// <para>
/// This should be registered as scoped to ensure consistent context within a request:
/// <code>
/// services.AddScoped&lt;ITargetingContextProvider, HttpContextTargetingProvider&gt;();
/// </code>
/// </para>
/// </remarks>
public interface ITargetingContextProvider
{
    /// <summary>
    /// Gets the current targeting context.
    /// </summary>
    /// <returns>The targeting context, or null if no context is available.</returns>
    ITargetingContext? GetContext();

    /// <summary>
    /// Asynchronously gets the current targeting context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The targeting context, or null if no context is available.</returns>
    ValueTask<ITargetingContext?> GetContextAsync(CancellationToken cancellationToken = default);
}
