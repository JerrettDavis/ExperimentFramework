namespace ExperimentFramework.Activation;

/// <summary>
/// Provides the current time for experiment activation evaluation.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts time access to enable deterministic testing of
/// time-based experiment activation. In production, the default implementation
/// returns <see cref="DateTimeOffset.UtcNow"/>.
/// </para>
/// <para>
/// For testing, you can provide a mock implementation that returns a fixed
/// or controlled time value.
/// </para>
/// </remarks>
public interface IExperimentTimeProvider
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    /// <returns>The current time as a <see cref="DateTimeOffset"/>.</returns>
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// Default implementation of <see cref="IExperimentTimeProvider"/> that returns the actual system time.
/// </summary>
public sealed class SystemTimeProvider : IExperimentTimeProvider
{
    /// <summary>
    /// Gets the singleton instance of <see cref="SystemTimeProvider"/>.
    /// </summary>
    public static SystemTimeProvider Instance { get; } = new();

    private SystemTimeProvider() { }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
