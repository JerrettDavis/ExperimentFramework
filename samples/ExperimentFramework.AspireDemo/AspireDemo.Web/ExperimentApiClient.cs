using System.Net.Http.Json;

namespace AspireDemo.Web;

public class ExperimentApiClient(HttpClient httpClient)
{
    // ============================================================================
    // Experiment Management
    // ============================================================================

    public async Task<List<ExperimentInfo>> GetExperimentsAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<ExperimentInfo>>("/api/experiments", cancellationToken);
        return result ?? [];
    }

    public async Task<ExperimentInfo?> GetExperimentAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ExperimentInfo>($"/api/experiments/{name}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ExperimentInfo?> ActivateVariantAsync(string experimentName, string variant, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"/api/experiments/{experimentName}/activate/{variant}", null, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ExperimentInfo>(cancellationToken);
        }
        return null;
    }

    // ============================================================================
    // Demo Endpoints
    // ============================================================================

    public async Task<PricingResponse?> CalculatePricingAsync(int units, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<PricingResponse>($"/api/pricing/calculate?units={units}", cancellationToken);
    }

    public async Task<NotificationResponse?> GetNotificationPreviewAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrEmpty(userId)
            ? "/api/notifications/preview"
            : $"/api/notifications/preview?userId={Uri.EscapeDataString(userId)}";
        return await httpClient.GetFromJsonAsync<NotificationResponse>(url, cancellationToken);
    }

    public async Task<RecommendationResponse?> GetRecommendationsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrEmpty(userId)
            ? "/api/recommendations"
            : $"/api/recommendations?userId={Uri.EscapeDataString(userId)}";
        return await httpClient.GetFromJsonAsync<RecommendationResponse>(url, cancellationToken);
    }

    public async Task<ThemeResponse?> GetThemeAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ThemeResponse>("/api/theme", cancellationToken);
    }

    // ============================================================================
    // Analytics
    // ============================================================================

    public async Task<List<AuditLogEntry>> GetAuditLogAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<AuditLogEntry>>($"/api/audit?limit={limit}", cancellationToken);
        return result ?? [];
    }

    public async Task<Dictionary<string, Dictionary<string, int>>> GetUsageStatsAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<Dictionary<string, Dictionary<string, int>>>("/api/analytics/usage", cancellationToken);
        return result ?? [];
    }

    // ============================================================================
    // Configuration
    // ============================================================================

    public async Task<string> GetConfigYamlAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetStringAsync("/api/config/yaml", cancellationToken);
    }

    public async Task<FrameworkInfo?> GetConfigInfoAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<FrameworkInfo>("/api/config/info", cancellationToken);
    }

    public async Task<List<KillSwitchStatus>> GetKillSwitchStatusesAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<KillSwitchStatus>>("/api/config/kill-switch", cancellationToken);
        return result ?? [];
    }

    public async Task<KillSwitchStatus?> UpdateKillSwitchAsync(KillSwitchUpdate request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/config/kill-switch", request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<KillSwitchStatus>(cancellationToken);
        }

        return null;
    }

    // ============================================================================
    // DSL Configuration
    // ============================================================================

    public async Task<DslValidationResponse?> ValidateDslAsync(string yaml, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/dsl/validate", new { yaml }, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<DslValidationResponse>(cancellationToken);
        }
        return null;
    }

    public async Task<DslApplyResponse?> ApplyDslAsync(string yaml, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/dsl/apply", new { yaml }, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<DslApplyResponse>(cancellationToken);
        }
        return null;
    }

    public async Task<DslCurrentResponse?> GetCurrentDslAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<DslCurrentResponse>("/api/dsl/current", cancellationToken);
    }

    public async Task<object?> GetDslSchemaAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<object>("/api/dsl/schema", cancellationToken);
    }

    // ============================================================================
    // Plugins
    // ============================================================================

    public async Task<List<PluginInfo>> GetPluginsAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<PluginInfo>>("/api/plugins", cancellationToken);
        return result ?? [];
    }

    public async Task<int> DiscoverPluginsAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync("/api/plugins/discover", null, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<DiscoverResult>(cancellationToken);
            return result?.LoadedCount ?? 0;
        }
        return 0;
    }

    public async Task ReloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        await httpClient.PostAsync($"/api/plugins/{pluginId}/reload", null, cancellationToken);
    }

    public async Task UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        await httpClient.DeleteAsync($"/api/plugins/{pluginId}", cancellationToken);
    }

    public async Task<PluginUseResult?> UsePluginImplementationAsync(string pluginId, string interfaceName, string implName, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"/api/plugins/{pluginId}/use?interface={Uri.EscapeDataString(interfaceName)}&impl={Uri.EscapeDataString(implName)}", null, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<PluginUseResult>(cancellationToken);
        }
        return null;
    }

    public async Task<Dictionary<string, ActivePluginImplementation>> GetActivePluginImplementationsAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<Dictionary<string, ActivePluginImplementation>>("/api/plugins/active", cancellationToken);
        return result ?? [];
    }

    public async Task<bool> ClearActivePluginImplementationAsync(string interfaceName, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/plugins/active/{Uri.EscapeDataString(interfaceName)}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}

// ============================================================================
// DTOs
// ============================================================================

public class ExperimentInfo
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string ActiveVariant { get; set; } = "";
    public List<VariantInfo> Variants { get; set; } = [];
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime LastModified { get; set; }
}

public class VariantInfo
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
}

public class PricingResponse
{
    public string Strategy { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public int Units { get; set; }
    public string Variant { get; set; } = "";
    public string? PluginId { get; set; }
    public bool IsPlugin { get; set; }
    public DateTime Timestamp { get; set; }
}

public class NotificationResponse
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Style { get; set; } = "";
    public string Variant { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class RecommendationResponse
{
    public string Algorithm { get; set; } = "";
    public List<string> Items { get; set; } = [];
    public double Confidence { get; set; }
    public string Variant { get; set; } = "";
    public string? PluginId { get; set; }
    public bool IsPlugin { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ThemeResponse
{
    public string Name { get; set; } = "";
    public string PrimaryColor { get; set; } = "";
    public string BackgroundColor { get; set; } = "";
    public string TextColor { get; set; } = "";
    public string AccentColor { get; set; } = "";
    public string Variant { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class AuditLogEntry
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = "";
    public string? ExperimentName { get; set; }
    public string? TrialName { get; set; }
    public string Details { get; set; } = "";
}

public class FrameworkInfo
{
    public FrameworkDetails Framework { get; set; } = new();
    public ServerDetails Server { get; set; } = new();
    public ExperimentStats Experiments { get; set; } = new();
    public List<FeatureInfo> Features { get; set; } = [];
}

public class FrameworkDetails
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Runtime { get; set; } = "";
    public string ProxyType { get; set; } = "";
}

public class ServerDetails
{
    public string MachineName { get; set; } = "";
    public int ProcessId { get; set; }
    public DateTime StartTime { get; set; }
    public string UpTime { get; set; } = "";
}

public class ExperimentStats
{
    public int Total { get; set; }
    public int Active { get; set; }
    public Dictionary<string, int> Categories { get; set; } = [];
}

public class FeatureInfo
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public string Description { get; set; } = "";
    public string Category { get; set; } = "General";
}

public class KillSwitchStatus
{
    public string Experiment { get; set; } = "";
    public bool ExperimentDisabled { get; set; }
    public List<string> DisabledVariants { get; set; } = [];
}

public class KillSwitchUpdate
{
    public string Experiment { get; set; } = "";
    public string? Variant { get; set; }
    public bool Disabled { get; set; }
}

// ============================================================================
// Plugin DTOs
// ============================================================================

public class PluginInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public string IsolationMode { get; set; } = "";
    public bool SupportsHotReload { get; set; }
    public DateTime LoadTime { get; set; }
    public string Path { get; set; } = "";
    public List<PluginServiceInfo> Services { get; set; } = [];
}

public class PluginServiceInfo
{
    public string Interface { get; set; } = "";
    public List<PluginImplementationInfo> Implementations { get; set; } = [];
}

public class PluginImplementationInfo
{
    public string Type { get; set; } = "";
    public string? Alias { get; set; }
}

public class DiscoverResult
{
    public int LoadedCount { get; set; }
}

public class PluginUseResult
{
    public string PluginId { get; set; } = "";
    public string Interface { get; set; } = "";
    public string Implementation { get; set; } = "";
    public string? Alias { get; set; }
    public bool Active { get; set; }
}

public class ActivePluginImplementation
{
    public string PluginId { get; set; } = "";
    public string Interface { get; set; } = "";
    public string ImplementationType { get; set; } = "";
    public string? Alias { get; set; }
    public DateTime ActivatedAt { get; set; }
}

// ============================================================================
// DSL DTOs
// ============================================================================

public class DslValidationResponse
{
    public bool IsValid { get; set; }
    public List<DslValidationError> Errors { get; set; } = [];
    public List<ExperimentPreview> ParsedExperiments { get; set; } = [];
}

public class DslApplyResponse
{
    public bool Success { get; set; }
    public List<AppliedExperiment> Changes { get; set; } = [];
    public List<DslValidationError> Errors { get; set; } = [];
}

public class DslCurrentResponse
{
    public string Yaml { get; set; } = "";
    public DateTime? LastApplied { get; set; }
    public bool HasUnappliedChanges { get; set; }
}

public class DslValidationError
{
    public string Path { get; set; } = "";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "error";
    public int Line { get; set; } = 1;
    public int Column { get; set; } = 1;
    public int EndLine { get; set; } = 1;
    public int EndColumn { get; set; } = 1;
}

public class ExperimentPreview
{
    public string Name { get; set; } = "";
    public int TrialCount { get; set; }
    public string Action { get; set; } = "";
}

public class AppliedExperiment
{
    public string Name { get; set; } = "";
    public string Action { get; set; } = "";
}
