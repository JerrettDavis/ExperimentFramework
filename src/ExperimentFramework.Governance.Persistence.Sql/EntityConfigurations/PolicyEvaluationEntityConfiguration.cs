using ExperimentFramework.Governance.Persistence.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExperimentFramework.Governance.Persistence.Sql.EntityConfigurations;

/// <summary>
/// Entity configuration for PolicyEvaluationEntity.
/// </summary>
public class PolicyEvaluationEntityConfiguration : IEntityTypeConfiguration<PolicyEvaluationEntity>
{
    public void Configure(EntityTypeBuilder<PolicyEvaluationEntity> builder)
    {
        builder.ToTable("PolicyEvaluations");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(e => e.EvaluationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ExperimentName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.PolicyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.IsCompliant)
            .IsRequired();

        builder.Property(e => e.Severity)
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.Property(e => e.TenantId)
            .HasMaxLength(100);

        builder.Property(e => e.Environment)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(e => new { e.ExperimentName, e.TenantId, e.Environment, e.Timestamp });

        builder.HasIndex(e => new { e.ExperimentName, e.PolicyName, e.Timestamp });

        builder.HasIndex(e => e.EvaluationId)
            .IsUnique();
    }
}
