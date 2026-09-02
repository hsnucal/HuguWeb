using System.Globalization;

namespace HuGuWeb.Workforce.Domain;

/// <summary>Organization-owned onboarding document checklist item definition.</summary>
public sealed class OnboardingDocumentRequirement
{
    public const int CodeMaxLength = 32;
    public const int NameMaxLength = 200;

    private OnboardingDocumentRequirement()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private OnboardingDocumentRequirement(
        Guid id,
        Guid organizationId,
        string code,
        string name,
        int sortOrder,
        bool isRequiredByDefault,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Code = code;
        Name = name;
        IsActive = true;
        SortOrder = sortOrder;
        IsRequiredByDefault = isRequiredByDefault;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsRequiredByDefault { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static OnboardingDocumentRequirement CreateSystemDefault(
        Guid id,
        Guid organizationId,
        string code,
        string name,
        int sortOrder,
        bool isRequiredByDefault,
        DateTimeOffset createdAtUtc)
    {
        if (!TryNormalizeCode(code, out var normalizedCode, out _, out _))
        {
            throw new ArgumentException($"System onboarding requirement code '{code}' is invalid.", nameof(code));
        }

        if (!TryNormalizeName(name, out var normalizedName, out _, out _))
        {
            throw new ArgumentException($"System onboarding requirement name '{name}' is invalid.", nameof(name));
        }

        return new OnboardingDocumentRequirement(
            id,
            organizationId,
            normalizedCode,
            normalizedName,
            sortOrder,
            isRequiredByDefault,
            createdAtUtc);
    }

    public static bool TryNormalizeCode(string? code, out string normalized, out string? field, out string? errorCode)
    {
        normalized = string.Empty;
        field = HrValidation.Fields.OnboardingRequirementCode;
        if (string.IsNullOrWhiteSpace(code))
        {
            errorCode = HrValidation.Codes.OnboardingRequirementCodeRequired;
            return false;
        }

        var trimmed = code.Trim().ToUpperInvariant();
        if (trimmed.Length > CodeMaxLength)
        {
            errorCode = HrValidation.Codes.OnboardingRequirementCodeTooLong;
            return false;
        }

        normalized = trimmed;
        field = null;
        errorCode = null;
        return true;
    }

    public static bool TryNormalizeName(string? name, out string normalized, out string? field, out string? errorCode)
    {
        normalized = string.Empty;
        field = HrValidation.Fields.OnboardingRequirementName;
        if (string.IsNullOrWhiteSpace(name))
        {
            errorCode = HrValidation.Codes.OnboardingRequirementNameRequired;
            return false;
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            errorCode = HrValidation.Codes.OnboardingRequirementNameTooLong;
            return false;
        }

        normalized = trimmed;
        field = null;
        errorCode = null;
        return true;
    }

    public static string NormalizeCodeForLookup(string code) =>
        code.Trim().ToUpper(CultureInfo.InvariantCulture);
}
