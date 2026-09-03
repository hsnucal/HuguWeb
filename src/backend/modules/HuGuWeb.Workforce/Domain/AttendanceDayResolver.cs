namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Deterministic Puantaj resolution. Does not mutate ScheduleEntry or LeaveRecord.
/// Absent is never inferred. A planned Shift is provisional Worked, not observed attendance.
/// AcceptedWorkedMinutes stays null in HR-07A (no punch times).
/// </summary>
public static class AttendanceDayResolver
{
    public static AttendanceDayResolution NotEmployed(DateOnly localDate) =>
        new(
            localDate,
            AttendanceCoverage.NotEmployed,
            AcceptedKind: null,
            Source: null,
            IsProvisional: false,
            IsManual: false,
            IsUnresolved: false,
            CorrectionReason: null,
            RecordedLeave: null,
            Schedule: null);

    public static AttendanceDayResolution ResolveInEmployment(
        DateOnly localDate,
        AttendanceCorrection? correction,
        LeaveRecord? recordedLeaveCoveringDate,
        ScheduleEntry? schedule)
    {
        if (correction is not null)
        {
            return new AttendanceDayResolution(
                localDate,
                AttendanceCoverage.InEmployment,
                correction.AcceptedKind,
                AttendanceSource.Manual,
                IsProvisional: false,
                IsManual: true,
                IsUnresolved: false,
                correction.Reason,
                recordedLeaveCoveringDate,
                schedule);
        }

        if (recordedLeaveCoveringDate is not null
            && recordedLeaveCoveringDate.CoversCalendarDate(localDate))
        {
            return new AttendanceDayResolution(
                localDate,
                AttendanceCoverage.InEmployment,
                AttendanceAcceptedKind.Leave,
                AttendanceSource.Leave,
                IsProvisional: false,
                IsManual: false,
                IsUnresolved: false,
                CorrectionReason: null,
                recordedLeaveCoveringDate,
                schedule);
        }

        if (schedule is { Kind: ScheduleEntryKind.RestDay })
        {
            return new AttendanceDayResolution(
                localDate,
                AttendanceCoverage.InEmployment,
                AttendanceAcceptedKind.RestDay,
                AttendanceSource.Schedule,
                IsProvisional: false,
                IsManual: false,
                IsUnresolved: false,
                CorrectionReason: null,
                RecordedLeave: null,
                schedule);
        }

        if (schedule is { Kind: ScheduleEntryKind.Shift })
        {
            return new AttendanceDayResolution(
                localDate,
                AttendanceCoverage.InEmployment,
                AttendanceAcceptedKind.Worked,
                AttendanceSource.Schedule,
                IsProvisional: true,
                IsManual: false,
                IsUnresolved: false,
                CorrectionReason: null,
                RecordedLeave: null,
                schedule);
        }

        return new AttendanceDayResolution(
            localDate,
            AttendanceCoverage.InEmployment,
            AttendanceAcceptedKind.Unresolved,
            Source: null,
            IsProvisional: false,
            IsManual: false,
            IsUnresolved: true,
            CorrectionReason: null,
            RecordedLeave: null,
            Schedule: null);
    }

    public static LeaveRecord? SelectCoveringRecordedLeave(
        IEnumerable<LeaveRecord> records,
        DateOnly localDate) =>
        records
            .Where(item => item.CoversCalendarDate(localDate))
            .OrderBy(item => item.StartDate)
            .ThenBy(item => item.Id)
            .FirstOrDefault();
}

public sealed record AttendanceDayResolution(
    DateOnly LocalDate,
    AttendanceCoverage Coverage,
    AttendanceAcceptedKind? AcceptedKind,
    AttendanceSource? Source,
    bool IsProvisional,
    bool IsManual,
    bool IsUnresolved,
    string? CorrectionReason,
    LeaveRecord? RecordedLeave,
    ScheduleEntry? Schedule);
