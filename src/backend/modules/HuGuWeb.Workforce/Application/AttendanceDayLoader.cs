using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

internal static class AttendanceDayLoader
{
    public static async Task<AttendanceDayResult> LoadAsync(
        IWorkforceStore store,
        AttendanceTarget target,
        DateOnly localDate,
        CancellationToken cancellationToken)
    {
        var correction = await store.GetAttendanceCorrectionAsync(
            target.Employment.Id,
            localDate,
            cancellationToken);
        var records = await store.ListLeaveRecordsAsync(target.Employment.Id, cancellationToken);
        var leave = AttendanceDayResolver.SelectCoveringRecordedLeave(records, localDate);
        var schedule = await store.GetScheduleEntryAsync(target.Employment.Id, localDate, cancellationToken);

        ShiftDefinition? shiftDefinition = null;
        if (schedule is { Kind: ScheduleEntryKind.Shift, ShiftDefinitionId: { } shiftId })
        {
            shiftDefinition = await store.GetShiftDefinitionAsync(shiftId, cancellationToken);
        }

        LeaveType? leaveType = null;
        if (leave is not null)
        {
            leaveType = await store.GetLeaveTypeAsync(leave.LeaveTypeId, cancellationToken);
        }

        var resolution = AttendanceDayResolver.ResolveInEmployment(localDate, correction, leave, schedule);
        return AttendanceDayComposer.FromResolution(
            resolution,
            target.Employment.Id,
            target.Assignment.Id,
            target.Department.Id,
            target.Department.Name,
            shiftDefinition,
            leaveType);
    }
}
