namespace HuGuWeb.Workforce.Domain;

public sealed class SgkOccupationCode
{
    public const int CodeMaxLength = 8;
    public const int DescriptionMaxLength = 200;
    public const int SourceMaxLength = 64;
    public const int CatalogueVersionMaxLength = 64;
    public const string CodePattern = @"^\d{4}\.\d{2}$";

    private SgkOccupationCode()
    {
        Code = string.Empty;
        Description = string.Empty;
    }

    public SgkOccupationCode(
        string code,
        string description,
        bool isActive = true,
        string? source = null,
        string? catalogueVersion = null)
    {
        Code = code;
        Description = description;
        IsActive = isActive;
        Source = source;
        CatalogueVersion = catalogueVersion;
    }

    public string Code { get; private set; }
    public string Description { get; private set; }
    public bool IsActive { get; private set; }
    public string? Source { get; private set; }
    public string? CatalogueVersion { get; private set; }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public bool SyncFromCatalogue(string description, bool isActive, string? source, string? catalogueVersion)
    {
        var changed = Description != description
            || IsActive != isActive
            || Source != source
            || CatalogueVersion != catalogueVersion;
        if (!changed)
        {
            return false;
        }

        Description = description;
        IsActive = isActive;
        Source = source;
        CatalogueVersion = catalogueVersion;
        return true;
    }

    public static bool IsValidFormat(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 7)
        {
            return false;
        }

        return char.IsDigit(code[0])
            && char.IsDigit(code[1])
            && char.IsDigit(code[2])
            && char.IsDigit(code[3])
            && code[4] == '.'
            && char.IsDigit(code[5])
            && char.IsDigit(code[6]);
    }
}
