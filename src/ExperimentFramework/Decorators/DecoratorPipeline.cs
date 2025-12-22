namespace ExperimentFramework.Decorators;

/// <summary>
/// Executes a configured sequence of experiment decorators around a terminal invocation.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline is created from a set of <see cref="IExperimentDecoratorFactory"/> instances and a scoped
/// <see cref="IServiceProvider"/>. Factories are materialized into concrete decorators when the pipeline is constructed.
/// </para>
/// <para>
/// Decorators are applied in registration order (outer-to-inner). The first registered decorator becomes the outermost.
/// </para>
/// </remarks>
public sealed class DecoratorPipeline
{
    private readonly IExperimentDecorator[] _decorators;

    /// <summary>
    /// Initializes a new decorator pipeline by creating decorators from the provided factories.
    /// </summary>
    /// <param name="factories">Factories used to create decorators.</param>
    /// <param name="sp">The service provider used during decorator creation.</param>
    /// <remarks>
    /// The pipeline eagerly constructs decorators via <see cref="IExperimentDecoratorFactory.Create(IServiceProvider)"/>.
    /// This ensures each decorator is created once for the pipeline instance.
    /// </remarks>
    public DecoratorPipeline(IEnumerable<IExperimentDecoratorFactory> factories, IServiceProvider sp)
        => _decorators = factories.Select(f => f.Create(sp)).ToArray();

    /// <summary>
    /// Executes the pipeline for a given invocation context synchronously.
    /// </summary>
    /// <param name="ctx">The invocation context for the current call.</param>
    /// <param name="terminal">The terminal invocation representing the actual implementation call.</param>
    /// <returns>The result of the invocation.</returns>
    /// <remarks>
    /// <para>
    /// The pipeline composes the chain by wrapping the <paramref name="terminal"/> delegate with each decorator,
    /// starting from the last decorator and moving toward the first, so registration order becomes outer-to-inner.
    /// </para>
    /// </remarks>
    public object? Invoke(InvocationContext ctx, Func<object?> terminal)
    {
        Func<ValueTask<object?>> asyncTerminal = () => new ValueTask<object?>(terminal());

        var next = asyncTerminal;

        // Outer-to-inner in registration order.
        for (var i = _decorators.Length - 1; i >= 0; i--)
        {
            var d = _decorators[i];
            var capturedNext = next;
            next = () => d.InvokeAsync(ctx, capturedNext);
        }

        return next().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes the pipeline for a given invocation context asynchronously.
    /// </summary>
    /// <param name="ctx">The invocation context for the current call.</param>
    /// <param name="terminal">The terminal invocation representing the actual implementation call.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that completes when the full pipeline and terminal invocation complete.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The pipeline composes the chain by wrapping the <paramref name="terminal"/> delegate with each decorator,
    /// starting from the last decorator and moving toward the first, so registration order becomes outer-to-inner.
    /// </para>
    /// <para>
    /// The returned task represents the entire decorated execution.
    /// </para>
    /// </remarks>
    public ValueTask<object?> InvokeAsync(InvocationContext ctx, Func<ValueTask<object?>> terminal)
    {
        var next = terminal;

        // Outer-to-inner in registration order.
        for (var i = _decorators.Length - 1; i >= 0; i--)
        {
            var d = _decorators[i];
            var capturedNext = next;
            next = () => d.InvokeAsync(ctx, capturedNext);
        }

        return next();
    }
}