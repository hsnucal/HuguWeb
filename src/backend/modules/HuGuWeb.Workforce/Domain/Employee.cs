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
        if (!TryNormalizePersonName(givenName, "given-name", out var given, out error))
        {
            return false;
        }

        if (!TryNormalizePersonName(familyName, "family-name", out var family, out error))
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

    public bool TryRename(string? givenName, string? familyName, out string? error)
    {
        if (!TryNormalizePersonName(givenName, "given-name", out var given, out error))
        {
            return false;
        }

        if (!TryNormalizePersonName(familyName, "family-name", out var family, out error))
        {
            return false;
        }

        GivenName = given;
        FamilyName = family;
        return true;
    }

    public bool TryChangePersonnelNumber(string? personnelNumber, out string? error)
    {
        if (!Domain.PersonnelNumber.TryCreate(personnelNumber, out var number, out error))
        {
            return false;
        }

        PersonnelNumber = number.Value;
        return true;
    }

    public static bool TryNormalizePersonName(
        string? value,
        string codePrefix,
        out string normalized,
        out string? error)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = $"{codePrefix}-required";
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            error = $"{codePrefix}-too-long";
            return false;
        }

        normalized = trimmed;
        error = null;
        return true;
    }
}
