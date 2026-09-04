using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class ListPersonnelMovementsQuery(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<IReadOnlyList<PersonnelMovementListItemDto>>> ExecuteAsync(
        ListPersonnelMovementsFilter filter,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var organizationId = workplace.Value.Organization.Id;
        if (filter.PropertyId is { } requestedProperty
            && filter.AccessiblePropertyIds is not null
            && !filter.AccessiblePropertyIds.Contains(requestedProperty))
        {
            return Array.Empty<PersonnelMovementListItemDto>();
        }

        IReadOnlyCollection<Guid>? employmentIds = null;
        if (filter.EmployeeId is { } employeeId)
        {
            var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
            if (employee is null || employee.OrganizationId != organizationId)
            {
                return Array.Empty<PersonnelMovementListItemDto>();
            }

            employmentIds = (await store.ListEmploymentsAsync(employee.Id, cancellationToken))
                .Select(item => item.Id)
                .ToArray();
            if (employmentIds.Count == 0)
            {
                return Array.Empty<PersonnelMovementListItemDto>();
            }
        }
        else if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var needle = filter.Search.Trim();
            var employees = await store.ListEmployeesAsync(organizationId, cancellationToken);
            var matches = employees
                .Where(item =>
                    item.PersonnelNumber.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || item.GivenName.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || item.FamilyName.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || $"{item.GivenName} {item.FamilyName}".Contains(needle, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Id)
                .ToArray();
            if (matches.Length == 0)
            {
                return Array.Empty<PersonnelMovementListItemDto>();
            }

            employmentIds = (await store.ListEmploymentsForEmployeesAsync(matches, cancellationToken))
                .Select(item => item.Id)
                .ToArray();
            if (employmentIds.Count == 0)
            {
                return Array.Empty<PersonnelMovementListItemDto>();
            }
        }

        var movements = await store.ListPersonnelMovementsAsync(
            organizationId,
            filter.DateFrom,
            filter.DateTo,
            filter.Type,
            employmentIds,
            cancellationToken);

        var items = new List<PersonnelMovementListItemDto>();
        foreach (var movement in movements)
        {
            var item = await PersonnelMovementComposer.ComposeListItemAsync(
                store,
                movement,
                calendarProperty: null,
                clock.UtcNow,
                cancellationToken);

            if (filter.DepartmentId is { } departmentId
                && item.PreviousAssignment?.DepartmentId != departmentId
                && item.NewAssignment?.DepartmentId != departmentId)
            {
                continue;
            }

            var propertyIds = PersonnelMovementComposer.PropertyIdsOf(item).ToHashSet();
            if (item.PreviousAssignment is null && item.NewAssignment is null)
            {
                var employment = await store.GetEmploymentAsync(movement.EmploymentId, cancellationToken);
                if (employment is not null)
                {
                    var assignments = await store.ListAssignmentsAsync(employment.Id, cancellationToken);
                    var covering = PrimaryAssignments.Covering(assignments, movement.EffectiveDate);
                    if (covering is not null)
                    {
                        var department = await store.GetDepartmentAsync(covering.DepartmentId, cancellationToken);
                        if (department is not null)
                        {
                            propertyIds.Add(department.PropertyId);
                            if (filter.DepartmentId is { } managerDept
                                && covering.DepartmentId != managerDept)
                            {
                                continue;
                            }
                        }
                    }
                }
            }

            if (filter.AccessiblePropertyIds is not null
                && (propertyIds.Count == 0
                    || !propertyIds.Overlaps(filter.AccessiblePropertyIds)))
            {
                continue;
            }

            if (filter.PropertyId is { } propertyId
                && (propertyIds.Count == 0 || !propertyIds.Contains(propertyId)))
            {
                continue;
            }

            items.Add(item);
        }

        return items;
    }
}

public sealed record ListPersonnelMovementsFilter(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    PersonnelMovementType? Type,
    Guid? DepartmentId,
    Guid? EmployeeId,
    string? Search,
    IReadOnlySet<Guid>? AccessiblePropertyIds,
    Guid? PropertyId = null);

public sealed class GetPersonnelMovementQuery(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<PersonnelMovementDetailDto>> ExecuteAsync(
        Guid movementId,
        IReadOnlySet<Guid>? accessiblePropertyIds,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var movement = await store.GetPersonnelMovementAsync(movementId, cancellationToken);
        if (movement is null || movement.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.MovementNotFound();
        }

        var detail = await PersonnelMovementComposer.ComposeAsync(
            store,
            movement,
            calendarProperty: null,
            clock.UtcNow,
            cancellationToken);

        var list = new ListPersonnelMovementsQuery(store, clock, workplaceContext);
        var filtered = await list.ExecuteAsync(
            new ListPersonnelMovementsFilter(
                movement.EffectiveDate,
                movement.EffectiveDate,
                movement.MovementType,
                DepartmentId: null,
                EmployeeId: detail.EmployeeId,
                Search: null,
                accessiblePropertyIds,
                PropertyId: null),
            cancellationToken);
        if (!filtered.IsSuccess || filtered.Value!.All(item => item.Id != movement.Id))
        {
            return WorkforceError.MovementNotFound();
        }

        return detail;
    }
}
