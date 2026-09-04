using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public static class ManagerHierarchyEligibility
{
    public static async Task<WorkforceResult<int>> RequiredManagerLevelAsync(
        IWorkforceStore store,
        Guid organizationId,
        Guid subordinateEmploymentId,
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        var subordinateLevel = await OrganizationalLevelOnAsync(
            store,
            subordinateEmploymentId,
            effectiveDate,
            missingAssignment: WorkforceError.InvalidRequest(
                MovementValidation.Codes.AssignmentNotFound,
                "No primary assignment covers the manager-change effective date."),
            cancellationToken);
        if (!subordinateLevel.IsSuccess)
        {
            return subordinateLevel.Error!;
        }

        var positions = await store.ListPositionsForOrganizationAsync(organizationId, cancellationToken);
        var required = ManagerHierarchy.RequiredManagerLevel(positions, subordinateLevel.Value);
        if (required is null)
        {
            return WorkforceError.MovementManagerLevelInvalid();
        }

        return required.Value;
    }

    public static async Task<WorkforceError?> ValidateCandidateAsync(
        IWorkforceStore store,
        Guid organizationId,
        Guid subordinateEmploymentId,
        Guid managerEmploymentId,
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        var required = await RequiredManagerLevelAsync(
            store,
            organizationId,
            subordinateEmploymentId,
            effectiveDate,
            cancellationToken);
        if (!required.IsSuccess)
        {
            return required.Error;
        }

        var snapshot = await PositionOnAsync(
            store,
            managerEmploymentId,
            effectiveDate,
            WorkforceError.ReportingLineManagerNotFound(),
            cancellationToken);
        if (!snapshot.IsSuccess)
        {
            return snapshot.Error;
        }

        if (!snapshot.Value.CanManageEmployees)
        {
            return WorkforceError.MovementManagerCannotManage();
        }

        if (snapshot.Value.OrganizationalLevel != required.Value)
        {
            return WorkforceError.MovementManagerLevelInvalid();
        }

        return null;
    }

    public static async Task<WorkforceResult<int>> OrganizationalLevelOnAsync(
        IWorkforceStore store,
        Guid employmentId,
        DateOnly effectiveDate,
        WorkforceError missingAssignment,
        CancellationToken cancellationToken)
    {
        var position = await PositionOnAsync(
            store,
            employmentId,
            effectiveDate,
            missingAssignment,
            cancellationToken);
        if (!position.IsSuccess)
        {
            return position.Error!;
        }

        return position.Value.OrganizationalLevel;
    }

    public static async Task<WorkforceResult<Position>> PositionOnAsync(
        IWorkforceStore store,
        Guid employmentId,
        DateOnly effectiveDate,
        WorkforceError missingAssignment,
        CancellationToken cancellationToken)
    {
        var assignments = await store.ListAssignmentsAsync(employmentId, cancellationToken);
        var covering = PrimaryAssignments.Covering(assignments, effectiveDate);
        if (covering is null)
        {
            return missingAssignment;
        }

        var position = await store.GetPositionAsync(covering.PositionId, cancellationToken);
        if (position is null)
        {
            return WorkforceError.PositionNotFound();
        }

        return position;
    }
}
