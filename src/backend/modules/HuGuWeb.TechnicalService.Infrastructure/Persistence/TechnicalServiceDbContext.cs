using HuGuWeb.TechnicalService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuGuWeb.TechnicalService.Infrastructure.Persistence;

public sealed class TechnicalServiceDbContext(DbContextOptions<TechnicalServiceDbContext> options) : DbContext(options)
{
    public const string CategoryNameIndexName = "IX_MaintenanceIssueCategories_PropertyId_Name";
    public const string IssuePropertyIndexName = "IX_MaintenanceIssues_PropertyId_Status";
    public const string IssueRoomIndexName = "IX_MaintenanceIssues_RoomId_Status";

    public DbSet<MaintenanceIssueCategory> Categories => Set<MaintenanceIssueCategory>();
    public DbSet<MaintenanceIssue> Issues => Set<MaintenanceIssue>();
    public DbSet<MaintenanceIssueHistoryEntry> History => Set<MaintenanceIssueHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new IssueConfiguration());
        modelBuilder.ApplyConfiguration(new HistoryConfiguration());
    }
}

file sealed class CategoryConfiguration : IEntityTypeConfiguration<MaintenanceIssueCategory>
{
    public void Configure(EntityTypeBuilder<MaintenanceIssueCategory> builder)
    {
        builder.ToTable("MaintenanceIssueCategories");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(MaintenanceIssueCategory.NameMaxLength).IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasIndex(entity => new { entity.PropertyId, entity.Name })
            .IsUnique()
            .HasDatabaseName(TechnicalServiceDbContext.CategoryNameIndexName);
        builder.HasIndex(entity => entity.PropertyId);
    }
}

file sealed class IssueConfiguration : IEntityTypeConfiguration<MaintenanceIssue>
{
    public void Configure(EntityTypeBuilder<MaintenanceIssue> builder)
    {
        builder.ToTable("MaintenanceIssues", table =>
        {
            table.HasCheckConstraint(
                "CK_MaintenanceIssues_Priority",
                "\"Priority\" IN ('Normal', 'High', 'Urgent')");
            table.HasCheckConstraint(
                "CK_MaintenanceIssues_Status",
                "\"Status\" IN ('Open', 'InProgress', 'UnableToResolve', 'Resolved')");
            table.HasCheckConstraint(
                "CK_MaintenanceIssues_Outage",
                "(\"BlocksRoomUse\" = FALSE AND \"OutageClassification\" IS NULL) OR (\"BlocksRoomUse\" = TRUE AND \"OutageClassification\" IN ('OutOfOrder', 'OutOfService'))");
            table.HasCheckConstraint(
                "CK_MaintenanceIssues_InProgressHasAssignee",
                "\"Status\" <> 'InProgress' OR \"AssignedEmployeeId\" IS NOT NULL");
            table.HasCheckConstraint(
                "CK_MaintenanceIssues_ResolvedHasNote",
                "\"Status\" <> 'Resolved' OR (\"ResolutionNote\" IS NOT NULL AND btrim(\"ResolutionNote\") <> '')");
            table.HasCheckConstraint(
                "CK_MaintenanceIssues_UnableHasNote",
                "\"Status\" <> 'UnableToResolve' OR (\"UnableToResolveNote\" IS NOT NULL AND btrim(\"UnableToResolveNote\") <> '')");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Description).HasMaxLength(MaintenanceIssue.DescriptionMaxLength).IsRequired();
        builder.Property(entity => entity.Priority)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.OriginNote).HasMaxLength(MaintenanceIssue.OriginNoteMaxLength);
        builder.Property(entity => entity.BlocksRoomUse).IsRequired();
        builder.Property(entity => entity.OutageClassification)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.ResolutionNote).HasMaxLength(MaintenanceIssue.NoteMaxLength);
        builder.Property(entity => entity.UnableToResolveNote).HasMaxLength(MaintenanceIssue.NoteMaxLength);
        builder.Property(entity => entity.PreparationImpact)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasOne<MaintenanceIssueCategory>()
            .WithMany()
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.PropertyId, entity.Status })
            .HasDatabaseName(TechnicalServiceDbContext.IssuePropertyIndexName);
        builder.HasIndex(entity => new { entity.RoomId, entity.Status })
            .HasDatabaseName(TechnicalServiceDbContext.IssueRoomIndexName);
        builder.HasIndex(entity => entity.AssignedEmployeeId);
        builder.HasIndex(entity => entity.CreatedAt);
    }
}

file sealed class HistoryConfiguration : IEntityTypeConfiguration<MaintenanceIssueHistoryEntry>
{
    public void Configure(EntityTypeBuilder<MaintenanceIssueHistoryEntry> builder)
    {
        builder.ToTable("MaintenanceIssueHistory", table =>
        {
            table.HasCheckConstraint(
                "CK_MaintenanceIssueHistory_Event",
                "\"EventType\" IN ('Created', 'Assigned', 'Reassigned', 'PriorityChanged', 'BlockingChanged', 'Started', 'UnableToResolve', 'Resumed', 'Resolved')");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.EventType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.FromPriority)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.ToPriority)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.OutageClassification)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.PreparationImpact)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.Note).HasMaxLength(MaintenanceIssue.NoteMaxLength);
        builder.Property(entity => entity.OccurredAt).IsRequired();
        builder.HasOne<MaintenanceIssue>()
            .WithMany()
            .HasForeignKey(entity => entity.IssueId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.IssueId, entity.OccurredAt });
    }
}
