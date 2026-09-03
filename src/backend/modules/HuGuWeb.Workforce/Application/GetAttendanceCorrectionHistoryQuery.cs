using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class GetAttendanceCorrectionHistoryQuery(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<AttendanceCorrectionHistoryDto>> ExecuteAsync(
        Guid employmentId,
        DateOnly localDate,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        CancellationToken cancellationToken)
    {
        var target = await AttendanceTargetResolver.ResolveAsync(
            store,
            workplaceContext,
            employmentId,
            localDate,
            scopedPropertyId,
            allowedDepartmentIds,
            cancellationToken);
        if (!target.IsSuccess)
        {
            return target.Error!;
        }

        var changes = await store.ListAttendanceCorrectionChangesAsync(
            target.Value!.Employment.Id,
            localDate,
            cancellationToken);

        return new AttendanceCorrectionHistoryDto(
            target.Value.Employment.Id,
            localDate,
            changes
                .OrderBy(item => item.ChangedAtUtc)
                .ThenBy(item => item.Id)
                .Select(item => new AttendanceCorrectionHistoryItemDto(
                    item.Id,
                    item.ChangeType.ToString(),
                    item.PreviousKind?.ToString(),
                    item.NewKind?.ToString(),
                    item.PreviousReason,
                    item.NewReason,
                    item.ChangedByUserId,
                    item.ChangedAtUtc))
                .ToArray());
    }
}

public sealed record AttendanceCorrectionHistoryDto(
    Guid EmploymentId,
    DateOnly LocalDate,
    IReadOnlyList<AttendanceCorrectionHistoryItemDto> Changes);

public sealed record AttendanceCorrectionHistoryItemDto(
    Guid Id,
    string ChangeType,
    string? PreviousKind,
    string? NewKind,
    string? PreviousReason,
    string? NewReason,
    string ChangedByUserId,
    DateTimeOffset ChangedAtUtc);
