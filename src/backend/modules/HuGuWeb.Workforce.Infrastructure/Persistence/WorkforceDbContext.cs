using HuGuWeb.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuGuWeb.Workforce.Infrastructure.Persistence;

public sealed class WorkforceDbContext(DbContextOptions<WorkforceDbContext> options) : DbContext(options)
{
    public const string PersonnelNumberIndexName = "IX_Employees_OrganizationId_PersonnelNumber";
    public const string NationalIdentityIndexName =
        "IX_EmployeeHrProfiles_OrganizationId_Scheme_NormalizedNumber";

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<DepartmentPositionApplicability> DepartmentPositionApplicabilities =>
        Set<DepartmentPositionApplicability>();
    public DbSet<PersonnelNumberSequence> PersonnelNumberSequences => Set<PersonnelNumberSequence>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Employment> Employments => Set<Employment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<EmployeeHrProfile> EmployeeHrProfiles => Set<EmployeeHrProfile>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<EmployeePhoto> EmployeePhotos => Set<EmployeePhoto>();
    public DbSet<SgkWorkplaceRegistration> SgkWorkplaceRegistrations => Set<SgkWorkplaceRegistration>();
    public DbSet<OfficialEmploymentProfile> OfficialEmploymentProfiles => Set<OfficialEmploymentProfile>();
    public DbSet<EmploymentBesSettings> EmploymentBesSettings => Set<EmploymentBesSettings>();
    public DbSet<SgkDocumentType> SgkDocumentTypes => Set<SgkDocumentType>();
    public DbSet<ApplicableLawCode> ApplicableLawCodes => Set<ApplicableLawCode>();
    public DbSet<InsuranceBranch> InsuranceBranches => Set<InsuranceBranch>();
    public DbSet<SgkOccupationCode> SgkOccupationCodes => Set<SgkOccupationCode>();
    public DbSet<EmploymentDutyCode> EmploymentDutyCodes => Set<EmploymentDutyCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new PropertyConfiguration());
        modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new PositionConfiguration());
        modelBuilder.ApplyConfiguration(new DepartmentPositionApplicabilityConfiguration());
        modelBuilder.ApplyConfiguration(new PersonnelNumberSequenceConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new EmploymentConfiguration());
        modelBuilder.ApplyConfiguration(new AssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeHrProfileConfiguration());
        modelBuilder.ApplyConfiguration(new EmergencyContactConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeePhotoConfiguration());
        modelBuilder.ApplyConfiguration(new SgkWorkplaceRegistrationConfiguration());
        modelBuilder.ApplyConfiguration(new OfficialEmploymentProfileConfiguration());
        modelBuilder.ApplyConfiguration(new EmploymentBesSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new SgkDocumentTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ApplicableLawCodeConfiguration());
        modelBuilder.ApplyConfiguration(new InsuranceBranchConfiguration());
        modelBuilder.ApplyConfiguration(new SgkOccupationCodeConfiguration());
        modelBuilder.ApplyConfiguration(new EmploymentDutyCodeConfiguration());
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

file sealed class DepartmentPositionApplicabilityConfiguration
    : IEntityTypeConfiguration<DepartmentPositionApplicability>
{
    public void Configure(EntityTypeBuilder<DepartmentPositionApplicability> builder)
    {
        builder.ToTable("DepartmentPositionApplicabilities");
        builder.HasKey(entity => new { entity.DepartmentId, entity.PositionId });
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(entity => entity.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Position>()
            .WithMany()
            .HasForeignKey(entity => entity.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.PositionId);
    }
}

file sealed class PersonnelNumberSequenceConfiguration : IEntityTypeConfiguration<PersonnelNumberSequence>
{
    public void Configure(EntityTypeBuilder<PersonnelNumberSequence> builder)
    {
        builder.ToTable("PersonnelNumberSequences");
        builder.HasKey(entity => entity.OrganizationId);
        builder.Property(entity => entity.NextValue).IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
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
            table.HasCheckConstraint(
                "CK_Employments_IncentiveRange",
                "\"IncentiveEndDate\" IS NULL OR \"IncentiveStartDate\" IS NULL OR \"IncentiveEndDate\" >= \"IncentiveStartDate\"");
            table.HasCheckConstraint(
                "CK_Employments_WorkPermitRange",
                "\"WorkPermitEndDate\" IS NULL OR \"WorkPermitStartDate\" IS NULL OR \"WorkPermitEndDate\" >= \"WorkPermitStartDate\"");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.StartDate).HasColumnType("date").IsRequired();
        builder.Property(entity => entity.EndDate).HasColumnType("date");
        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.ContractType).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.ContractEndDate).HasColumnType("date");
        builder.Property(entity => entity.PartTimeMonthlyHours).HasPrecision(6, 2);
        builder.Property(entity => entity.IskurStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.IncentiveStartDate).HasColumnType("date");
        builder.Property(entity => entity.IncentiveEndDate).HasColumnType("date");
        builder.Property(entity => entity.IskurWorkforceStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.WorkPermitStartDate).HasColumnType("date");
        builder.Property(entity => entity.WorkPermitEndDate).HasColumnType("date");
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

file sealed class EmployeeHrProfileConfiguration : IEntityTypeConfiguration<EmployeeHrProfile>
{
    public void Configure(EntityTypeBuilder<EmployeeHrProfile> builder)
    {
        builder.ToTable("EmployeeHrProfiles");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.NationalIdentityScheme)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.NationalIdentityNumber).HasMaxLength(NationalIdentity.NumberMaxLength);
        builder.Property(entity => entity.NormalizedNationalIdentityNumber)
            .HasMaxLength(NationalIdentity.NumberMaxLength);
        builder.Property(entity => entity.Nationality).HasMaxLength(ContactValue.NationalityMaxLength);
        builder.Property(entity => entity.Gender).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.BirthDate).HasColumnType("date");
        builder.Property(entity => entity.BirthPlace).HasMaxLength(ContactValue.PlaceMaxLength);
        builder.Property(entity => entity.MaritalStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.BloodType).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.EducationLevel).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.EducationDescription).HasMaxLength(ContactValue.EducationTextMaxLength);
        builder.Property(entity => entity.SchoolName).HasMaxLength(ContactValue.EducationTextMaxLength);
        builder.Property(entity => entity.GraduationDate).HasColumnType("date");
        builder.Property(entity => entity.ForeignLanguage).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.ArgeProjectCode).HasMaxLength(ContactValue.ArgeProjectCodeMaxLength);
        builder.Property(entity => entity.DrivingLicenceCategory).HasConversion<string>().HasMaxLength(8);
        builder.Property(entity => entity.MilitaryServiceStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.MilitaryExemptionReason).HasMaxLength(ContactValue.MilitaryReasonMaxLength);
        builder.Property(entity => entity.MilitaryDefermentReason).HasMaxLength(ContactValue.MilitaryReasonMaxLength);
        builder.Property(entity => entity.KepAddress).HasMaxLength(ContactValue.EmailMaxLength);
        builder.Property(entity => entity.MobilePhone).HasMaxLength(ContactValue.PhoneMaxLength);
        builder.Property(entity => entity.HomePhone).HasMaxLength(ContactValue.PhoneMaxLength);
        builder.Property(entity => entity.Email).HasMaxLength(ContactValue.EmailMaxLength);
        builder.Property(entity => entity.ResidenceAddress).HasMaxLength(ContactValue.AddressMaxLength);
        builder.Property(entity => entity.ResidenceCity).HasMaxLength(ContactValue.PlaceMaxLength);
        builder.Property(entity => entity.ResidenceDistrict).HasMaxLength(ContactValue.PlaceMaxLength);
        builder.Property(entity => entity.NotificationAddress).HasMaxLength(ContactValue.AddressMaxLength);
        builder.Property(entity => entity.HrNotes).HasMaxLength(ContactValue.NotesMaxLength);
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(entity => entity.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EmployeeId).IsUnique();
        builder.HasIndex(entity => new
            {
                entity.OrganizationId,
                entity.NationalIdentityScheme,
                entity.NormalizedNationalIdentityNumber
            })
            .IsUnique()
            .HasFilter("\"NormalizedNationalIdentityNumber\" IS NOT NULL")
            .HasDatabaseName(WorkforceDbContext.NationalIdentityIndexName);
    }
}

file sealed class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmergencyContact> builder)
    {
        builder.ToTable("EmergencyContacts");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(EmergencyContact.NameMaxLength).IsRequired();
        builder.Property(entity => entity.Relationship).HasMaxLength(EmergencyContact.RelationshipMaxLength);
        builder.Property(entity => entity.Phone).HasMaxLength(ContactValue.PhoneMaxLength).IsRequired();
        builder.Property(entity => entity.IsPrimary).IsRequired();
        builder.Property(entity => entity.SortOrder).IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(entity => entity.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EmployeeId)
            .HasDatabaseName("IX_EmergencyContacts_EmployeeId");
        builder.HasIndex(entity => entity.EmployeeId)
            .IsUnique()
            .HasFilter("\"IsPrimary\" = TRUE")
            .HasDatabaseName("IX_EmergencyContacts_EmployeeId_Primary");
    }
}

file sealed class EmployeePhotoConfiguration : IEntityTypeConfiguration<EmployeePhoto>
{
    public void Configure(EntityTypeBuilder<EmployeePhoto> builder)
    {
        builder.ToTable("EmployeePhotos");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.StorageKey).HasMaxLength(EmployeePhoto.StorageKeyMaxLength).IsRequired();
        builder.Property(entity => entity.ContentType).HasMaxLength(EmployeePhoto.ContentTypeMaxLength).IsRequired();
        builder.Property(entity => entity.ByteSize).IsRequired();
        builder.Property(entity => entity.UploadedAtUtc).IsRequired();
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(entity => entity.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EmployeeId).IsUnique();
        builder.HasIndex(entity => entity.StorageKey).IsUnique();
    }
}

file sealed class SgkWorkplaceRegistrationConfiguration : IEntityTypeConfiguration<SgkWorkplaceRegistration>
{
    public void Configure(EntityTypeBuilder<SgkWorkplaceRegistration> builder)
    {
        builder.ToTable("SgkWorkplaceRegistrations");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.RegistrationNumber)
            .HasMaxLength(SgkWorkplaceRegistration.RegistrationNumberMaxLength)
            .IsRequired();
        builder.Property(entity => entity.DisplayName).HasMaxLength(SgkWorkplaceRegistration.DisplayNameMaxLength);
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).IsRequired();
        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(entity => entity.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.PropertyId);
        builder.HasIndex(entity => new { entity.PropertyId, entity.IsActive });
    }
}

file sealed class OfficialEmploymentProfileConfiguration : IEntityTypeConfiguration<OfficialEmploymentProfile>
{
    public void Configure(EntityTypeBuilder<OfficialEmploymentProfile> builder)
    {
        builder.ToTable("OfficialEmploymentProfiles");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.DocumentTypeCode).HasMaxLength(SgkDocumentType.CodeMaxLength);
        builder.Property(entity => entity.ApplicableLawCode).HasMaxLength(ApplicableLawCode.CodeMaxLength);
        builder.Property(entity => entity.InsuranceBranchCode).HasMaxLength(InsuranceBranch.CodeMaxLength);
        builder.Property(entity => entity.OccupationCode).HasMaxLength(SgkOccupationCode.CodeMaxLength);
        builder.Property(entity => entity.DutyCode).HasMaxLength(EmploymentDutyCode.CodeMaxLength);
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Employment>()
            .WithMany()
            .HasForeignKey(entity => entity.EmploymentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EmploymentId).IsUnique();
        builder.HasOne<SgkWorkplaceRegistration>()
            .WithMany()
            .HasForeignKey(entity => entity.SgkWorkplaceRegistrationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SgkDocumentType>()
            .WithMany()
            .HasForeignKey(entity => entity.DocumentTypeCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicableLawCode>()
            .WithMany()
            .HasForeignKey(entity => entity.ApplicableLawCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InsuranceBranch>()
            .WithMany()
            .HasForeignKey(entity => entity.InsuranceBranchCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SgkOccupationCode>()
            .WithMany()
            .HasForeignKey(entity => entity.OccupationCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmploymentDutyCode>()
            .WithMany()
            .HasForeignKey(entity => entity.DutyCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

file sealed class SgkDocumentTypeConfiguration : IEntityTypeConfiguration<SgkDocumentType>
{
    public void Configure(EntityTypeBuilder<SgkDocumentType> builder)
    {
        builder.ToTable("SgkDocumentTypes");
        builder.HasKey(entity => entity.Code);
        builder.Property(entity => entity.Code).HasMaxLength(SgkDocumentType.CodeMaxLength);
        builder.Property(entity => entity.Description).HasMaxLength(SgkDocumentType.DescriptionMaxLength).IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
    }
}

file sealed class ApplicableLawCodeConfiguration : IEntityTypeConfiguration<ApplicableLawCode>
{
    public void Configure(EntityTypeBuilder<ApplicableLawCode> builder)
    {
        builder.ToTable("ApplicableLawCodes");
        builder.HasKey(entity => entity.Code);
        builder.Property(entity => entity.Code).HasMaxLength(ApplicableLawCode.CodeMaxLength);
        builder.Property(entity => entity.Description)
            .HasMaxLength(ApplicableLawCode.DescriptionMaxLength)
            .IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
    }
}

file sealed class InsuranceBranchConfiguration : IEntityTypeConfiguration<InsuranceBranch>
{
    public void Configure(EntityTypeBuilder<InsuranceBranch> builder)
    {
        builder.ToTable("InsuranceBranches");
        builder.HasKey(entity => entity.Code);
        builder.Property(entity => entity.Code).HasMaxLength(InsuranceBranch.CodeMaxLength);
        builder.Property(entity => entity.Description)
            .HasMaxLength(InsuranceBranch.DescriptionMaxLength)
            .IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
    }
}

file sealed class SgkOccupationCodeConfiguration : IEntityTypeConfiguration<SgkOccupationCode>
{
    public void Configure(EntityTypeBuilder<SgkOccupationCode> builder)
    {
        builder.ToTable("SgkOccupationCodes");
        builder.HasKey(entity => entity.Code);
        builder.Property(entity => entity.Code).HasMaxLength(SgkOccupationCode.CodeMaxLength);
        builder.Property(entity => entity.Description)
            .HasMaxLength(SgkOccupationCode.DescriptionMaxLength)
            .IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.Property(entity => entity.Source).HasMaxLength(SgkOccupationCode.SourceMaxLength);
        builder.Property(entity => entity.CatalogueVersion).HasMaxLength(SgkOccupationCode.CatalogueVersionMaxLength);
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasIndex(entity => entity.IsActive);
        builder.HasIndex(entity => entity.Description);
    }
}

file sealed class EmploymentDutyCodeConfiguration : IEntityTypeConfiguration<EmploymentDutyCode>
{
    public void Configure(EntityTypeBuilder<EmploymentDutyCode> builder)
    {
        builder.ToTable("EmploymentDutyCodes");
        builder.HasKey(entity => entity.Code);
        builder.Property(entity => entity.Code).HasMaxLength(EmploymentDutyCode.CodeMaxLength);
        builder.Property(entity => entity.Description)
            .HasMaxLength(EmploymentDutyCode.DescriptionMaxLength)
            .IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
    }
}

file sealed class EmploymentBesSettingsConfiguration : IEntityTypeConfiguration<EmploymentBesSettings>
{
    public void Configure(EntityTypeBuilder<EmploymentBesSettings> builder)
    {
        builder.ToTable("EmploymentBesSettings");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.DeductionEnabled).IsRequired();
        builder.Property(entity => entity.RatePercent).HasPrecision(5, 2);
        builder.Property(entity => entity.ExtraAmount).HasPrecision(18, 2);
        builder.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("now()");
        builder.HasOne<Employment>()
            .WithMany()
            .HasForeignKey(entity => entity.EmploymentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EmploymentId).IsUnique();
    }
}
