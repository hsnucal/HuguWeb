namespace HuGuWeb.Workforce.Domain;

public sealed class SgkDocumentType
{
    public const int CodeMaxLength = 8;
    public const int DescriptionMaxLength = 200;

    private SgkDocumentType()
    {
        Code = string.Empty;
        Description = string.Empty;
    }

    public SgkDocumentType(string code, string description, bool isActive = true)
    {
        Code = code;
        Description = description;
        IsActive = isActive;
    }

    public string Code { get; private set; }
    public string Description { get; private set; }
    public bool IsActive { get; private set; }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
