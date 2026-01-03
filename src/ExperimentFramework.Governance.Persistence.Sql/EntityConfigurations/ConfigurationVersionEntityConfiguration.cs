using ExperimentFramework.Governance.Persistence.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExperimentFramework.Governance.Persistence.Sql.EntityConfigurations;

/// <summary>
/// Entity configuration for ConfigurationVersionEntity.
/// </summary>
public class ConfigurationVersionEntityConfiguration : IEntityTypeConfiguration<ConfigurationVersionEntity>
{
    public void Configure(EntityTypeBuilder<ConfigurationVersionEntity> builder)
    {
        builder.ToTable("ConfigurationVersions");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(e => e.ExperimentName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.VersionNumber)
            .IsRequired();

        builder.Property(e => e.ConfigurationJson)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(200);

        builder.Property(e => e.ConfigurationHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.TenantId)
            .HasMaxLength(100);

        builder.Property(e => e.Environment)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(e => new { e.ExperimentName, e.VersionNumber, e.TenantId, e.Environment })
            .IsUnique();
    }
}
