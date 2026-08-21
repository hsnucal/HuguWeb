namespace HuGuWeb.Workforce.Domain;

public sealed class Employee
{
    public const int NameMaxLength = 100;

    private Employee()
    {
        GivenName = string.Empty;
        FamilyName = string.Empty;
        PersonnelNumber = string.Empty;
    }

    private Employee(
        Guid id,
        Guid organizationId,
        string givenName,
        string familyName,
        string personnelNumber)
    {
        Id = id;
        OrganizationId = organizationId;
        GivenName = givenName;
        FamilyName = familyName;
        PersonnelNumber = personnelNumber;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string GivenName { get; private set; }
    public string FamilyName { get; private set; }
    public string PersonnelNumber { get; private set; }

    public static bool TryCreate(
        Guid id,
        Guid organizationId,
        string? givenName,
        string? familyName,
        string? personnelNumber,
        out Employee? employee,
        out string? error)
    {
        employee = null;
        if (!TryNormalizePersonName(givenName, "Given name", out var given, out error))
        {
            return false;
        }

        if (!TryNormalizePersonName(familyName, "Family name", out var family, out error))
        {
            return false;
        }

        if (!Domain.PersonnelNumber.TryCreate(personnelNumber, out var number, out error))
        {
            return false;
        }

        employee = new Employee(id, organizationId, given, family, number.Value);
        return true;
    }

    public static bool TryNormalizePersonName(
        string? value,
        string fieldLabel,
        out string normalized,
        out string? error)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = $"{fieldLabel} is required.";
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            error = $"{fieldLabel} must be {NameMaxLength} characters or fewer.";
            return false;
        }

        normalized = trimmed;
        error = null;
        return true;
    }
}
