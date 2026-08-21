using HuGuWeb.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuGuWeb.Workforce.Infrastructure.Persistence;

public sealed class WorkforceDbContext(DbContextOptions<WorkforceDbContext> options) : DbContext(options)
{
    public const string PersonnelNumberIndexName = "IX_Employees_OrganizationId_PersonnelNumber";

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Employment> Employments => Set<Employment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new PropertyConfiguration());
        modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new PositionConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new EmploymentConfiguration());
        modelBuilder.ApplyConfiguration(new AssignmentConfiguration());
    }
}

file sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
    }
}

file sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.OrganizationId);
    }
}

file sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(Department.NameMaxLength).IsRequired();
        builder.Property(entity => entity.Code).HasMaxLength(Department.CodeMaxLength);
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(entity => entity.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.PropertyId);
    }
}

file sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(Position.NameMaxLength).IsRequired();
        builder.Property(entity => entity.Code).HasMaxLength(Position.CodeMaxLength);
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(entity => entity.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.PropertyId);
    }
}

file sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.GivenName).HasMaxLength(Employee.NameMaxLength).IsRequired();
        builder.Property(entity => entity.FamilyName).HasMaxLength(Employee.NameMaxLength).IsRequired();
        builder.Property(entity => entity.PersonnelNumber)
            .HasMaxLength(PersonnelNumber.MaxLength)
            .IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PersonnelNumber })
            .IsUnique()
            .HasDatabaseName(WorkforceDbContext.PersonnelNumberIndexName);
    }
}

file sealed class EmploymentConfiguration : IEntityTypeConfiguration<Employment>
{
    public void Configure(EntityTypeBuilder<Employment> builder)
    {
        builder.ToTable("Employments", table =>
        {
            table.HasCheckConstraint(
                "CK_Employments_Period",
                "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
            table.HasCheckConstraint(
                "CK_Employments_EndedHasEndDate",
                "\"Status\" <> 'Ended' OR \"EndDate\" IS NOT NULL");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.StartDate).HasColumnType("date").IsRequired();
        builder.Property(entity => entity.EndDate).HasColumnType("date");
        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(entity => entity.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EmployeeId);
    }
}

file sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments", table =>
        {
            table.HasCheckConstraint(
                "CK_Assignments_Period",
                "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.StartDate).HasColumnType("date").IsRequired();
        builder.Property(entity => entity.EndDate).HasColumnType("date");
        builder.Property(entity => entity.Kind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Employment>()
            .WithMany()
            .HasForeignKey(entity => entity.EmploymentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(entity => entity.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Position>()
            .WithMany()
            .HasForeignKey(entity => entity.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EmploymentId);
        builder.HasIndex(entity => new { entity.EmploymentId, entity.Kind, entity.StartDate });
    }
}
