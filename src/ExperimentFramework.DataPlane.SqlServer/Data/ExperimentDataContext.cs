using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.DataPlane.SqlServer.Data;

public sealed class ExperimentDataContext : DbContext
{
    private readonly SqlServerDataBackplaneOptions _options;

    public DbSet<ExperimentEventEntity> ExperimentEvents { get; set; } = null!;

    public ExperimentDataContext(
        DbContextOptions<ExperimentDataContext> options,
        IOptions<SqlServerDataBackplaneOptions> backplaneOptions)
        : base(options)
    {
        _options = backplaneOptions.Value;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(_options.Schema);

        modelBuilder.Entity<ExperimentEventEntity>(entity =>
        {
            // Table configuration
            entity.ToTable(_options.TableName, _options.Schema);

            // Primary key
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // EventId - unique identifier for idempotency
            entity.Property(e => e.EventId)
                .IsRequired()
                .HasMaxLength(100);
            entity.HasIndex(e => e.EventId)
                .IsUnique();

            // Timestamp
            entity.Property(e => e.Timestamp)
                .IsRequired();
            entity.HasIndex(e => e.Timestamp);

            // EventType
            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(50);
            entity.HasIndex(e => e.EventType);

            // SchemaVersion
            entity.Property(e => e.SchemaVersion)
                .IsRequired()
                .HasMaxLength(20);

            // PayloadJson - stores the event payload as JSON
            entity.Property(e => e.PayloadJson)
                .IsRequired();

            // CorrelationId - for tracing related events
            entity.Property(e => e.CorrelationId)
                .HasMaxLength(100);
            entity.HasIndex(e => e.CorrelationId);

            // MetadataJson - optional metadata
            entity.Property(e => e.MetadataJson)
                .IsRequired(false);

            // CreatedAt - auto-populated on insert
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
