using ExperimentFramework.Governance.Persistence.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExperimentFramework.Governance.Persistence.Sql.EntityConfigurations;

/// <summary>
/// Entity configuration for ExperimentStateEntity.
/// </summary>
public class ExperimentStateEntityConfiguration : IEntityTypeConfiguration<ExperimentStateEntity>
{
    public void Configure(EntityTypeBuilder<ExperimentStateEntity> builder)
    {
        builder.ToTable("ExperimentStates");

        // Composite key
        builder.HasKey(e => new { e.ExperimentName, e.TenantId, e.Environment });

        // Properties
        builder.Property(e => e.ExperimentName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.TenantId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Environment)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.CurrentState)
            .IsRequired();

        builder.Property(e => e.LastModified)
            .IsRequired();

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(200);

        builder.Property(e => e.ETag)
            .IsRequired()
            .HasMaxLength(100)
            .IsConcurrencyToken();
    }
}
