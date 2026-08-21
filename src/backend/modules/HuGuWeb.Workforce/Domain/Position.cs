namespace HuGuWeb.Workforce.Domain;

public sealed class Position
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 32;

    private Position()
    {
        Name = string.Empty;
    }

    private Position(Guid id, Guid propertyId, string name, string? code, bool isActive)
    {
        Id = id;
        PropertyId = propertyId;
        Name = name;
        Code = code;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; }
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }

    public static bool TryCreate(
        Guid id,
        Guid propertyId,
        string? name,
        string? code,
        out Position? position,
        out string? error)
    {
        position = null;
        if (!TryNormalizeName(name, out var normalizedName, out error))
        {
            return false;
        }

        if (!TryNormalizeCode(code, out var normalizedCode, out error))
        {
            return false;
        }

        position = new Position(id, propertyId, normalizedName, normalizedCode, isActive: true);
        return true;
    }

    public bool TryRename(string? name, out string? error) =>
        TryNormalizeName(name, out var normalized, out error) && ApplyName(normalized);

    public bool TryChangeCode(string? code, out string? error)
    {
        if (!TryNormalizeCode(code, out var normalized, out error))
        {
            return false;
        }

        Code = normalized;
        return true;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private bool ApplyName(string name)
    {
        Name = name;
        return true;
    }

    public static bool TryNormalizeName(string? name, out string normalized, out string? error)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Position name is required.";
            return false;
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            error = $"Position name must be {NameMaxLength} characters or fewer.";
            return false;
        }

        normalized = trimmed;
        error = null;
        return true;
    }

    public static bool TryNormalizeCode(string? code, out string? normalized, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(code))
        {
            normalized = null;
            return true;
        }

        var trimmed = code.Trim();
        if (trimmed.Length > CodeMaxLength)
        {
            normalized = null;
            error = $"Position code must be {CodeMaxLength} characters or fewer.";
            return false;
        }

        normalized = trimmed;
        return true;
    }
}
