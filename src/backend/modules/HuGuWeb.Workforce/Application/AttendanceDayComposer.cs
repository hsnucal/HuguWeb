using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// Maps a domain resolution plus authorized workplace facts into an HR-07B-ready day DTO.
/// Does not query the store. AcceptedWorkedMinutes is always null in HR-07A.
/// </summary>
internal static class AttendanceDayComposer
{
    public static AttendanceDayResult OutOfScope(DateOnly localDate) =>
        AttendanceDayResult.OutOfScope(localDate);

    public static AttendanceDayResult NotEmployed(DateOnly localDate) =>
        AttendanceDayResult.NotEmployed(localDate);

    public static AttendanceDayResult FromResolution(
        AttendanceDayResolution resolution,
        Guid employmentId,
        Guid assignmentId,
        Guid departmentId,
        string departmentName,
        ShiftDefinition? shiftDefinition,
        LeaveType? leaveType)
    {
        AttendanceDayScheduleDto? schedule = null;
        int? plannedMinutes = null;
        if (resolution.Schedule is { } entry)
        {
            if (entry.Kind == ScheduleEntryKind.RestDay)
            {
                schedule = AttendanceDayScheduleDto.RestDay(entry.Id);
            }
            else if (entry.Kind == ScheduleEntryKind.Shift)
            {
                if (shiftDefinition is not null)
                {
                    var interval = ShiftLocalInterval.From(resolution.LocalDate, shiftDefinition);
                    plannedMinutes = interval.PlannedNetMinutes;
                    schedule = AttendanceDayScheduleDto.Shift(entry, shiftDefinition, interval);
                }
                else
                {
                    schedule = AttendanceDayScheduleDto.ShiftMissingDefinition(entry);
                }
            }
        }
        else
        {
            schedule = AttendanceDayScheduleDto.Unscheduled();
        }

        AttendanceDayLeaveDto? leave = null;
        if (resolution.RecordedLeave is { } record)
        {
            leave = new AttendanceDayLeaveDto(
                record.Id,
                record.LeaveTypeId,
                leaveType?.Code,
                leaveType?.Name,
                leaveType?.SystemKind,
                record.StartDate,
                record.EndDate,
                record.Amount);
        }

        return new AttendanceDayResult(
            resolution.LocalDate,
            nameof(AttendanceCoverage.InEmployment),
            resolution.AcceptedKind?.ToString(),
            resolution.Source?.ToString(),
            resolution.IsProvisional,
            resolution.IsManual,
            resolution.IsUnresolved,
            resolution.CorrectionReason,
            employmentId,
            assignmentId,
            departmentId,
            departmentName,
            schedule,
            leave,
            plannedMinutes,
            AcceptedWorkedMinutes: null);
    }
}
