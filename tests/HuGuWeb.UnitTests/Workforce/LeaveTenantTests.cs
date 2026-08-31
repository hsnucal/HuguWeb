using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class LeaveTenantTests
{
    private static Employee ForeignEmployee(WorkforceHarness harness)
    {
        Assert.True(Employee.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Foreign",
            "Person",
            "F-0001",
            out var employee,
            out _));
        harness.Store.Employees.Add(employee!);
        return employee!;
    }

    [Fact]
    public async Task LeaveOverview_ForEmployeeInAnotherOrganization_IsNotFound()
    {
        var harness = new WorkforceHarness();
        var foreign = ForeignEmployee(harness);

        var result = await harness.LeaveQuery.ExecuteAsync(foreign.Id, null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("employee-not-found", result.Error!.Code);
    }

    [Fact]
    public async Task RecordLeave_ForEmployeeInAnotherOrganization_IsNotFound()
    {
        var harness = new WorkforceHarness();
        var annual = harness.SeedLeaveType("annual", "Yıllık İzin", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var foreign = ForeignEmployee(harness);

        var result = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                foreign.Id, null, annual.Id,
                new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 4), 2.0m, null, "actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("employee-not-found", result.Error!.Code);
    }

    [Fact]
    public async Task LeaveType_FromAnotherOrganization_CannotBeUsed()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        // A leave type owned by a different organization.
        var foreignType = LeaveType.CreateSystemDefault(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "annual",
            "Yıllık İzin",
            LeaveTypeSystemKind.Annual,
            tracksBalance: true,
            "seed",
            harness.Clock.UtcNow);
        harness.Store.LeaveTypes.Add(foreignType);

        var result = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, foreignType.Id,
                new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 4), 2.0m, null, "actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveTypeNotFound, result.Error!.Code);
    }

    [Fact]
    public void PermissionCatalog_IncludesLeavePermissions()
    {
        Assert.Contains(HrLeavePermissions.Read, PermissionCatalog.All);
        Assert.Contains(HrLeavePermissions.Manage, PermissionCatalog.All);
        Assert.True(PermissionCatalog.IsKnown(HrLeavePermissions.Read));
        Assert.True(PermissionCatalog.IsKnown(HrLeavePermissions.Manage));
        Assert.Equal("hr", PermissionCatalog.DomainGroup(HrLeavePermissions.Read));
    }

    [Fact]
    public void HrRoleTemplates_GrantLeavePermissions()
    {
        Assert.Contains(HrLeavePermissions.Read, SystemRoleTemplates.HumanResourcesPermissions);
        Assert.Contains(HrLeavePermissions.Manage, SystemRoleTemplates.HumanResourcesPermissions);
        Assert.Contains(HrLeavePermissions.Approve, SystemRoleTemplates.HumanResourcesPermissions);
        Assert.DoesNotContain(HrLeavePermissions.Request, SystemRoleTemplates.HumanResourcesPermissions);
    }

    [Fact]
    public void LeaveRequestAndApprovePermissions_AreCatalogued_WithPersonaBundles()
    {
        Assert.True(PermissionCatalog.IsKnown(HrLeavePermissions.Request));
        Assert.True(PermissionCatalog.IsKnown(HrLeavePermissions.Approve));
        Assert.Contains(HrLeavePermissions.Approve, SystemRoleTemplates.DepartmentSchedulerPermissions);
        Assert.Contains(HrLeavePermissions.Read, SystemRoleTemplates.DepartmentSchedulerPermissions);
        Assert.DoesNotContain(HrLeavePermissions.Manage, SystemRoleTemplates.DepartmentSchedulerPermissions);
        Assert.Contains(HrLeavePermissions.Request, SystemRoleTemplates.EmployeeLeaveSelfServicePermissions);
        Assert.DoesNotContain(HrLeavePermissions.Request, SystemRoleTemplates.HumanResourcesPermissions);
        Assert.Equal(
            SystemRoleTemplates.EmployeeLeaveSelfServicePermissions,
            SystemRoleTemplates.ByCode(SystemRoleTemplates.EmployeeLeaveSelfService)!.Permissions);
        Assert.DoesNotContain(
            SystemRoleTemplates.All.Where(template =>
                template.Code is not (
                    SystemRoleTemplates.DevelopmentSuperuser
                    or SystemRoleTemplates.EmployeeLeaveSelfService)),
            template => template.Permissions.Contains(HrLeavePermissions.Request));
    }
}