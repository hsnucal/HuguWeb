using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class UpsertScheduleEntryUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public Task<WorkforceResult<ScheduleStateDto>> ExecuteAsync(
        UpsertScheduleEntryCommand command,
        CancellationToken cancellationToken) =>
        ExecuteCoreAsync(command, saveChanges: true, cancellationToken);

    /// <summary>
    /// Applies a single-cell upsert using the same domain rules as <see cref="ExecuteAsync"/>.
    /// When <paramref name="saveChanges"/> is false, callers (bulk/copy) own the transaction boundary.
    /// </summary>
    internal async Task<WorkforceResult<ScheduleStateDto>> ExecuteCoreAsync(
        UpsertScheduleEntryCommand command,
        bool saveChanges,
        CancellationToken cancellationToken)
    {
        if (!workplaceContext.HasOrganization)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        var employee = await store.GetEmployeeAsync(command.EmployeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplaceContext.OrganizationId)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var employments = await store.ListEmploymentsAsync(command.EmployeeId, cancellationToken);
        var employmentResult = ScheduleEmploymentResolver.ResolveCovering(employments, command.ScheduleDate);
        if (!employmentResult.IsSuccess)
        {
            return employmentResult.Error!;
        }

        var workplace = await ScheduleWorkplaceResolver.ResolveAsync(
            store,
            employmentResult.Value!,
            command.ScheduleDate,
            cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        if (!ScheduleAccess.AllowsWorkplace(
                command.ScopedPropertyId,
                command.AllowedDepartmentIds,
                workplace.Value!.Property.Id,
                workplace.Value.Department.Id))
        {
            return WorkforceError.SchedulePropertyAccessDenied();
        }

        ShiftDefinition? definition = null;
        if (command.Kind == ScheduleEntryKind.Shift)
        {
            if (command.ShiftDefinitionId is not { } shiftDefinitionId || shiftDefinitionId == Guid.Empty)
            {
                return WorkforceError.ScheduleValidationField(
                    ScheduleValidation.Fields.ShiftDefinitionId,
                    ScheduleValidation.Codes.ScheduleShiftDefinitionRequired,
                    "A shift definition is required for Kind=Shift.");
            }

            definition = await store.GetShiftDefinitionAsync(shiftDefinitionId, cancellationToken);
            if (definition is null)
            {
                return WorkforceError.ShiftDefinitionNotFound();
            }

            if (!definition.IsActive)
            {
                return WorkforceError.ShiftDefinitionInactive();
            }

            if (definition.PropertyId != workplace.Value!.Property.Id)
            {
                return WorkforceError.ScheduleCrossPropertyShift();
            }
        }
        else if (command.Kind == ScheduleEntryKind.RestDay)
        {
            if (command.ShiftDefinitionId is not null)
            {
                return WorkforceError.ScheduleValidationField(
                    ScheduleValidation.Fields.ShiftDefinitionId,
                    ScheduleValidation.Codes.ScheduleShiftDefinitionMustBeNull,
                    "RestDay cannot reference a shift definition.");
            }
        }
        else
        {
            return WorkforceError.ScheduleValidationField(
                ScheduleValidation.Fields.Kind,
                ScheduleValidation.Codes.ScheduleInvalidKind,
                "Schedule kind must be Shift or RestDay.");
        }

        var existing = await store.GetScheduleEntryAsync(
            workplace.Value!.Employment.Id,
            command.ScheduleDate,
            cancellationToken);

        if (existing is null)
        {
            ScheduleEntry? created;
            string? field;
            string? errorCode;
            if (command.Kind == ScheduleEntryKind.Shift)
            {
                if (!ScheduleEntry.TryCreateShift(
                        Guid.CreateVersion7(),
                        workplace.Value.Employment.Id,
                        workplace.Value.Assignment.Id,
                        command.ScheduleDate,
                        definition!.Id,
                        command.Note,
                        command.ActorUserId,
                        clock.UtcNow,
                        out created,
                        out field,
                        out errorCode))
                {
                    return WorkforceError.ScheduleValidationField(field!, errorCode!, "Schedule entry is invalid.");
                }
            }
            else if (!ScheduleEntry.TryCreateRestDay(
                         Guid.CreateVersion7(),
                         workplace.Value.Employment.Id,
                         workplace.Value.Assignment.Id,
                         command.ScheduleDate,
                         command.Note,
                         command.ActorUserId,
                         clock.UtcNow,
                         out created,
                         out field,
                         out errorCode))
            {
                return WorkforceError.ScheduleValidationField(field!, errorCode!, "Schedule entry is invalid.");
            }

            store.AddScheduleEntry(created!);
            store.AddScheduleEntryChange(
                ScheduleEntryChange.FromMutation(
                    Guid.CreateVersion7(),
                    previous: null,
                    next: created,
                    workplace.Value.Employment.Id,
                    command.ScheduleDate,
                    command.ActorUserId,
                    clock.UtcNow));
            if (saveChanges)
            {
                await store.SaveChangesAsync(cancellationToken);
            }

            return await GetScheduleStateQuery.BuildStateAsync(store, created!, workplace.Value, definition, cancellationToken);
        }

        var unchanged = existing.Kind == command.Kind
            && existing.ShiftDefinitionId == (command.Kind == ScheduleEntryKind.Shift ? definition!.Id : null)
            && string.Equals(existing.Note, NormalizeNoteForCompare(command.Note), StringComparison.Ordinal)
            && existing.AssignmentId == workplace.Value.Assignment.Id;
        if (unchanged)
        {
            return await GetScheduleStateQuery.BuildStateAsync(store, existing, workplace.Value, definition, cancellationToken);
        }

        var previousKind = existing.Kind;
        var previousShiftId = existing.ShiftDefinitionId;

        if (command.Kind == ScheduleEntryKind.Shift)
        {
            if (!existing.TryAssignShift(
                    workplace.Value.Assignment.Id,
                    definition!.Id,
                    command.Note,
                    command.ActorUserId,
                    clock.UtcNow,
                    out var field,
                    out var errorCode))
            {
                return WorkforceError.ScheduleValidationField(field!, errorCode!, "Schedule entry is invalid.");
            }
        }
        else if (!existing.TryMarkRestDay(
                     workplace.Value.Assignment.Id,
                     command.Note,
                     command.ActorUserId,
                     clock.UtcNow,
                     out var restField,
                     out var restError))
        {
            return WorkforceError.ScheduleValidationField(restField!, restError!, "Schedule entry is invalid.");
        }

        store.AddScheduleEntryChange(
            ScheduleEntryChange.Record(
                Guid.CreateVersion7(),
                workplace.Value.Employment.Id,
                command.ScheduleDate,
                existing.Id,
                previousKind,
                previousShiftId,
                existing.Kind,
                existing.ShiftDefinitionId,
                command.ActorUserId,
                clock.UtcNow));
        if (saveChanges)
        {
            await store.SaveChangesAsync(cancellationToken);
        }

        return await GetScheduleStateQuery.BuildStateAsync(store, existing, workplace.Value, definition, cancellationToken);
    }

    private static string? NormalizeNoteForCompare(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}

public sealed record UpsertScheduleEntryCommand(
    Guid EmployeeId,
    DateOnly ScheduleDate,
    ScheduleEntryKind Kind,
    Guid? ShiftDefinitionId,
    string? Note,
    string ActorUserId,
    Guid? ScopedPropertyId,
    IReadOnlySet<Guid>? AllowedDepartmentIds = null);
