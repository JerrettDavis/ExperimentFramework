using Microsoft.AspNetCore.Http;

namespace ExperimentFramework.Dashboard.Abstractions;

/// <summary>
/// Resolves tenant context from HTTP requests.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves the tenant context from the HTTP context.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>The resolved tenant context, or null if no tenant could be determined.</returns>
    Task<TenantContext?> ResolveAsync(HttpContext httpContext);
}

/// <summary>
/// Represents a resolved tenant context.
/// </summary>
public sealed class TenantContext
{
    /// <summary>
    /// Gets the unique tenant identifier.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the tenant display name.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the environment (e.g., dev, staging, prod).
    /// </summary>
    public string? Environment { get; init; }

    /// <summary>
    /// Gets additional tenant metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Provides access to the current tenant context via AsyncLocal storage.
/// </summary>
public static class TenantContextAccessor
{
    private static readonly AsyncLocal<TenantContext?> _current = new();

    /// <summary>
    /// Gets or sets the current tenant context.
    /// </summary>
    public static TenantContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
