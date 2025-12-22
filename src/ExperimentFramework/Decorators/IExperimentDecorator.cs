namespace ExperimentFramework.Decorators;

/// <summary>
/// Defines an experiment decorator that can wrap an invocation with additional behavior.
/// </summary>
/// <remarks>
/// <para>
/// Decorators are invoked as a chain. Each decorator receives the <see cref="InvocationContext"/> for the call and a
/// next delegate that represents the remainder of the pipeline (including the terminal invocation).
/// </para>
/// <para>
/// Implementations should be side-effect safe and must call next exactly once unless intentionally
/// short-circuiting is desired (e.g., caching, fallback behavior, forced redirects).
/// </para>
/// </remarks>
public interface IExperimentDecorator
{
    /// <summary>
    /// Invokes the decorator for the provided call context.
    /// </summary>
    /// <param name="ctx">The invocation context for the current call.</param>
    /// <param name="next">
    /// The continuation representing the next decorator in the chain, or the terminal invocation if this is the last decorator.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that completes when the decorated invocation completes. The resulting object is
    /// the terminal return value (or <see langword="null"/> for void-like calls).
    /// </returns>
    ValueTask<object?> InvokeAsync(InvocationContext ctx, Func<ValueTask<object?>> next);
}