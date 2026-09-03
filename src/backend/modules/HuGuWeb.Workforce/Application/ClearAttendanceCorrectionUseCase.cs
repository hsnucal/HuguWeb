using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class ClearAttendanceCorrectionUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    /// <summary>
    /// Removes the current override so resolution falls back to Schedule/Leave.
    /// Already-cleared is a stable no-op success. History is retained.
    /// </summary>
    public async Task<WorkforceResult<AttendanceDayResult>> ExecuteAsync(
        ClearAttendanceCorrectionCommand command,
        CancellationToken cancellationToken)
    {
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

        if (existing is null)
        {
            return await AttendanceDayLoader.LoadAsync(store, target.Value, command.LocalDate, cancellationToken);
        }

        store.AddAttendanceCorrectionChange(
            AttendanceCorrectionChange.RecordClear(
                Guid.CreateVersion7(),
                target.Value.Employment.Id,
                command.LocalDate,
                existing.Id,
                existing.Kind,
                existing.Reason,
                command.ActorUserId,
                clock.UtcNow));
        store.RemoveAttendanceCorrection(existing);
        await store.SaveChangesAsync(cancellationToken);
        return await AttendanceDayLoader.LoadAsync(store, target.Value, command.LocalDate, cancellationToken);
    }
}

public sealed record ClearAttendanceCorrectionCommand(
    Guid EmploymentId,
    DateOnly LocalDate,
    string ActorUserId,
    Guid? ScopedPropertyId,
    IReadOnlySet<Guid>? AllowedDepartmentIds = null);
