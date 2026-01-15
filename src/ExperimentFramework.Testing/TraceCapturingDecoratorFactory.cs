using ExperimentFramework.Decorators;

namespace ExperimentFramework.Testing;

/// <summary>
/// Factory for creating trace capturing decorators.
/// </summary>
internal sealed class TraceCapturingDecoratorFactory : IExperimentDecoratorFactory
{
    private readonly InMemoryExperimentEventSink _sink;

    public TraceCapturingDecoratorFactory(InMemoryExperimentEventSink sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public IExperimentDecorator Create(IServiceProvider serviceProvider)
    {
        return new TraceCapturingDecorator(_sink);
    }
}
