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
        return positions
            .OrderBy(item => item.Name)
            .Select(ToRecord)
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
                out var position,
                out var error))
        {
            return WorkforceError.InvalidRequest("invalid-position", error!);
        }

        store.AddPosition(position!);
        await store.SaveChangesAsync(cancellationToken);
        return ToRecord(position!);
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

        if (command.IsActive is true)
        {
            position.Activate();
        }
        else if (command.IsActive is false)
        {
            position.Deactivate();
        }

        await store.SaveChangesAsync(cancellationToken);
        return ToRecord(position);
    }

    private static PositionRecord ToRecord(Position position) =>
        new(position.Id, position.PropertyId, position.Name, position.Code, position.IsActive);
}

public sealed record CreatePositionCommand(string Name, string? Code);

public sealed record UpdatePositionCommand(Guid Id, string? Name, string? Code, bool CodeProvided, bool? IsActive);

public sealed record PositionRecord(Guid Id, Guid PropertyId, string Name, string? Code, bool IsActive);
