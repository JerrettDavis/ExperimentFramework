using PatternKit.Behavioral.Chain;

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
    private readonly AsyncActionChain<DecoratorInvocationState> _chain;

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
    {
        var decorators = factories.Select(f => f.Create(sp)).ToArray();
        _chain = BuildChain(decorators);
    }

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
        return InvokeAsync(ctx, () => new ValueTask<object?>(terminal()))
            .AsTask()
            .GetAwaiter()
            .GetResult();
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
    public async ValueTask<object?> InvokeAsync(InvocationContext ctx, Func<ValueTask<object?>> terminal)
    {
        var state = new DecoratorInvocationState(ctx, terminal);

        await _chain.ExecuteAsync(state).ConfigureAwait(false);

        return state.Result;
    }

    private static AsyncActionChain<DecoratorInvocationState> BuildChain(IReadOnlyList<IExperimentDecorator> decorators)
    {
        var builder = AsyncActionChain<DecoratorInvocationState>.Create();

        foreach (var decorator in decorators)
        {
            builder.Use(async (state, ct, next) =>
            {
                state.Result = await decorator.InvokeAsync(
                    state.Context,
                    async () =>
                    {
                        await next(state, ct).ConfigureAwait(false);
                        return state.Result;
                    }).ConfigureAwait(false);
            });
        }

        builder.Finally(async (state, _) =>
        {
            state.Result = await state.Terminal().ConfigureAwait(false);
        });

        return builder.Build();
    }

    private sealed class DecoratorInvocationState(
        InvocationContext context,
        Func<ValueTask<object?>> terminal)
    {
        public InvocationContext Context { get; } = context;
        public Func<ValueTask<object?>> Terminal { get; } = terminal;
        public object? Result { get; set; }
    }
}
