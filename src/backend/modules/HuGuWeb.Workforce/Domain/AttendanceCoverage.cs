namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Presentation/security coverage for a calendar date. Never persisted.
/// NotEmployed is not Unresolved and must not count as absence.
/// </summary>
public enum AttendanceCoverage
{
    InEmployment = 1,
    NotEmployed = 2,
    OutOfScope = 3
}
