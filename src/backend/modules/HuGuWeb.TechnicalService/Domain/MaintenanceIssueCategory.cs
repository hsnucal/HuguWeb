namespace HuGuWeb.TechnicalService.Domain;

public sealed class MaintenanceIssueCategory
{
    public const int NameMaxLength = 100;

    private MaintenanceIssueCategory()
    {
        Name = string.Empty;
    }

    private MaintenanceIssueCategory(Guid id, Guid propertyId, string name, bool isActive)
    {
        Id = id;
        PropertyId = propertyId;
        Name = name;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    public static bool TryCreate(
        Guid id,
        Guid propertyId,
        string? name,
        out MaintenanceIssueCategory? category,
        out string? error)
    {
        category = null;
        if (id == Guid.Empty || propertyId == Guid.Empty)
        {
            error = "Category identity is invalid.";
            return false;
        }

        if (!TryNormalizeName(name, out var normalized, out error))
        {
            return false;
        }

        category = new MaintenanceIssueCategory(id, propertyId, normalized, isActive: true);
        return true;
    }

    public static bool TryNormalizeName(string? name, out string normalized, out string? error)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Category name is required.";
            return false;
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            error = $"Category name must be {NameMaxLength} characters or fewer.";
            return false;
        }

        normalized = trimmed;
        error = null;
        return true;
    }
}
