namespace HuGuWeb.Workforce.Domain;

public sealed class PersonnelProfileChange
{
    public const int FieldCodeMaxLength = 64;
    public const int ValueMaxLength = 500;
    public const int SourceMaxLength = 32;
    public const int UserIdMaxLength = 450;

    private PersonnelProfileChange()
    {
        FieldCode = string.Empty;
        ChangedByUserId = string.Empty;
    }

    private PersonnelProfileChange(
        Guid id,
        Guid employeeId,
        Guid organizationId,
        Guid? propertyId,
        string fieldCode,
        string? oldValue,
        string? newValue,
        DateTimeOffset changedAtUtc,
        string changedByUserId,
        Guid? changedByEmployeeId,
        string? changeSource)
    {
        Id = id;
        EmployeeId = employeeId;
        OrganizationId = organizationId;
        PropertyId = propertyId;
        FieldCode = fieldCode;
        OldValue = oldValue;
        NewValue = newValue;
        ChangedAtUtc = changedAtUtc;
        ChangedByUserId = changedByUserId;
        ChangedByEmployeeId = changedByEmployeeId;
        ChangeSource = changeSource;
    }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? PropertyId { get; private set; }
    public string FieldCode { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }
    public string ChangedByUserId { get; private set; }
    public Guid? ChangedByEmployeeId { get; private set; }
    public string? ChangeSource { get; private set; }

    public static PersonnelProfileChange Record(
        Guid id,
        Guid employeeId,
        Guid organizationId,
        Guid? propertyId,
        string fieldCode,
        string? oldValue,
        string? newValue,
        DateTimeOffset changedAtUtc,
        string changedByUserId,
        Guid? changedByEmployeeId,
        string? changeSource = null) =>
        new(
            id,
            employeeId,
            organizationId,
            propertyId,
            fieldCode,
            Truncate(oldValue),
            Truncate(newValue),
            changedAtUtc,
            changedByUserId,
            changedByEmployeeId,
            changeSource);

    private static string? Truncate(string? value) =>
        value is null ? null : value.Length <= ValueMaxLength ? value : value[..ValueMaxLength];
}
