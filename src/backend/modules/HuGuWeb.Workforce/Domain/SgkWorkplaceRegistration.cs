namespace HuGuWeb.Workforce.Domain;

public sealed class SgkWorkplaceRegistration
{
    public const int RegistrationNumberMaxLength = 32;
    public const int DisplayNameMaxLength = 100;

    private SgkWorkplaceRegistration()
    {
        RegistrationNumber = string.Empty;
    }

    private SgkWorkplaceRegistration(
        Guid id,
        Guid propertyId,
        string registrationNumber,
        string? displayName,
        bool isActive,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        PropertyId = propertyId;
        RegistrationNumber = registrationNumber;
        DisplayName = displayName;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string RegistrationNumber { get; private set; }
    public string? DisplayName { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static bool TryCreate(
        Guid id,
        Guid propertyId,
        string? registrationNumber,
        string? displayName,
        DateTimeOffset createdAtUtc,
        out SgkWorkplaceRegistration? registration,
        out string? field,
        out string? code)
    {
        registration = null;
        if (!TryNormalizeRegistrationNumber(registrationNumber, out var number, out field, out code))
        {
            return false;
        }

        if (!TryNormalizeDisplayName(displayName, out var name, out field, out code))
        {
            return false;
        }

        registration = new SgkWorkplaceRegistration(id, propertyId, number, name, isActive: true, createdAtUtc);
        field = null;
        code = null;
        return true;
    }

    public bool TryUpdate(
        string? registrationNumber,
        string? displayName,
        out string? field,
        out string? code)
    {
        if (!TryNormalizeRegistrationNumber(registrationNumber, out var number, out field, out code))
        {
            return false;
        }

        if (!TryNormalizeDisplayName(displayName, out var name, out field, out code))
        {
            return false;
        }

        RegistrationNumber = number;
        DisplayName = name;
        field = null;
        code = null;
        return true;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public static bool TryNormalizeRegistrationNumber(
        string? value,
        out string normalized,
        out string? field,
        out string? code)
    {
        normalized = string.Empty;
        field = HrValidation.Fields.RegistrationNumber;
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            code = HrValidation.Codes.RegistrationNumberRequired;
            return false;
        }

        var compact = string.Concat(trimmed.Where(character => !char.IsWhiteSpace(character)));
        if (compact.Length > RegistrationNumberMaxLength)
        {
            code = HrValidation.Codes.RegistrationNumberTooLong;
            return false;
        }

        normalized = compact;
        field = null;
        code = null;
        return true;
    }

    public static bool TryNormalizeDisplayName(
        string? value,
        out string? normalized,
        out string? field,
        out string? code)
    {
        field = HrValidation.Fields.DisplayName;
        if (string.IsNullOrWhiteSpace(value))
        {
            normalized = null;
            field = null;
            code = null;
            return true;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > DisplayNameMaxLength)
        {
            normalized = null;
            code = HrValidation.Codes.DisplayNameTooLong;
            return false;
        }

        normalized = trimmed;
        field = null;
        code = null;
        return true;
    }

    public static string MaskRegistrationNumber(string registrationNumber)
    {
        if (string.IsNullOrEmpty(registrationNumber) || registrationNumber.Length <= 4)
        {
            return registrationNumber;
        }

        return new string('•', registrationNumber.Length - 4) + registrationNumber[^4..];
    }

    public string FormatPickerLabel(bool maskRegistration)
    {
        var number = maskRegistration ? MaskRegistrationNumber(RegistrationNumber) : RegistrationNumber;
        return string.IsNullOrWhiteSpace(DisplayName) ? number : $"{DisplayName} — {number}";
    }
}
