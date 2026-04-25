using ExperimentFramework.Dashboard.Abstractions;
using ExperimentFramework.Dashboard.Persistence;

namespace ExperimentFramework.Dashboard.Tests;

/// <summary>
/// Unit tests for InMemoryRolloutPersistence.
/// </summary>
public sealed class InMemoryRolloutPersistenceTests
{
    private static InMemoryRolloutPersistence CreatePersistence() => new();

    private static RolloutConfiguration MakeConfig(
        string experimentName,
        bool enabled = true,
        RolloutStatus status = RolloutStatus.InProgress,
        int percentage = 50)
        => new RolloutConfiguration
        {
            ExperimentName = experimentName,
            Enabled = enabled,
            Status = status,
            Percentage = percentage,
            TargetVariant = "variant-a"
        };

    [Fact]
    public async Task SaveAndGet_ReturnsStoredConfig()
    {
        var persistence = CreatePersistence();
        var config = MakeConfig("exp1");

        await persistence.SaveRolloutConfigAsync(config);
        var result = await persistence.GetRolloutConfigAsync("exp1");

        Assert.NotNull(result);
        Assert.Equal("exp1", result.ExperimentName);
        Assert.True(result.Enabled);
        Assert.Equal(50, result.Percentage);
    }

    [Fact]
    public async Task GetRolloutConfig_NonExistent_ReturnsNull()
    {
        var persistence = CreatePersistence();
        var result = await persistence.GetRolloutConfigAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveRolloutConfig_UpdatesExisting()
    {
        var persistence = CreatePersistence();
        var config1 = MakeConfig("exp1", percentage: 10);
        await persistence.SaveRolloutConfigAsync(config1);

        var config2 = MakeConfig("exp1", percentage: 75);
        await persistence.SaveRolloutConfigAsync(config2);

        var result = await persistence.GetRolloutConfigAsync("exp1");

        Assert.NotNull(result);
        Assert.Equal(75, result.Percentage);
    }

    [Fact]
    public async Task DeleteRolloutConfig_RemovesConfig()
    {
        var persistence = CreatePersistence();
        var config = MakeConfig("exp1");
        await persistence.SaveRolloutConfigAsync(config);

        await persistence.DeleteRolloutConfigAsync("exp1");
        var result = await persistence.GetRolloutConfigAsync("exp1");

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteRolloutConfig_NonExistent_DoesNotThrow()
    {
        var persistence = CreatePersistence();
        // Should not throw
        await persistence.DeleteRolloutConfigAsync("nonexistent");
    }

    [Fact]
    public async Task GetActiveRollouts_ReturnsOnlyActiveRollouts()
    {
        var persistence = CreatePersistence();
        await persistence.SaveRolloutConfigAsync(MakeConfig("exp-active", enabled: true, status: RolloutStatus.InProgress));
        await persistence.SaveRolloutConfigAsync(MakeConfig("exp-inactive", enabled: false, status: RolloutStatus.InProgress));
        await persistence.SaveRolloutConfigAsync(MakeConfig("exp-completed", enabled: true, status: RolloutStatus.Completed));

        var active = await persistence.GetActiveRolloutsAsync();

        Assert.Single(active);
        Assert.Equal("exp-active", active[0].ExperimentName);
    }

    [Fact]
    public async Task GetActiveRollouts_EmptyStore_ReturnsEmpty()
    {
        var persistence = CreatePersistence();
        var result = await persistence.GetActiveRolloutsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveRolloutConfig_WithTenantId_ScopesCorrectly()
    {
        var persistence = CreatePersistence();
        var config = MakeConfig("exp1");

        await persistence.SaveRolloutConfigAsync(config, tenantId: "tenant-a");
        var resultA = await persistence.GetRolloutConfigAsync("exp1", tenantId: "tenant-a");
        var resultNoTenant = await persistence.GetRolloutConfigAsync("exp1");

        Assert.NotNull(resultA);
        Assert.Null(resultNoTenant);
    }

    [Fact]
    public async Task DeleteRolloutConfig_WithTenantId_RemovesCorrectly()
    {
        var persistence = CreatePersistence();
        await persistence.SaveRolloutConfigAsync(MakeConfig("exp1"), tenantId: "tenant-a");
        await persistence.SaveRolloutConfigAsync(MakeConfig("exp1"));

        await persistence.DeleteRolloutConfigAsync("exp1", tenantId: "tenant-a");

        var resultA = await persistence.GetRolloutConfigAsync("exp1", tenantId: "tenant-a");
        var resultGlobal = await persistence.GetRolloutConfigAsync("exp1");

        Assert.Null(resultA);
        Assert.NotNull(resultGlobal);
    }

    [Fact]
    public async Task GetActiveRollouts_WithTenantId_FiltersCorrectly()
    {
        var persistence = CreatePersistence();
        await persistence.SaveRolloutConfigAsync(MakeConfig("exp1"), tenantId: "tenant-a");
        await persistence.SaveRolloutConfigAsync(MakeConfig("exp2"), tenantId: "tenant-b");

        var resultA = await persistence.GetActiveRolloutsAsync(tenantId: "tenant-a");
        var resultB = await persistence.GetActiveRolloutsAsync(tenantId: "tenant-b");

        Assert.Single(resultA);
        Assert.Equal("exp1", resultA[0].ExperimentName);
        Assert.Single(resultB);
        Assert.Equal("exp2", resultB[0].ExperimentName);
    }

    [Fact]
    public async Task SaveRolloutConfig_SetsLastModified()
    {
        var persistence = CreatePersistence();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        await persistence.SaveRolloutConfigAsync(MakeConfig("exp1"));

        var result = await persistence.GetRolloutConfigAsync("exp1");

        Assert.NotNull(result);
        Assert.True(result.LastModified >= before);
    }

    [Fact]
    public async Task SaveRolloutConfig_SetsTenantId()
    {
        var persistence = CreatePersistence();
        await persistence.SaveRolloutConfigAsync(MakeConfig("exp1"), tenantId: "t1");

        var result = await persistence.GetRolloutConfigAsync("exp1", tenantId: "t1");

        Assert.NotNull(result);
        Assert.Equal("t1", result.TenantId);
    }
}
