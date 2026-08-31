using Microsoft.EntityFrameworkCore;
using HuGuWeb.Api.Identity;

namespace HuGuWeb.Api.Authorization;

public interface IAuthorizationStore
{
    Task<IReadOnlyList<UserMembership>> ListMembershipsForUserAsync(string userId, CancellationToken cancellationToken);
    Task<UserMembership?> GetMembershipAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserMembership>> ListMembershipsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<UserMembership>> ListMembershipsForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken);
    Task<AuthorizationRole?> GetRoleAsync(Guid id, CancellationToken cancellationToken);
    Task<AuthorizationRole?> FindRoleByCodeAsync(Guid organizationId, string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuthorizationRole>> ListRolesAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RolePermission>> ListPermissionsForRolesAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken);
    Task<EmployeeAccountLink?> FindLinkByUserAsync(string userId, CancellationToken cancellationToken);
    Task<EmployeeAccountLink?> FindLinkByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeAccountLink>> ListLinksAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListUserIdsForRoleAsync(Guid roleId, CancellationToken cancellationToken);

    void AddMembership(UserMembership membership);
    void AddDepartmentScope(UserMembershipDepartmentScope scope);
    void RemoveDepartmentScope(UserMembershipDepartmentScope scope);
    void AddRole(AuthorizationRole role);
    void AddPermission(RolePermission permission);
    void RemovePermission(RolePermission permission);
    void AddAssignment(UserRoleAssignment assignment);
    void RemoveAssignment(UserRoleAssignment assignment);
    void AddLink(EmployeeAccountLink link);
    void RemoveLink(EmployeeAccountLink link);
    void AddAudit(AuthorizationAuditRecord record);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class EfAuthorizationStore(AppIdentityDbContext dbContext) : IAuthorizationStore
{
    public async Task<IReadOnlyList<UserMembership>> ListMembershipsForUserAsync(
        string userId,
        CancellationToken cancellationToken) =>
        await dbContext.UserMemberships
            .Include(item => item.RoleAssignments)
            .Include(item => item.DepartmentScopes)
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);

    public Task<UserMembership?> GetMembershipAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.UserMemberships
            .Include(item => item.RoleAssignments)
            .Include(item => item.DepartmentScopes)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UserMembership>> ListMembershipsAsync(CancellationToken cancellationToken) =>
        await dbContext.UserMemberships
            .Include(item => item.RoleAssignments)
            .Include(item => item.DepartmentScopes)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserMembership>> ListMembershipsForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        await dbContext.UserMemberships
            .Include(item => item.RoleAssignments)
            .Include(item => item.DepartmentScopes)
            .Where(item => item.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

    public Task<AuthorizationRole?> GetRoleAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.AuthorizationRoles
            .Include(item => item.Permissions)
            .Include(item => item.Assignments)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<AuthorizationRole?> FindRoleByCodeAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken) =>
        dbContext.AuthorizationRoles.FirstOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.Code == code,
            cancellationToken);

    public async Task<IReadOnlyList<AuthorizationRole>> ListRolesAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        await dbContext.AuthorizationRoles
            .Include(item => item.Permissions)
            .Include(item => item.Assignments)
            .Where(item => item.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RolePermission>> ListPermissionsForRolesAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        return await dbContext.RolePermissions
            .Where(item => roleIds.Contains(item.RoleId))
            .ToListAsync(cancellationToken);
    }

    public Task<EmployeeAccountLink?> FindLinkByUserAsync(string userId, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccountLinks.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

    public Task<EmployeeAccountLink?> FindLinkByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccountLinks.FirstOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);

    public async Task<IReadOnlyList<EmployeeAccountLink>> ListLinksAsync(CancellationToken cancellationToken) =>
        await dbContext.EmployeeAccountLinks.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> ListUserIdsForRoleAsync(Guid roleId, CancellationToken cancellationToken) =>
        await dbContext.UserRoleAssignments
            .Where(item => item.RoleId == roleId)
            .Join(
                dbContext.UserMemberships,
                assignment => assignment.MembershipId,
                membership => membership.Id,
                (_, membership) => membership.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public void AddMembership(UserMembership membership) => dbContext.UserMemberships.Add(membership);

    public void AddDepartmentScope(UserMembershipDepartmentScope scope) =>
        dbContext.UserMembershipDepartmentScopes.Add(scope);

    public void RemoveDepartmentScope(UserMembershipDepartmentScope scope) =>
        dbContext.UserMembershipDepartmentScopes.Remove(scope);

    public void AddRole(AuthorizationRole role) => dbContext.AuthorizationRoles.Add(role);

    public void AddPermission(RolePermission permission) => dbContext.RolePermissions.Add(permission);

    public void RemovePermission(RolePermission permission) => dbContext.RolePermissions.Remove(permission);

    public void AddAssignment(UserRoleAssignment assignment) => dbContext.UserRoleAssignments.Add(assignment);

    public void RemoveAssignment(UserRoleAssignment assignment) => dbContext.UserRoleAssignments.Remove(assignment);

    public void AddLink(EmployeeAccountLink link) => dbContext.EmployeeAccountLinks.Add(link);

    public void RemoveLink(EmployeeAccountLink link) => dbContext.EmployeeAccountLinks.Remove(link);

    public void AddAudit(AuthorizationAuditRecord record) => dbContext.AuthorizationAuditRecords.Add(record);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
