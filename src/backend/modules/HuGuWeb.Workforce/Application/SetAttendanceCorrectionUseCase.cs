using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class SetAttendanceCorrectionUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<AttendanceDayResult>> ExecuteAsync(
        SetAttendanceCorrectionCommand command,
        CancellationToken cancellationToken)
    {
        if (!AttendanceCorrection.TryParseKind(command.Kind, out var kind))
        {
            return WorkforceError.AttendanceValidationField(
                AttendanceValidation.Fields.Kind,
                AttendanceValidation.Codes.AttendanceCorrectionKindInvalid,
                "Attendance correction kind must be Worked, Leave, RestDay, or Absent.");
        }

        var target = await AttendanceTargetResolver.ResolveAsync(
            store,
            workplaceContext,
            command.EmploymentId,
            command.LocalDate,
            command.ScopedPropertyId,
            command.AllowedDepartmentIds,
            cancellationToken);
        if (!target.IsSuccess)
        {
            return target.Error!;
        }

        var existing = await store.GetAttendanceCorrectionAsync(
            target.Value!.Employment.Id,
            command.LocalDate,
            cancellationToken);

        if (existing is not null
            && existing.HasSameCurrentValues(target.Value.Assignment.Id, kind, command.Reason))
        {
            return await AttendanceDayLoader.LoadAsync(store, target.Value, command.LocalDate, cancellationToken);
        }

        if (existing is null)
        {
            if (!AttendanceCorrection.TryCreate(
                    Guid.CreateVersion7(),
                    target.Value.OrganizationId,
                    target.Value.Property.Id,
                    target.Value.Employment.Id,
                    target.Value.Assignment.Id,
                    command.LocalDate,
                    kind,
                    command.Reason,
                    command.ActorUserId,
                    clock.UtcNow,
                    out var created,
                    out var field,
                    out var errorCode))
            {
                return WorkforceError.AttendanceValidationField(field!, errorCode!, "Attendance correction is invalid.");
            }

            store.AddAttendanceCorrection(created!);
            store.AddAttendanceCorrectionChange(
                AttendanceCorrectionChange.RecordSet(
                    Guid.CreateVersion7(),
                    target.Value.Employment.Id,
                    command.LocalDate,
                    created!.Id,
                    previousKind: null,
                    previousReason: null,
                    created.Kind,
                    created.Reason,
                    command.ActorUserId,
                    clock.UtcNow));
            await store.SaveChangesAsync(cancellationToken);
            return await AttendanceDayLoader.LoadAsync(store, target.Value, command.LocalDate, cancellationToken);
        }

        var previousKind = existing.Kind;
        var previousReason = existing.Reason;
        if (!existing.TryReplace(
                target.Value.Assignment.Id,
                kind,
                command.Reason,
                command.ActorUserId,
                clock.UtcNow,
                out var replaceField,
                out var replaceCode))
        {
            return WorkforceError.AttendanceValidationField(
                replaceField!,
                replaceCode!,
                "Attendance correction is invalid.");
        }

        store.AddAttendanceCorrectionChange(
            AttendanceCorrectionChange.RecordSet(
                Guid.CreateVersion7(),
                target.Value.Employment.Id,
                command.LocalDate,
                existing.Id,
                previousKind,
                previousReason,
                existing.Kind,
                existing.Reason,
                command.ActorUserId,
                clock.UtcNow));
        await store.SaveChangesAsync(cancellationToken);
        return await AttendanceDayLoader.LoadAsync(store, target.Value, command.LocalDate, cancellationToken);
    }
}

public sealed record SetAttendanceCorrectionCommand(
    Guid EmploymentId,
    DateOnly LocalDate,
    string? Kind,
    string? Reason,
    string ActorUserId,
    Guid? ScopedPropertyId,
    IReadOnlySet<Guid>? AllowedDepartmentIds = null);
