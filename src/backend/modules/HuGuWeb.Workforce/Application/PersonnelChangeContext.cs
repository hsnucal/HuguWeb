namespace HuGuWeb.Workforce.Application;

public sealed record PersonnelChangeContext(
    string UserId,
    Guid? EmployeeId,
    Guid OrganizationId,
    Guid? PropertyId,
    DateTimeOffset OccurredAtUtc,
    string? ChangeSource = null);

public static class PersonnelChangeSources
{
    public const string Manual = "Manual";
    public const string ExcelImport = "ExcelImport";
}
