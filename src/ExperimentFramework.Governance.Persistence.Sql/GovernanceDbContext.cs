using ExperimentFramework.Governance.Persistence.Sql.Entities;
using ExperimentFramework.Governance.Persistence.Sql.EntityConfigurations;
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

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new ExperimentStateEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StateTransitionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ApprovalRecordEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ConfigurationVersionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PolicyEvaluationEntityConfiguration());
    }
}
