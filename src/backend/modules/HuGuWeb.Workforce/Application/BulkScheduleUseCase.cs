using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// HR-06B bulk schedule mutations. All-or-nothing transaction; each op re-runs full Upsert/Clear rules.
/// </summary>
public sealed class BulkScheduleUseCase(
    IWorkforceStore store,
    UpsertScheduleEntryUseCase upsert,
    ClearScheduleEntryUseCase clear)
{
    public async Task<WorkforceResult<BulkScheduleResultDto>> ExecuteAsync(
        BulkScheduleCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Operations.Count == 0)
        {
            return WorkforceError.ScheduleValidationField(
                ScheduleValidation.Fields.Operations,
                ScheduleValidation.Codes.ScheduleBulkFailed,
                "At least one schedule operation is required.");
        }

        await using var transaction = await store.BeginTransactionAsync(cancellationToken);
        var applied = new List<BulkScheduleOperationResultDto>(command.Operations.Count);

        for (var index = 0; index < command.Operations.Count; index++)
        {
            var operation = command.Operations[index];
            WorkforceResult<ScheduleStateDto> result;
            if (operation.Clear)
            {
                result = await clear.ExecuteCoreAsync(
                    new ClearScheduleEntryCommand(
                        operation.EmployeeId,
                        operation.Date,
                        command.ActorUserId,
                        command.ScopedPropertyId,
                        command.AllowedDepartmentIds),
                    saveChanges: false,
                    cancellationToken);
            }
            else
            {
                if (operation.Kind is not { } kind
                    || (kind != ScheduleEntryKind.Shift && kind != ScheduleEntryKind.RestDay))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return WorkforceError.ScheduleBulkOperationFailed(
                        index,
                        operation.EmployeeId,
                        operation.Date,
                        WorkforceError.ScheduleValidationField(
                            ScheduleValidation.Fields.Kind,
                            ScheduleValidation.Codes.ScheduleInvalidKind,
                            "Schedule kind must be Shift or RestDay."));
                }

                result = await upsert.ExecuteCoreAsync(
                    new UpsertScheduleEntryCommand(
                        operation.EmployeeId,
                        operation.Date,
                        kind,
                        operation.ShiftDefinitionId,
                        operation.Note,
                        command.ActorUserId,
                        command.ScopedPropertyId,
                        command.AllowedDepartmentIds),
                    saveChanges: false,
                    cancellationToken);
            }

            if (!result.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);
                return WorkforceError.ScheduleBulkOperationFailed(
                    index,
                    operation.EmployeeId,
                    operation.Date,
                    result.Error!);
            }

            applied.Add(new BulkScheduleOperationResultDto(
                index,
                operation.EmployeeId,
                operation.Date,
                result.Value!));
        }

        await store.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new BulkScheduleResultDto(applied);
    }
}

public sealed record BulkScheduleCommand(
    IReadOnlyList<BulkScheduleOperation> Operations,
    string ActorUserId,
    Guid? ScopedPropertyId,
    IReadOnlySet<Guid>? AllowedDepartmentIds = null);

public sealed record BulkScheduleOperation(
    Guid EmployeeId,
    DateOnly Date,
    bool Clear,
    ScheduleEntryKind? Kind,
    Guid? ShiftDefinitionId,
    string? Note);

public sealed record BulkScheduleResultDto(IReadOnlyList<BulkScheduleOperationResultDto> Results);

public sealed record BulkScheduleOperationResultDto(
    int Index,
    Guid EmployeeId,
    DateOnly Date,
    ScheduleStateDto State);
