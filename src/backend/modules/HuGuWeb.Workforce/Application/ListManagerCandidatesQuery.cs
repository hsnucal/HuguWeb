using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class ListManagerCandidatesQuery(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<IReadOnlyList<ManagerCandidateDto>>> ExecuteAsync(
        Guid employmentId,
        DateOnly effectiveDate,
        IReadOnlySet<Guid>? accessiblePropertyIds,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employment = await store.GetEmploymentAsync(employmentId, cancellationToken);
        if (employment is null)
        {
            return WorkforceError.MovementEmploymentNotFound();
        }

        var employee = await store.GetEmployeeAsync(employment.EmployeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.MovementEmploymentNotFound();
        }

        employment.RefreshLifecycle(effectiveDate);
        var assignments = await store.ListAssignmentsAsync(employment.Id, cancellationToken);
        var covering = PrimaryAssignments.Covering(assignments, effectiveDate);
        if (covering is null)
        {
            return WorkforceError.InvalidRequest(
                MovementValidation.Codes.AssignmentNotFound,
                "No primary assignment covers the manager-change effective date.");
        }

        var department = await store.GetDepartmentAsync(covering.DepartmentId, cancellationToken);
        if (department is null)
        {
            return WorkforceError.DepartmentNotFound();
        }

        if (!MovementPropertyAccess.CanAccess(accessiblePropertyIds, department.PropertyId))
        {
            return WorkforceError.MovementPropertyAccessDenied();
        }

        var subordinatePosition = await store.GetPositionAsync(covering.PositionId, cancellationToken);
        if (subordinatePosition is null)
        {
            return WorkforceError.PositionNotFound();
        }

        var positions = await store.ListPositionsForOrganizationAsync(
            workplace.Value.Organization.Id,
            cancellationToken);
        var required = ManagerHierarchy.RequiredManagerLevel(positions, subordinatePosition.OrganizationalLevel);
        if (required is null)
        {
            return Array.Empty<ManagerCandidateDto>();
        }

        var employees = await store.ListEmployeesAsync(workplace.Value.Organization.Id, cancellationToken);
        var employments = await store.ListEmploymentsForEmployeesAsync(
            employees.Select(item => item.Id).ToArray(),
            cancellationToken);
        var allAssignments = await store.ListAssignmentsForEmploymentsAsync(
            employments.Select(item => item.Id).ToArray(),
            cancellationToken);
        var departments = (await store.ListDepartmentsForOrganizationAsync(
                workplace.Value.Organization.Id,
                cancellationToken))
            .ToDictionary(item => item.Id);
        var positionsById = positions.ToDictionary(item => item.Id);
        var employeesById = employees.ToDictionary(item => item.Id);
        var assignmentsByEmployment = allAssignments.ToLookup(item => item.EmploymentId);
        var candidates = new List<ManagerCandidateDto>();

        foreach (var candidateEmployment in employments)
        {
            if (candidateEmployment.Id == employment.Id)
            {
                continue;
            }

            candidateEmployment.RefreshLifecycle(effectiveDate);
            if (!candidateEmployment.Period.Contains(effectiveDate) || candidateEmployment.IsEnded)
            {
                continue;
            }

            var candidateCovering = PrimaryAssignments.Covering(
                assignmentsByEmployment[candidateEmployment.Id].ToArray(),
                effectiveDate);
            if (candidateCovering is null)
            {
                continue;
            }

            if (!positionsById.TryGetValue(candidateCovering.PositionId, out var candidatePosition)
                || !ManagerHierarchy.IsEligibleDirectManager(candidatePosition, required.Value))
            {
                continue;
            }

            if (!employeesById.TryGetValue(candidateEmployment.EmployeeId, out var candidateEmployee)
                || candidateEmployee.OrganizationId != workplace.Value.Organization.Id)
            {
                continue;
            }

            departments.TryGetValue(candidateCovering.DepartmentId, out var candidateDepartment);
            candidates.Add(
                new ManagerCandidateDto(
                    candidateEmployee.Id,
                    candidateEmployment.Id,
                    candidateEmployee.PersonnelNumber,
                    candidateEmployee.GivenName,
                    candidateEmployee.FamilyName,
                    candidateDepartment?.Id,
                    candidateDepartment?.Name,
                    candidatePosition.Id,
                    candidatePosition.Name,
                    candidateDepartment?.PropertyId));
        }

        return candidates
            .OrderBy(item => item.FamilyName)
            .ThenBy(item => item.GivenName)
            .ToArray();
    }
}

public sealed record ManagerCandidateDto(
    Guid EmployeeId,
    Guid EmploymentId,
    string PersonnelNumber,
    string GivenName,
    string FamilyName,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid PositionId,
    string PositionName,
    Guid? PropertyId);
