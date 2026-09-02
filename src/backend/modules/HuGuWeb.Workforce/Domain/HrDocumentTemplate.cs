using System.Globalization;

namespace HuGuWeb.Workforce.Domain;

public enum HrDocumentTemplateCategory
{
    Onboarding = 1
}

/// <summary>Organization-owned printable HR document template with allow-listed placeholders.</summary>
public sealed class HrDocumentTemplate
{
    public const int CodeMaxLength = 32;
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 500;
    public const int VersionMaxLength = 32;

    private HrDocumentTemplate()
    {
        Code = string.Empty;
        Name = string.Empty;
        Content = string.Empty;
        Version = string.Empty;
        Category = HrDocumentTemplateCategory.Onboarding;
    }

    private HrDocumentTemplate(
        Guid id,
        Guid organizationId,
        string code,
        string name,
        string? description,
        HrDocumentTemplateCategory category,
        string content,
        string? templateAssetPath,
        string version,
        int sortOrder,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Code = code;
        Name = name;
        Description = description;
        Category = category;
        Content = content;
        TemplateAssetPath = templateAssetPath;
        IsActive = true;
        Version = version;
        SortOrder = sortOrder;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public HrDocumentTemplateCategory Category { get; private set; }
    public string Content { get; private set; }
    /// <summary>Application-owned relative asset id (never a client filesystem path).</summary>
    public string? TemplateAssetPath { get; private set; }
    public bool IsActive { get; private set; }
    public string Version { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static HrDocumentTemplate CreateSystemDefault(
        Guid id,
        Guid organizationId,
        string code,
        string name,
        string? description,
        HrDocumentTemplateCategory category,
        string content,
        string version,
        int sortOrder,
        DateTimeOffset createdAtUtc,
        string? templateAssetPath = null)
    {
        if (!TryNormalizeCode(code, out var normalizedCode, out _, out _))
        {
            throw new ArgumentException($"System HR document template code '{code}' is invalid.", nameof(code));
        }

        if (!TryNormalizeName(name, out var normalizedName, out _, out _))
        {
            throw new ArgumentException($"System HR document template name '{name}' is invalid.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(version) || version.Trim().Length > VersionMaxLength)
        {
            throw new ArgumentException($"System HR document template version '{version}' is invalid.", nameof(version));
        }

        string? normalizedDescription = null;
        if (!string.IsNullOrWhiteSpace(description))
        {
            normalizedDescription = description.Trim();
            if (normalizedDescription.Length > DescriptionMaxLength)
            {
                throw new ArgumentException(
                    $"System HR document template description is too long.",
                    nameof(description));
            }
        }

        return new HrDocumentTemplate(
            id,
            organizationId,
            normalizedCode,
            normalizedName,
            normalizedDescription,
            category,
            content ?? string.Empty,
            NormalizeAssetPath(templateAssetPath),
            version.Trim(),
            sortOrder,
            createdAtUtc);
    }

    public static string? NormalizeAssetPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var normalized = path.Trim().Replace('\\', '/');
        if (normalized.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(normalized)
            || normalized.StartsWith('/')
            || normalized.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException("Template asset path is not an application-owned relative path.", nameof(path));
        }

        return normalized;
    }

    public static bool TryNormalizeCode(string? code, out string normalized, out string? field, out string? errorCode)
    {
        normalized = string.Empty;
        field = HrValidation.Fields.DocumentTemplateCode;
        if (string.IsNullOrWhiteSpace(code))
        {
            errorCode = HrValidation.Codes.DocumentTemplateCodeRequired;
            return false;
        }

        var trimmed = code.Trim().ToUpperInvariant();
        if (trimmed.Length > CodeMaxLength)
        {
            errorCode = HrValidation.Codes.DocumentTemplateCodeTooLong;
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
        field = HrValidation.Fields.DocumentTemplateName;
        if (string.IsNullOrWhiteSpace(name))
        {
            errorCode = HrValidation.Codes.DocumentTemplateNameRequired;
            return false;
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            errorCode = HrValidation.Codes.DocumentTemplateNameTooLong;
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
