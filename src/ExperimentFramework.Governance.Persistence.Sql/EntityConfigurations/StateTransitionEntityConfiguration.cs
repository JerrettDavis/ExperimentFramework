using ExperimentFramework.Governance.Persistence.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExperimentFramework.Governance.Persistence.Sql.EntityConfigurations;

/// <summary>
/// Entity configuration for StateTransitionEntity.
/// </summary>
public class StateTransitionEntityConfiguration : IEntityTypeConfiguration<StateTransitionEntity>
{
    public void Configure(EntityTypeBuilder<StateTransitionEntity> builder)
    {
        builder.ToTable("StateTransitions");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(e => e.TransitionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ExperimentName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.FromState)
            .IsRequired();

        builder.Property(e => e.ToState)
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.Property(e => e.Actor)
            .HasMaxLength(200);

        builder.Property(e => e.TenantId)
            .HasMaxLength(100);

        builder.Property(e => e.Environment)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(e => new { e.ExperimentName, e.TenantId, e.Environment, e.Timestamp });

        builder.HasIndex(e => e.TransitionId)
            .IsUnique();
    }
}
