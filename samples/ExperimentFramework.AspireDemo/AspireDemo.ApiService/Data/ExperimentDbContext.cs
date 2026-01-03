using Microsoft.EntityFrameworkCore;

namespace AspireDemo.ApiService.Data;

/// <summary>
/// EF Core DbContext for persisting experiment-related data.
/// </summary>
public class ExperimentDbContext : DbContext
{
    public ExperimentDbContext(DbContextOptions<ExperimentDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExperimentEntity> Experiments => Set<ExperimentEntity>();
    public DbSet<VariantEntity> Variants => Set<VariantEntity>();
    public DbSet<KillSwitchEntity> KillSwitches => Set<KillSwitchEntity>();
    public DbSet<PluginImplementationEntity> PluginImplementations => Set<PluginImplementationEntity>();
    public DbSet<DslConfigurationEntity> DslConfigurations => Set<DslConfigurationEntity>();
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Variants)
                  .WithOne(v => v.Experiment)
                  .HasForeignKey(v => v.ExperimentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VariantEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ExperimentId, e.Name }).IsUnique();
        });

        modelBuilder.Entity<KillSwitchEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ServiceTypeName, e.TrialKey }).IsUnique();
        });

        modelBuilder.Entity<PluginImplementationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InterfaceName).IsUnique();
        });

        modelBuilder.Entity<DslConfigurationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IsCurrent);
        });

        modelBuilder.Entity<AuditEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
        });
    }
}
