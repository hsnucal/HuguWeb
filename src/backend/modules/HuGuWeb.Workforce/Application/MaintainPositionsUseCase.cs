using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class MaintainPositionsUseCase(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<IReadOnlyList<PositionRecord>>> ListAsync(CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var positions = await store.ListPositionsAsync(workplace.Value.Property.Id, cancellationToken);
        var applicabilities = await store.ListApplicabilitiesForPositionsAsync(
            positions.Select(item => item.Id).ToArray(),
            cancellationToken);
        var departmentsByPosition = applicabilities
            .GroupBy(item => item.PositionId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Guid>)group.Select(item => item.DepartmentId).Distinct().ToArray());

        return positions
            .OrderBy(item => item.Name)
            .Select(item => ToRecord(
                item,
                departmentsByPosition.GetValueOrDefault(item.Id, [])))
            .ToArray();
    }

    public async Task<WorkforceResult<PositionRecord>> CreateAsync(
        CreatePositionCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        if (!Position.TryCreate(
                Guid.CreateVersion7(),
                workplace.Value.Property.Id,
                command.Name,
                command.Code,
                command.OrganizationalLevel,
                command.CanManageEmployees,
                out var position,
                out var error))
        {
            return WorkforceError.InvalidRequest("invalid-position", error!);
        }

        var departments = await ResolveDepartmentsAsync(
            store,
            workplace.Value.Property.Id,
            command.DepartmentIds,
            cancellationToken);
        if (!departments.IsSuccess)
        {
            return departments.Error!;
        }

        store.AddPosition(position!);
        foreach (var department in departments.Value!)
        {
            store.AddApplicability(new DepartmentPositionApplicability(department.Id, position!.Id));
        }

        await store.SaveChangesAsync(cancellationToken);
        return ToRecord(position!, departments.Value.Select(item => item.Id).ToArray());
    }

    public async Task<WorkforceResult<PositionRecord>> UpdateAsync(
        UpdatePositionCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var position = await store.GetPositionAsync(command.Id, cancellationToken);
        if (position is null || position.PropertyId != workplace.Value.Property.Id)
        {
            return WorkforceError.PositionNotFound();
        }

        if (command.Name is not null && !position.TryRename(command.Name, out var renameError))
        {
            return WorkforceError.InvalidRequest("invalid-position", renameError!);
        }

        if (command.CodeProvided && !position.TryChangeCode(command.Code, out var codeError))
        {
            return WorkforceError.InvalidRequest("invalid-position", codeError!);
        }

        if (command.OrganizationalLevel is { } organizationalLevel
            && !position.TrySetOrganizationalLevel(organizationalLevel, out var levelError))
        {
            return WorkforceError.InvalidRequest("invalid-position", levelError!);
        }

        if (command.CanManageEmployees is { } canManageEmployees)
        {
            position.SetCanManageEmployees(canManageEmployees);
        }

        if (command.IsActive is true)
        {
            position.Activate();
        }
        else if (command.IsActive is false)
        {
            position.Deactivate();
        }

        var current = await store.ListApplicabilitiesForPositionsAsync([position.Id], cancellationToken);
        IReadOnlyList<Guid> departmentIds;
        if (command.DepartmentIds is null)
        {
            departmentIds = current.Select(item => item.DepartmentId).ToArray();
        }
        else
        {
            var departments = await ResolveDepartmentsAsync(
                store,
                workplace.Value.Property.Id,
                command.DepartmentIds,
                cancellationToken);
            if (!departments.IsSuccess)
            {
                return departments.Error!;
            }

            var nextIds = departments.Value!.Select(item => item.Id).ToHashSet();
            foreach (var row in current)
            {
                if (!nextIds.Contains(row.DepartmentId))
                {
                    store.RemoveApplicability(row);
                }
            }

            var existingIds = current.Select(item => item.DepartmentId).ToHashSet();
            foreach (var department in departments.Value)
            {
                if (!existingIds.Contains(department.Id))
                {
                    store.AddApplicability(new DepartmentPositionApplicability(department.Id, position.Id));
                }
            }

            departmentIds = nextIds.ToArray();
        }

        await store.SaveChangesAsync(cancellationToken);
        return ToRecord(position, departmentIds);
    }

    private static async Task<WorkforceResult<IReadOnlyList<Department>>> ResolveDepartmentsAsync(
        IWorkforceStore store,
        Guid propertyId,
        IReadOnlyList<Guid>? departmentIds,
        CancellationToken cancellationToken)
    {
        if (departmentIds is null || departmentIds.Count == 0)
        {
            return Array.Empty<Department>();
        }

        var unique = departmentIds.Distinct().ToArray();
        var resolved = new List<Department>(unique.Length);
        foreach (var departmentId in unique)
        {
            var department = await store.GetDepartmentAsync(departmentId, cancellationToken);
            if (department is null || department.PropertyId != propertyId)
            {
                return WorkforceError.DepartmentNotFound();
            }

            resolved.Add(department);
        }

        return resolved;
    }

    private static PositionRecord ToRecord(Position position, IReadOnlyList<Guid> departmentIds) =>
        new(
            position.Id,
            position.PropertyId,
            position.Name,
            position.Code,
            position.IsActive,
            position.OrganizationalLevel,
            position.CanManageEmployees,
            departmentIds);
}

public sealed record CreatePositionCommand(
    string Name,
    string? Code,
    IReadOnlyList<Guid>? DepartmentIds,
    int OrganizationalLevel = Position.DefaultOrganizationalLevel,
    bool CanManageEmployees = false);

public sealed record UpdatePositionCommand(
    Guid Id,
    string? Name,
    string? Code,
    bool CodeProvided,
    bool? IsActive,
    IReadOnlyList<Guid>? DepartmentIds,
    int? OrganizationalLevel = null,
    bool? CanManageEmployees = null);

public sealed record PositionRecord(
    Guid Id,
    Guid PropertyId,
    string Name,
    string? Code,
    bool IsActive,
    int OrganizationalLevel,
    bool CanManageEmployees,
    IReadOnlyList<Guid> ApplicableDepartmentIds);
