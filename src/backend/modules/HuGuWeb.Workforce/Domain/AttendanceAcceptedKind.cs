namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Resolved Puantaj result for a Property-local date. Not a persisted AttendanceDay row.
/// Worked from Schedule and Worked from Manual share this kind; provenance distinguishes them.
/// </summary>
public enum AttendanceAcceptedKind
{
    Unresolved = 0,
    Worked = 1,
    Leave = 2,
    RestDay = 3,
    Absent = 4
}
