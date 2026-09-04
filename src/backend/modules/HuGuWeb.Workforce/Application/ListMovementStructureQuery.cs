using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class ListMovementStructureQuery(IWorkforceStore store, IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<MovementStructureDto>> ExecuteAsync(
        Guid propertyId,
        IReadOnlySet<Guid>? accessiblePropertyIds,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var property = await store.GetPropertyAsync(propertyId, cancellationToken);
        if (property is null || property.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.NotFound("property-not-found", "The property was not found.");
        }

        if (!MovementPropertyAccess.CanAccess(accessiblePropertyIds, property.Id))
        {
            return WorkforceError.MovementPropertyAccessDenied();
        }

        var departments = await store.ListDepartmentsAsync(property.Id, cancellationToken);
        var positions = await store.ListPositionsAsync(property.Id, cancellationToken);
        var applicabilities = await store.ListApplicabilitiesForPositionsAsync(
            positions.Select(item => item.Id).ToArray(),
            cancellationToken);
        var departmentsByPosition = applicabilities
            .GroupBy(item => item.PositionId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Guid>)group.Select(item => item.DepartmentId).Distinct().ToArray());

        return new MovementStructureDto(
            property.Id,
            property.Name,
            departments
                .OrderBy(item => item.Name)
                .Select(item => new DepartmentRecord(item.Id, item.PropertyId, item.Name, item.Code, item.IsActive))
                .ToArray(),
            positions
                .OrderBy(item => item.Name)
                .Select(item => new PositionRecord(
                    item.Id,
                    item.PropertyId,
                    item.Name,
                    item.Code,
                    item.IsActive,
                    item.OrganizationalLevel,
                    item.CanManageEmployees,
                    departmentsByPosition.GetValueOrDefault(item.Id, [])))
                .ToArray());
    }
}

public sealed record MovementStructureDto(
    Guid PropertyId,
    string PropertyName,
    IReadOnlyList<DepartmentRecord> Departments,
    IReadOnlyList<PositionRecord> Positions);
