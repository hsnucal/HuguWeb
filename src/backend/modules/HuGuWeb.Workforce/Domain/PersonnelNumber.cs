using System.Globalization;

namespace HuGuWeb.Workforce.Domain;

public readonly record struct PersonnelNumber
{
    public const int MaxLength = 32;

    public static string Format(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public string Value { get; }

    private PersonnelNumber(string value) => Value = value;

    public static bool TryCreate(string? value, out PersonnelNumber personnelNumber, out string? error)
    {
        personnelNumber = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = HrValidation.Codes.PersonnelNumberRequired;
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            error = HrValidation.Codes.PersonnelNumberTooLong;
            return false;
        }

        personnelNumber = new PersonnelNumber(trimmed);
        error = null;
        return true;
    }

    public override string ToString() => Value;
}
