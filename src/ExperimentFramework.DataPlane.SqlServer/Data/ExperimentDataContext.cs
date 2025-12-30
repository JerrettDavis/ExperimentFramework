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
            entity.ToTable(_options.TableName, _options.Schema);
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EventId).IsUnique();
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        });
    }
}
