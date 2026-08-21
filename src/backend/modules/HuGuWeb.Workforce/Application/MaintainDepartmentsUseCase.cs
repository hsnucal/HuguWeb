using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class MaintainDepartmentsUseCase(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<IReadOnlyList<DepartmentRecord>>> ListAsync(CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var departments = await store.ListDepartmentsAsync(workplace.Value.Property.Id, cancellationToken);
        return departments
            .OrderBy(item => item.Name)
            .Select(ToRecord)
            .ToArray();
    }

    public async Task<WorkforceResult<DepartmentRecord>> CreateAsync(
        CreateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        if (!Department.TryCreate(
                Guid.CreateVersion7(),
                workplace.Value.Property.Id,
                command.Name,
                command.Code,
                out var department,
                out var error))
        {
            return WorkforceError.InvalidRequest("invalid-department", error!);
        }

        store.AddDepartment(department!);
        await store.SaveChangesAsync(cancellationToken);
        return ToRecord(department!);
    }

    public async Task<WorkforceResult<DepartmentRecord>> UpdateAsync(
        UpdateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var department = await store.GetDepartmentAsync(command.Id, cancellationToken);
        if (department is null || department.PropertyId != workplace.Value.Property.Id)
        {
            return WorkforceError.DepartmentNotFound();
        }

        if (command.Name is not null && !department.TryRename(command.Name, out var renameError))
        {
            return WorkforceError.InvalidRequest("invalid-department", renameError!);
        }

        if (command.CodeProvided && !department.TryChangeCode(command.Code, out var codeError))
        {
            return WorkforceError.InvalidRequest("invalid-department", codeError!);
        }

        if (command.IsActive is true)
        {
            department.Activate();
        }
        else if (command.IsActive is false)
        {
            department.Deactivate();
        }

        await store.SaveChangesAsync(cancellationToken);
        return ToRecord(department);
    }

    private static DepartmentRecord ToRecord(Department department) =>
        new(department.Id, department.PropertyId, department.Name, department.Code, department.IsActive);
}

public sealed record CreateDepartmentCommand(string Name, string? Code);

public sealed record UpdateDepartmentCommand(Guid Id, string? Name, string? Code, bool CodeProvided, bool? IsActive);

public sealed record DepartmentRecord(Guid Id, Guid PropertyId, string Name, string? Code, bool IsActive);
