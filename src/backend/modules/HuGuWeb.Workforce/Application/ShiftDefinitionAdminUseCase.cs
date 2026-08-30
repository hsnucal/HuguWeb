using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class ShiftDefinitionAdminUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<IReadOnlyList<ShiftDefinitionDto>>> ListAsync(
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var definitions = await store.ListShiftDefinitionsAsync(workplace.Value.Property.Id, cancellationToken);
        var filtered = activeOnly ? definitions.Where(item => item.IsActive) : definitions;
        var usageIds = await store.ListShiftDefinitionIdsWithUsageAsync(
            filtered.Select(item => item.Id).ToArray(),
            cancellationToken);
        var used = usageIds.ToHashSet();

        return WorkforceResult<IReadOnlyList<ShiftDefinitionDto>>.Success(
            filtered
                .OrderByDescending(item => item.IsActive)
                .ThenBy(item => item.Name)
                .Select(item => ShiftDefinitionDto.From(item, used.Contains(item.Id)))
                .ToArray());
    }

    public async Task<WorkforceResult<ShiftDefinitionDto>> CreateAsync(
        CreateShiftDefinitionCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var propertyId = workplace.Value.Property.Id;
        if (!ShiftDefinition.TryNormalizeCode(command.Code, out var normalizedCode, out var codeField, out var codeError))
        {
            return WorkforceError.ScheduleValidationField(codeField!, codeError!, "Shift definition code is invalid.");
        }

        var existing = await store.FindShiftDefinitionByCodeAsync(propertyId, normalizedCode, cancellationToken);
        if (existing is not null)
        {
            return WorkforceError.ShiftDefinitionCodeExists();
        }

        if (!ShiftDefinition.TryCreate(
                Guid.CreateVersion7(),
                propertyId,
                command.Code,
                command.Name,
                command.StartLocalTime,
                command.EndLocalTime,
                command.EndsNextDay,
                command.BreakMinutes,
                command.ActorUserId,
                clock.UtcNow,
                out var definition,
                out var field,
                out var errorCode))
        {
            return WorkforceError.ScheduleValidationField(field!, errorCode!, "Shift definition is invalid.");
        }

        store.AddShiftDefinition(definition!);
        await store.SaveChangesAsync(cancellationToken);
        return ShiftDefinitionDto.From(definition!, semanticFieldsLocked: false);
    }

    public async Task<WorkforceResult<ShiftDefinitionDto>> UpdateAsync(
        UpdateShiftDefinitionCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var definition = await store.GetShiftDefinitionAsync(command.ShiftDefinitionId, cancellationToken);
        if (definition is null || definition.PropertyId != workplace.Value.Property.Id)
        {
            return WorkforceError.ShiftDefinitionNotFound();
        }

        var hasUsage = await store.ShiftDefinitionHasUsageAsync(definition.Id, cancellationToken);

        if (command.Name is not null
            && !definition.TryRename(command.Name, command.ActorUserId, clock.UtcNow, out var nameField, out var nameError))
        {
            return WorkforceError.ScheduleValidationField(nameField!, nameError!, "Shift definition name is invalid.");
        }

        if (command.StartLocalTime is { } start
            || command.EndLocalTime is { } end
            || command.EndsNextDay is { }
            || command.BreakMinutes is { })
        {
            var nextStart = command.StartLocalTime ?? definition.StartLocalTime;
            var nextEnd = command.EndLocalTime ?? definition.EndLocalTime;
            var nextEndsNextDay = command.EndsNextDay ?? definition.EndsNextDay;
            var nextBreak = command.BreakMinutes ?? definition.BreakMinutes;

            if (!definition.TryUpdateSemanticTimes(
                    nextStart,
                    nextEnd,
                    nextEndsNextDay,
                    nextBreak,
                    hasUsage,
                    command.ActorUserId,
                    clock.UtcNow,
                    out var timeField,
                    out var timeError))
            {
                return WorkforceError.ScheduleValidationField(
                    timeField!,
                    timeError!,
                    "Shift definition time fields are invalid or locked after schedule use.");
            }
        }

        if (command.IsActive is { } isActive)
        {
            definition.SetActive(isActive, command.ActorUserId, clock.UtcNow);
        }

        await store.SaveChangesAsync(cancellationToken);
        return ShiftDefinitionDto.From(definition, hasUsage);
    }
}

public sealed record CreateShiftDefinitionCommand(
    string? Code,
    string? Name,
    TimeOnly StartLocalTime,
    TimeOnly EndLocalTime,
    bool EndsNextDay,
    int BreakMinutes,
    string ActorUserId);

public sealed record UpdateShiftDefinitionCommand(
    Guid ShiftDefinitionId,
    string? Name,
    TimeOnly? StartLocalTime,
    TimeOnly? EndLocalTime,
    bool? EndsNextDay,
    int? BreakMinutes,
    bool? IsActive,
    string ActorUserId);

public sealed record ShiftDefinitionDto(
    Guid Id,
    Guid PropertyId,
    string Code,
    string Name,
    TimeOnly StartLocalTime,
    TimeOnly EndLocalTime,
    bool EndsNextDay,
    int BreakMinutes,
    int GrossMinutes,
    int PlannedNetMinutes,
    bool IsActive,
    bool SemanticFieldsLocked)
{
    public static ShiftDefinitionDto From(ShiftDefinition definition, bool semanticFieldsLocked) =>
        new(
            definition.Id,
            definition.PropertyId,
            definition.Code,
            definition.Name,
            definition.StartLocalTime,
            definition.EndLocalTime,
            definition.EndsNextDay,
            definition.BreakMinutes,
            definition.GrossMinutes,
            definition.PlannedNetMinutes,
            definition.IsActive,
            semanticFieldsLocked);
}
