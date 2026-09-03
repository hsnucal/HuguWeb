namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Persisted manual Puantaj override. Unresolved is derived only and must never be stored.
/// Correction Leave is an accepted attendance override, not a new LeaveRecord / entitlement.
/// </summary>
public enum AttendanceCorrectionKind
{
    Worked = 1,
    Leave = 2,
    RestDay = 3,
    Absent = 4
}
