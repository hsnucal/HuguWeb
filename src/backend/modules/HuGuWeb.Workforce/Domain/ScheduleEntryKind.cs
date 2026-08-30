namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Authoritative schedule presence kinds. Unscheduled is absence of a row — never an enum member.
/// </summary>
public enum ScheduleEntryKind
{
    Shift = 1,
    RestDay = 2
}
