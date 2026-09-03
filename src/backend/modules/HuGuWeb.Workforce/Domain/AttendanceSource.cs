namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Provenance of an accepted Puantaj result. Punch is reserved for a future PDKS slice.
/// </summary>
public enum AttendanceSource
{
    Schedule = 1,
    Leave = 2,
    Manual = 3,
    Punch = 4
}
