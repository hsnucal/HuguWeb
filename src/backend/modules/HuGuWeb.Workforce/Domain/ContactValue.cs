using System.Net.Mail;
using System.Text;

namespace HuGuWeb.Workforce.Domain;

public static class ContactValue
{
    public const int PhoneMaxLength = 32;
    public const int EmailMaxLength = 254;
    public const int AddressMaxLength = 500;
    public const int PlaceMaxLength = 100;
    public const int NationalityMaxLength = 64;
    public const int NotesMaxLength = 2000;

    public static bool TryNormalizePhone(string? value, bool required, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                error = HrValidation.Codes.PhoneRequired;
                return false;
            }

            return true;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim())
        {
            if (char.IsDigit(character))
            {
                builder.Append(character);
            }
            else if (character == '+' && builder.Length == 0)
            {
                builder.Append(character);
            }
        }

        if (builder.Length is < 7 or > PhoneMaxLength)
        {
            error = HrValidation.Codes.PhoneInvalid;
            return false;
        }

        normalized = builder.ToString();
        return true;
    }

    public static bool TryNormalizeEmail(string? value, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > EmailMaxLength)
        {
            error = HrValidation.Codes.EmailTooLong;
            return false;
        }

        try
        {
            var address = new MailAddress(trimmed);
            normalized = address.Address.ToLowerInvariant();
            return true;
        }
        catch (FormatException)
        {
            error = HrValidation.Codes.EmailInvalid;
            return false;
        }
    }

    public static bool TryNormalizeOptionalText(
        string? value,
        int maxLength,
        out string? normalized,
        out string? error)
    {
        normalized = null;
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            error = HrValidation.Codes.TextTooLong;
            return false;
        }

        normalized = trimmed;
        return true;
    }

    public static bool TryNormalizeBirthDate(DateOnly? value, DateOnly today, out DateOnly? normalized, out string? error)
    {
        normalized = value;
        error = null;
        if (value is null)
        {
            return true;
        }

        var earliest = today.AddYears(-120);
        if (value.Value > today || value.Value < earliest)
        {
            error = HrValidation.Codes.BirthDateInvalid;
            return false;
        }

        return true;
    }
}
