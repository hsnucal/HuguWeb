namespace HuGuWeb.Workforce.Domain;

public sealed class InsuranceBranch
{
    public const int CodeMaxLength = 8;
    public const int DescriptionMaxLength = 200;

    private InsuranceBranch()
    {
        Code = string.Empty;
        Description = string.Empty;
    }

    public InsuranceBranch(string code, string description, bool isActive = true)
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
