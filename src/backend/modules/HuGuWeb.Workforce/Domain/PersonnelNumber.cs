namespace HuGuWeb.Workforce.Domain;

public readonly record struct PersonnelNumber
{
    public const int MaxLength = 32;

    public string Value { get; }

    private PersonnelNumber(string value) => Value = value;

    public static bool TryCreate(string? value, out PersonnelNumber personnelNumber, out string? error)
    {
        personnelNumber = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Personnel number is required.";
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            error = $"Personnel number must be {PersonnelNumber.MaxLength} characters or fewer.";
            return false;
        }

        personnelNumber = new PersonnelNumber(trimmed);
        error = null;
        return true;
    }

    public override string ToString() => Value;
}
