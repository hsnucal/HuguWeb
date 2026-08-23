using HuGuWeb.RoomOperations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuGuWeb.RoomOperations.Infrastructure.Persistence;

public sealed class RoomOperationsDbContext(DbContextOptions<RoomOperationsDbContext> options) : DbContext(options)
{
    public const string RoomNumberIndexName = "IX_Rooms_PropertyId_Number";
    public const string OpenWorkIndexName = "IX_HousekeepingWorkItems_RoomId_Open";

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<HousekeepingWorkItem> HousekeepingWorkItems => Set<HousekeepingWorkItem>();
    public DbSet<RoomReadinessHistoryEntry> RoomReadinessHistory => Set<RoomReadinessHistoryEntry>();
    public DbSet<RoomInspection> RoomInspections => Set<RoomInspection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RoomConfiguration());
        modelBuilder.ApplyConfiguration(new HousekeepingWorkItemConfiguration());
        modelBuilder.ApplyConfiguration(new RoomReadinessHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new RoomInspectionConfiguration());
    }
}

file sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms", table =>
        {
            table.HasCheckConstraint(
                "CK_Rooms_Readiness",
                "\"CurrentReadiness\" IN ('Dirty', 'Clean', 'Inspected', 'Ready')");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Number).HasMaxLength(Room.NumberMaxLength).IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.Property(entity => entity.CurrentReadiness)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.ReadinessCycleId).IsRequired();
        builder.Property(entity => entity.ReadinessVersion).IsConcurrencyToken();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasIndex(entity => new { entity.PropertyId, entity.Number })
            .IsUnique()
            .HasDatabaseName(RoomOperationsDbContext.RoomNumberIndexName);
        builder.HasIndex(entity => entity.PropertyId);
    }
}

file sealed class HousekeepingWorkItemConfiguration : IEntityTypeConfiguration<HousekeepingWorkItem>
{
    public void Configure(EntityTypeBuilder<HousekeepingWorkItem> builder)
    {
        builder.ToTable("HousekeepingWorkItems", table =>
        {
            table.HasCheckConstraint(
                "CK_HousekeepingWorkItems_Priority",
                "\"Priority\" IN ('Normal', 'High', 'Urgent')");
            table.HasCheckConstraint(
                "CK_HousekeepingWorkItems_State",
                "\"State\" IN ('Open', 'Completed')");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Priority)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.State)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.Origin)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property<DateTimeOffset>("RecordedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(entity => entity.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.RoomId);
        builder.HasIndex(entity => new { entity.RoomId, entity.State })
            .IsUnique()
            .HasFilter("\"State\" = 'Open'")
            .HasDatabaseName(RoomOperationsDbContext.OpenWorkIndexName);
    }
}

file sealed class RoomReadinessHistoryConfiguration : IEntityTypeConfiguration<RoomReadinessHistoryEntry>
{
    public void Configure(EntityTypeBuilder<RoomReadinessHistoryEntry> builder)
    {
        builder.ToTable("RoomReadinessHistory");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Readiness)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.Cause)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.Comment).HasMaxLength(500);
        builder.Property(entity => entity.OccurredAt).IsRequired();
        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(entity => entity.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.RoomId, entity.OccurredAt });
    }
}

file sealed class RoomInspectionConfiguration : IEntityTypeConfiguration<RoomInspection>
{
    public void Configure(EntityTypeBuilder<RoomInspection> builder)
    {
        builder.ToTable("RoomInspections", table =>
        {
            table.HasCheckConstraint(
                "CK_RoomInspections_Result",
                "\"Result\" IN ('Accepted', 'Rejected')");
            table.HasCheckConstraint(
                "CK_RoomInspections_RejectedHasReason",
                "\"Result\" <> 'Rejected' OR (\"Reason\" IS NOT NULL AND btrim(\"Reason\") <> '')");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Result)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(RoomInspection.ReasonMaxLength);
        builder.Property(entity => entity.OccurredAt).IsRequired();
        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(entity => entity.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.RoomId, entity.OccurredAt });
    }
}
