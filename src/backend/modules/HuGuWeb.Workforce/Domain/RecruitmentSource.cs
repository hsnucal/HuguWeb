using System.Globalization;

namespace HuGuWeb.Workforce.Domain;

/// <summary>Organization-owned catalog of how candidates are sourced.</summary>
public sealed class RecruitmentSource
{
    public const int CodeMaxLength = 32;
    public const int NameMaxLength = 200;

    private RecruitmentSource()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private RecruitmentSource(
        Guid id,
        Guid organizationId,
        string code,
        string name,
        int sortOrder,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Code = code;
        Name = name;
        IsActive = true;
        SortOrder = sortOrder;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static RecruitmentSource CreateSystemDefault(
        Guid id,
        Guid organizationId,
        string code,
        string name,
        int sortOrder,
        DateTimeOffset createdAtUtc)
    {
        if (!TryNormalizeCode(code, out var normalizedCode, out _, out _))
        {
            throw new ArgumentException($"System recruitment source code '{code}' is invalid.", nameof(code));
        }

        if (!TryNormalizeName(name, out var normalizedName, out _, out _))
        {
            throw new ArgumentException($"System recruitment source name '{name}' is invalid.", nameof(name));
        }

        return new RecruitmentSource(
            id,
            organizationId,
            normalizedCode,
            normalizedName,
            sortOrder,
            createdAtUtc);
    }

    public static bool TryNormalizeCode(string? code, out string normalized, out string? field, out string? errorCode)
    {
        normalized = string.Empty;
        field = HrValidation.Fields.RecruitmentSourceCode;
        if (string.IsNullOrWhiteSpace(code))
        {
            errorCode = HrValidation.Codes.RecruitmentSourceCodeRequired;
            return false;
        }

        var trimmed = code.Trim().ToUpperInvariant();
        if (trimmed.Length > CodeMaxLength)
        {
            errorCode = HrValidation.Codes.RecruitmentSourceCodeTooLong;
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
        field = HrValidation.Fields.RecruitmentSourceName;
        if (string.IsNullOrWhiteSpace(name))
        {
            errorCode = HrValidation.Codes.RecruitmentSourceNameRequired;
            return false;
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            errorCode = HrValidation.Codes.RecruitmentSourceNameTooLong;
            return false;
        }

        normalized = trimmed;
        field = null;
        errorCode = null;
        return true;
    }

    public static string NormalizeCodeForLookup(string code) =>
        code.Trim().ToUpper(CultureInfo.InvariantCulture);

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        IsActive = false;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Activate(DateTimeOffset updatedAtUtc)
    {
        IsActive = true;
        UpdatedAtUtc = updatedAtUtc;
    }
}
