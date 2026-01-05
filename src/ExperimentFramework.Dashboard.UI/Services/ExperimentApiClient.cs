using System.Net.Http.Json;
using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.Dashboard.UI.Services;

public class ExperimentApiClient(HttpClient httpClient)
{
    public HttpClient HttpClient => httpClient;

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

    // ============================================================================
    // Governance
    // ============================================================================

    public async Task<ExperimentStateInfo?> GetLifecycleStateAsync(string experimentName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ExperimentStateInfo>($"/api/governance/{experimentName}/state", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<bool> TransitionStateAsync(string experimentName, string targetState, string? actor = null, string? reason = null, CancellationToken cancellationToken = default)
    {
        var request = new { TargetState = targetState, Actor = actor, Reason = reason };
        var response = await httpClient.PostAsJsonAsync($"/api/governance/{experimentName}/transition", request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<StateTransitionInfo>> GetStateTransitionHistoryAsync(string experimentName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<GovernanceListResponse<StateTransitionInfo>>($"/api/governance/{experimentName}/transitions", cancellationToken);
            return response?.Transitions ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<List<PolicyEvaluationInfo>> GetPolicyEvaluationsAsync(string experimentName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<GovernancePolicyResponse>($"/api/governance/{experimentName}/policies", cancellationToken);
            return response?.Policies ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<List<ConfigurationVersionInfo>> GetConfigurationVersionsAsync(string experimentName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<GovernanceVersionsResponse>($"/api/governance/{experimentName}/versions", cancellationToken);
            return response?.Versions ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<ConfigurationVersionInfo?> GetConfigurationVersionAsync(string experimentName, int version, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ConfigurationVersionInfo>($"/api/governance/{experimentName}/versions/{version}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<bool> RollbackToVersionAsync(string experimentName, int version, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"/api/governance/{experimentName}/versions/{version}/rollback", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<AuditLogItem>> GetGovernanceAuditLogAsync(string experimentName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<GovernanceAuditResponse>($"/api/governance/{experimentName}/audit", cancellationToken);
            return response?.AuditLog ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
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
    public RolloutConfiguration? Rollout { get; set; }
    public List<TargetingRule> TargetingRules { get; set; } = [];
    public HypothesisInfo? Hypothesis { get; set; }
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

// ============================================================================
// Rollout DTOs - using types from ExperimentFramework.Dashboard.Abstractions
// ============================================================================
// RolloutConfiguration, RolloutStageDto, RolloutStatus, RolloutStageStatus
// are now imported from ExperimentFramework.Dashboard.Abstractions

// ============================================================================
// Targeting DTOs
// ============================================================================

public class TargetingRule
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Variant { get; set; } = "";
    public List<TargetingCondition> Conditions { get; set; } = [];
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 0;
}

public class TargetingCondition
{
    public string Attribute { get; set; } = "";
    public string Operator { get; set; } = "";
    public string Value { get; set; } = "";
}

// ============================================================================
// Hypothesis Testing DTOs
// ============================================================================

public class HypothesisInfo
{
    public string Statement { get; set; } = "";
    public string TestType { get; set; } = "";
    public string PrimaryMetric { get; set; } = "";
    public List<string> SecondaryMetrics { get; set; } = [];
    public double ExpectedEffectSize { get; set; }
    public double AlphaLevel { get; set; } = 0.05;
    public double PowerLevel { get; set; } = 0.80;
    public int MinimumSampleSize { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public HypothesisStatus Status { get; set; } = HypothesisStatus.Draft;
    public HypothesisResults? Results { get; set; }
}

public class HypothesisResults
{
    public int ControlSamples { get; set; }
    public int TreatmentSamples { get; set; }
    public double ControlMean { get; set; }
    public double TreatmentMean { get; set; }
    public double EffectSize { get; set; }
    public double PValue { get; set; }
    public double ConfidenceIntervalLower { get; set; }
    public double ConfidenceIntervalUpper { get; set; }
    public bool IsSignificant { get; set; }
    public string Conclusion { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
}

public enum HypothesisStatus
{
    Draft,
    Registered,
    Running,
    Analyzing,
    Completed,
    Inconclusive
}

// ============================================================================
// Governance DTOs
// ============================================================================

public class ExperimentStateInfo
{
    public string ExperimentName { get; set; } = "";
    public string State { get; set; } = "";
    public int ConfigurationVersion { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public string[] Transitions { get; set; } = Array.Empty<string>();
    public string? TenantId { get; set; }
    public string? Environment { get; set; }
}

public class StateTransitionInfo
{
    public string TransitionId { get; set; } = "";
    public string ExperimentName { get; set; } = "";
    public string FromState { get; set; } = "";
    public string ToState { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public string? Actor { get; set; }
    public string? Reason { get; set; }
    public string? TenantId { get; set; }
    public string? Environment { get; set; }
}

public class PolicyEvaluationInfo
{
    public string EvaluationId { get; set; } = "";
    public string PolicyName { get; set; } = "";
    public bool IsCompliant { get; set; }
    public string? Reason { get; set; }
    public string Severity { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public string? CurrentState { get; set; }
    public string? TargetState { get; set; }
}

public class ConfigurationVersionInfo
{
    public int VersionNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? ChangeDescription { get; set; }
    public string? LifecycleState { get; set; }
    public bool IsRollback { get; set; }
    public int? RolledBackFrom { get; set; }
    public string ConfigurationHash { get; set; } = "";
    public string? ConfigurationJson { get; set; }
}

public class AuditLogItem
{
    public string Type { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public string? Actor { get; set; }
    public string Details { get; set; } = "";
    public string? Reason { get; set; }
    public string TransitionId { get; set; } = "";
}

// Response wrappers for governance endpoints
public class GovernanceListResponse<T>
{
    public string ExperimentName { get; set; } = "";
    public List<T> Transitions { get; set; } = [];
}

public class GovernancePolicyResponse
{
    public string ExperimentName { get; set; } = "";
    public List<PolicyEvaluationInfo> Policies { get; set; } = [];
}

public class GovernanceVersionsResponse
{
    public string ExperimentName { get; set; } = "";
    public List<ConfigurationVersionInfo> Versions { get; set; } = [];
}

public class GovernanceAuditResponse
{
    public string ExperimentName { get; set; } = "";
    public List<AuditLogItem> AuditLog { get; set; } = [];
}
