using ExperimentFramework.StickyRouting;

namespace ExperimentFramework.SampleWebApp.Services;

/// <summary>
/// Provides user identity for sticky A/B routing based on session ID.
/// This ensures the same user always sees the same experiment variant.
/// </summary>
public class SessionIdentityProvider(IHttpContextAccessor contextAccessor) : IExperimentIdentityProvider
{
    public bool TryGetIdentity(out string identity)
    {
        var context = contextAccessor.HttpContext;
        if (context == null)
        {
            identity = string.Empty;
            return false;
        }

        // Try to get user ID from claims (authenticated users)
        var userId = context.User?.FindFirst("sub")?.Value
                  ?? context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            identity = $"user:{userId}";
            return true;
        }

        // Fall back to session ID for anonymous users
        // Ensure session is initialized by setting a value if not present
        if (context.Session != null)
        {
            const string SessionKey = "_ExperimentIdentity";
            if (!context.Session.Keys.Contains(SessionKey))
            {
                // Initialize session with a stable identifier
                context.Session.SetString(SessionKey, Guid.NewGuid().ToString());
            }

            var sessionId = context.Session.Id;
            if (!string.IsNullOrEmpty(sessionId))
            {
                identity = $"session:{sessionId}";
                return true;
            }
        }

        // Fall back to IP address + user agent hash as last resort
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var fallbackId = $"{ipAddress}:{userAgent}".GetHashCode().ToString("X");

        identity = $"fallback:{fallbackId}";
        return true;
    }
}
