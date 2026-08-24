namespace HuGuWeb.Workforce.Domain;

public sealed class EmploymentDutyCode
{
    public const int CodeMaxLength = 32;
    public const int DescriptionMaxLength = 120;

    private EmploymentDutyCode()
    {
        Code = string.Empty;
        Description = string.Empty;
    }

    public EmploymentDutyCode(string code, string description, bool isActive = true)
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
