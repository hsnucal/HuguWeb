using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Identity;
using HuGuWeb.Workforce.Infrastructure.Seeding;
using Microsoft.Extensions.Logging.Abstractions;

namespace HuGuWeb.UnitTests.Identity;

public class DevelopmentPersonaEmployeeMappingTests
{
    [Fact]
    public void Fixtures_AreDeterministicAndUnique()
    {
        var employeeIds = DevelopmentPersonaEmployeeFixtures.All.Select(item => item.EmployeeId).ToList();
        var employmentIds = DevelopmentPersonaEmployeeFixtures.All.Select(item => item.EmploymentId).ToList();
        var assignmentIds = DevelopmentPersonaEmployeeFixtures.All.Select(item => item.AssignmentId).ToList();
        var linkIds = DevelopmentPersonaEmployeeFixtures.All.Select(item => item.AccountLinkId).ToList();
        var personnelNumbers = DevelopmentPersonaEmployeeFixtures.All.Select(item => item.PersonnelNumber).ToList();

        Assert.Equal(employeeIds.Count, employeeIds.Distinct().Count());
        Assert.Equal(employmentIds.Count, employmentIds.Distinct().Count());
        Assert.Equal(assignmentIds.Count, assignmentIds.Distinct().Count());
        Assert.Equal(linkIds.Count, linkIds.Distinct().Count());
        Assert.Equal(personnelNumbers.Count, personnelNumbers.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(
            DevelopmentPersonaEmployeeFixtures.All,
            item => item.PersonaEmail == "maintenance.technician@localhost"
                && item.PersonnelNumber == "DEMO-TECH-01"
                && item.DepartmentCode == "ENG"
                && item.PositionCode == "ENG-TECH");
        Assert.Contains(
            DevelopmentPersonaEmployeeFixtures.All,
            item => item.PersonaEmail == "hr.manager@localhost"
                && item.PersonnelNumber == "DEMO-HR-01"
                && item.DepartmentCode == "HR"
                && item.PositionCode == "HR-OFF");
        Assert.Contains(
            DevelopmentPersonaEmployeeFixtures.All,
            item => item.PersonaEmail == "frontoffice.receptionist@localhost"
                && item.PersonnelNumber == "DEMO-FO-01"
                && item.GivenName == "Hasan"
                && item.DepartmentCode == "FO"
                && item.PositionCode == "FO-REC");
    }

    [Fact]
    public async Task OperationalPersonnelReset_BlockedOutsideDevelopment()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DevelopmentOperationalPersonnelReset.ClearAsync(
                null!,
                NullLogger.Instance,
                isDevelopment: false));
        Assert.Contains("blocked outside Development", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PersonaEmployeeSeeder_BlockedOutsideDevelopment()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DevelopmentPersonaEmployeeSeeder.EnsureAsync(
                null!,
                NullLogger.Instance,
                isDevelopment: false));
        Assert.Contains("blocked outside Development", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccessSnapshot_ResolvesLinkedEmployeeOnly_NoFallback()
    {
        var technicianUser = "tech-user";
        var hrUser = "hr-user";
        var techEmployee = DevelopmentPersonaEmployeeFixtures.MaintenanceTechnicianEmployeeId;
        var hrEmployee = DevelopmentPersonaEmployeeFixtures.HrManagerEmployeeId;
        var store = new LinkStore(
        [
            new EmployeeAccountLink
            {
                Id = DevelopmentPersonaEmployeeFixtures.MaintenanceTechnicianLinkId,
                UserId = technicianUser,
                EmployeeId = techEmployee,
                CreatedAtUtc = DateTimeOffset.UtcNow
            },
            new EmployeeAccountLink
            {
                Id = DevelopmentPersonaEmployeeFixtures.HrManagerLinkId,
                UserId = hrUser,
                EmployeeId = hrEmployee,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }
        ]);
        var service = new AccessSnapshotService(store);

        var techSnap = await service.GetSnapshotAsync(technicianUser, null, CancellationToken.None);
        var hrSnap = await service.GetSnapshotAsync(hrUser, null, CancellationToken.None);
        var orphanSnap = await service.GetSnapshotAsync("no-link-user", null, CancellationToken.None);

        Assert.Equal(techEmployee, techSnap.EmployeeId);
        Assert.Equal(hrEmployee, hrSnap.EmployeeId);
        Assert.NotEqual(techSnap.EmployeeId, hrSnap.EmployeeId);
        Assert.Null(orphanSnap.EmployeeId);
        Assert.NotEqual(DevelopmentWorkforceSeeder.DevelopmentEmployeeId, techSnap.EmployeeId);
    }

    private sealed class LinkStore(IReadOnlyList<EmployeeAccountLink> links) : IAuthorizationStore
    {
        private readonly List<EmployeeAccountLink> _links = links.ToList();

        public Task<EmployeeAccountLink?> FindLinkByUserAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(_links.FirstOrDefault(item => item.UserId == userId));

        public Task<EmployeeAccountLink?> FindLinkByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken) =>
            Task.FromResult(_links.FirstOrDefault(item => item.EmployeeId == employeeId));

        public Task<IReadOnlyList<EmployeeAccountLink>> ListLinksAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EmployeeAccountLink>>(_links);

        public Task<IReadOnlyList<UserMembership>> ListMembershipsForUserAsync(
            string userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserMembership>>([]);

        public Task<UserMembership?> GetMembershipAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<UserMembership?>(null);

        public Task<IReadOnlyList<UserMembership>> ListMembershipsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserMembership>>([]);

        public Task<IReadOnlyList<UserMembership>> ListMembershipsForOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserMembership>>([]);

        public Task<AuthorizationRole?> GetRoleAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<AuthorizationRole?>(null);

        public Task<AuthorizationRole?> FindRoleByCodeAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken) =>
            Task.FromResult<AuthorizationRole?>(null);

        public Task<IReadOnlyList<AuthorizationRole>> ListRolesAsync(
            Guid organizationId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AuthorizationRole>>([]);

        public Task<IReadOnlyList<RolePermission>> ListPermissionsForRolesAsync(
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RolePermission>>([]);

        public Task<IReadOnlyList<string>> ListUserIdsForRoleAsync(Guid roleId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public void AddMembership(UserMembership membership) => throw new NotSupportedException();
        public void AddDepartmentScope(UserMembershipDepartmentScope scope) => throw new NotSupportedException();
        public void RemoveDepartmentScope(UserMembershipDepartmentScope scope) => throw new NotSupportedException();
        public void AddRole(AuthorizationRole role) => throw new NotSupportedException();
        public void AddPermission(RolePermission permission) => throw new NotSupportedException();
        public void RemovePermission(RolePermission permission) => throw new NotSupportedException();
        public void AddAssignment(UserRoleAssignment assignment) => throw new NotSupportedException();
        public void RemoveAssignment(UserRoleAssignment assignment) => throw new NotSupportedException();
        public void AddLink(EmployeeAccountLink link) => _links.Add(link);
        public void RemoveLink(EmployeeAccountLink link) => _links.Remove(link);
        public void AddAudit(AuthorizationAuditRecord record) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
