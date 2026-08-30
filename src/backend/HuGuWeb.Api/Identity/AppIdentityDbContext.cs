using HuGuWeb.Api.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HuGuWeb.Api.Identity;

public sealed class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<UserMembership> UserMemberships => Set<UserMembership>();
    public DbSet<UserMembershipDepartmentScope> UserMembershipDepartmentScopes => Set<UserMembershipDepartmentScope>();
    public DbSet<AuthorizationRole> AuthorizationRoles => Set<AuthorizationRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<EmployeeAccountLink> EmployeeAccountLinks => Set<EmployeeAccountLink>();
    public DbSet<AuthorizationAuditRecord> AuthorizationAuditRecords => Set<AuthorizationAuditRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.PreferredLanguage)
                .HasMaxLength(8);
        });

        builder.Entity<UserMembership>(entity =>
        {
            entity.ToTable("UserMemberships");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.UserId).HasMaxLength(450).IsRequired();
            entity.HasIndex(item => item.UserId);
            entity.HasIndex(item => item.OrganizationId);
            entity.HasIndex(item => item.PropertyId)
                .HasFilter("\"PropertyId\" IS NOT NULL")
                .HasDatabaseName("IX_UserMemberships_PropertyId");
            entity.HasIndex(item => new { item.UserId, item.OrganizationId })
                .IsUnique()
                .HasFilter("\"PropertyId\" IS NULL")
                .HasDatabaseName("IX_UserMemberships_User_Organization_OrgWide");
            entity.HasIndex(item => new { item.UserId, item.OrganizationId, item.PropertyId })
                .IsUnique()
                .HasFilter("\"PropertyId\" IS NOT NULL")
                .HasDatabaseName("IX_UserMemberships_User_Organization_Property");
            entity.HasMany(item => item.RoleAssignments)
                .WithOne(item => item.Membership)
                .HasForeignKey(item => item.MembershipId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(item => item.DepartmentScopes)
                .WithOne(item => item.Membership)
                .HasForeignKey(item => item.UserMembershipId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserMembershipDepartmentScope>(entity =>
        {
            entity.ToTable("UserMembershipDepartmentScopes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.HasIndex(item => new { item.UserMembershipId, item.DepartmentId })
                .IsUnique()
                .HasDatabaseName("IX_UserMembershipDepartmentScopes_Membership_Department");
            entity.HasIndex(item => item.DepartmentId)
                .HasDatabaseName("IX_UserMembershipDepartmentScopes_DepartmentId");
        });

        builder.Entity<AuthorizationRole>(entity =>
        {
            entity.ToTable("AuthorizationRoles");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Code).HasMaxLength(64).IsRequired();
            entity.Property(item => item.ScopeType).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(item => new { item.OrganizationId, item.Code })
                .IsUnique()
                .HasDatabaseName("IX_AuthorizationRoles_OrganizationId_Code");
            entity.HasMany(item => item.Permissions)
                .WithOne(item => item.Role)
                .HasForeignKey(item => item.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(item => item.Assignments)
                .WithOne(item => item.Role)
                .HasForeignKey(item => item.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(item => new { item.RoleId, item.PermissionCode });
            entity.Property(item => item.PermissionCode).HasMaxLength(128).IsRequired();
        });

        builder.Entity<UserRoleAssignment>(entity =>
        {
            entity.ToTable("UserRoleAssignments");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.MembershipId, item.RoleId })
                .IsUnique()
                .HasDatabaseName("IX_UserRoleAssignments_MembershipId_RoleId");
        });

        builder.Entity<EmployeeAccountLink>(entity =>
        {
            entity.ToTable("EmployeeAccountLinks");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.UserId).HasMaxLength(450).IsRequired();
            entity.HasIndex(item => item.UserId).IsUnique();
            entity.HasIndex(item => item.EmployeeId).IsUnique();
        });

        builder.Entity<AuthorizationAuditRecord>(entity =>
        {
            entity.ToTable("AuthorizationAuditRecords");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ActorUserId).HasMaxLength(450);
            entity.Property(item => item.SubjectUserId).HasMaxLength(450);
            entity.Property(item => item.Action).HasMaxLength(64).IsRequired();
            entity.Property(item => item.PermissionCode).HasMaxLength(128);
            entity.Property(item => item.Details).HasMaxLength(2000);
            entity.HasIndex(item => item.OccurredAtUtc);
            entity.HasIndex(item => item.ActorUserId);
            entity.HasIndex(item => item.ActorOrganizationId);
        });
    }
}
