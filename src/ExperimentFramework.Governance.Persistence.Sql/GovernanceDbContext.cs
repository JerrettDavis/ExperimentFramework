using ExperimentFramework.Governance.Persistence.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExperimentFramework.Governance.Persistence.Sql;

/// <summary>
/// Database context for governance persistence.
/// </summary>
public sealed class GovernanceDbContext : DbContext
{
    public GovernanceDbContext(DbContextOptions<GovernanceDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExperimentStateEntity> ExperimentStates => Set<ExperimentStateEntity>();
    public DbSet<StateTransitionEntity> StateTransitions => Set<StateTransitionEntity>();
    public DbSet<ApprovalRecordEntity> ApprovalRecords => Set<ApprovalRecordEntity>();
    public DbSet<ConfigurationVersionEntity> ConfigurationVersions => Set<ConfigurationVersionEntity>();
    public DbSet<PolicyEvaluationEntity> PolicyEvaluations => Set<PolicyEvaluationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure composite key for ExperimentStates
        modelBuilder.Entity<ExperimentStateEntity>()
            .HasKey(e => new { e.ExperimentName, e.TenantId, e.Environment });

        // Configure indexes for StateTransitions
        modelBuilder.Entity<StateTransitionEntity>()
            .HasIndex(e => new { e.ExperimentName, e.TenantId, e.Environment, e.Timestamp });

        modelBuilder.Entity<StateTransitionEntity>()
            .HasIndex(e => e.TransitionId)
            .IsUnique();

        // Configure indexes for ApprovalRecords
        modelBuilder.Entity<ApprovalRecordEntity>()
            .HasIndex(e => new { e.ExperimentName, e.TenantId, e.Environment, e.Timestamp });

        modelBuilder.Entity<ApprovalRecordEntity>()
            .HasIndex(e => e.TransitionId);

        modelBuilder.Entity<ApprovalRecordEntity>()
            .HasIndex(e => e.ApprovalId)
            .IsUnique();

        // Configure indexes for ConfigurationVersions
        modelBuilder.Entity<ConfigurationVersionEntity>()
            .HasIndex(e => new { e.ExperimentName, e.VersionNumber, e.TenantId, e.Environment })
            .IsUnique();

        // Configure indexes for PolicyEvaluations
        modelBuilder.Entity<PolicyEvaluationEntity>()
            .HasIndex(e => new { e.ExperimentName, e.TenantId, e.Environment, e.Timestamp });

        modelBuilder.Entity<PolicyEvaluationEntity>()
            .HasIndex(e => new { e.ExperimentName, e.PolicyName, e.Timestamp });

        modelBuilder.Entity<PolicyEvaluationEntity>()
            .HasIndex(e => e.EvaluationId)
            .IsUnique();
    }
}
