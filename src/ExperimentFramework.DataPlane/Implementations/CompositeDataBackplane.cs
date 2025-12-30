using ExperimentFramework.DataPlane.Abstractions;

namespace ExperimentFramework.DataPlane.Implementations;

/// <summary>
/// Composite data backplane that publishes events to multiple backplanes.
/// </summary>
/// <remarks>
/// This allows routing events to multiple destinations simultaneously.
/// Failures in individual backplanes do not affect other backplanes.
/// </remarks>
public sealed class CompositeDataBackplane : IDataBackplane
{
    private readonly IDataBackplane[] _backplanes;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeDataBackplane"/> class.
    /// </summary>
    /// <param name="backplanes">The backplanes to composite.</param>
    public CompositeDataBackplane(IEnumerable<IDataBackplane> backplanes)
    {
        _backplanes = backplanes?.ToArray() ?? throw new ArgumentNullException(nameof(backplanes));
        
        if (_backplanes.Length == 0)
        {
            throw new ArgumentException("At least one backplane must be provided", nameof(backplanes));
        }
    }

    /// <inheritdoc />
    public async ValueTask PublishAsync(DataPlaneEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var tasks = _backplanes.Select(bp => bp.PublishAsync(envelope, cancellationToken).AsTask());
        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _backplanes.Select(bp => bp.FlushAsync(cancellationToken).AsTask());
        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public async ValueTask<BackplaneHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        var healthChecks = await Task.WhenAll(
            _backplanes.Select(bp => bp.HealthAsync(cancellationToken).AsTask()));

        var allHealthy = healthChecks.All(h => h.IsHealthy);
        var description = allHealthy
            ? "All backplanes are healthy"
            : $"{healthChecks.Count(h => !h.IsHealthy)} of {_backplanes.Length} backplanes are unhealthy";

        var diagnostics = healthChecks
            .Select((h, i) => new KeyValuePair<string, object>($"backplane_{i}", h))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return allHealthy
            ? BackplaneHealth.Healthy(description)
            : BackplaneHealth.Unhealthy(description, diagnostics);
    }
}
