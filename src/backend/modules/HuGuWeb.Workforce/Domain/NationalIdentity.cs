using System.Text;

namespace HuGuWeb.Workforce.Domain;

public static class NationalIdentity
{
    public const int NumberMaxLength = 32;

    public static bool TryNormalize(
        NationalIdentityScheme? scheme,
        string? number,
        out NationalIdentityScheme? normalizedScheme,
        out string? displayNumber,
        out string? normalizedNumber,
        out string? error)
    {
        normalizedScheme = null;
        displayNumber = null;
        normalizedNumber = null;
        error = null;

        var trimmed = string.IsNullOrWhiteSpace(number) ? null : number.Trim();
        if (scheme is null && trimmed is null)
        {
            return true;
        }

        if (trimmed is null)
        {
            normalizedScheme = null;
            return true;
        }

        if (scheme is null)
        {
            error = HrValidation.Codes.IdentitySchemeRequired;
            return false;
        }

        if (trimmed.Length > NumberMaxLength)
        {
            error = HrValidation.Codes.IdentityTooLong;
            return false;
        }

        var normalized = scheme.Value switch
        {
            NationalIdentityScheme.Tckn or NationalIdentityScheme.Ykn => DigitsOnly(trimmed),
            NationalIdentityScheme.Passport => PassportNormalize(trimmed),
            NationalIdentityScheme.Other => OtherNormalize(trimmed),
            _ => null
        };

        if (string.IsNullOrEmpty(normalized))
        {
            error = scheme.Value switch
            {
                NationalIdentityScheme.Tckn => HrValidation.Codes.TcknLength,
                NationalIdentityScheme.Ykn => HrValidation.Codes.YknFormat,
                NationalIdentityScheme.Passport => HrValidation.Codes.PassportFormat,
                _ => HrValidation.Codes.IdentityInvalid
            };
            return false;
        }

        if (scheme.Value == NationalIdentityScheme.Tckn && normalized.Length != 11)
        {
            error = HrValidation.Codes.TcknLength;
            return false;
        }

        if (!IsFormatValid(scheme.Value, normalized))
        {
            error = scheme.Value switch
            {
                NationalIdentityScheme.Tckn => HrValidation.Codes.TcknInvalid,
                NationalIdentityScheme.Ykn => HrValidation.Codes.YknFormat,
                NationalIdentityScheme.Passport => HrValidation.Codes.PassportFormat,
                _ => HrValidation.Codes.IdentityInvalid
            };
            return false;
        }

        normalizedScheme = scheme;
        displayNumber = trimmed;
        normalizedNumber = normalized;
        return true;
    }

    public static bool IsFormatValid(NationalIdentityScheme scheme, string normalized) =>
        scheme switch
        {
            NationalIdentityScheme.Tckn => IsValidTcknFormat(normalized),
            NationalIdentityScheme.Ykn => IsValidYknFormat(normalized),
            NationalIdentityScheme.Passport => normalized.Length is >= 5 and <= 15
                && normalized.All(char.IsLetterOrDigit),
            NationalIdentityScheme.Other => normalized.Length is >= 1 and <= NumberMaxLength,
            _ => false
        };

    private static bool IsValidTcknFormat(string digits)
    {
        if (digits.Length != 11 || digits[0] == '0' || !digits.All(char.IsDigit))
        {
            return false;
        }

        var values = digits.Select(character => character - '0').ToArray();
        var odd = values[0] + values[2] + values[4] + values[6] + values[8];
        var even = values[1] + values[3] + values[5] + values[7];
        var tenth = ((odd * 7) - even) % 10;
        if (tenth < 0)
        {
            tenth += 10;
        }

        if (values[9] != tenth)
        {
            return false;
        }

        return values[10] == values.Take(10).Sum() % 10;
    }

    private static bool IsValidYknFormat(string digits) =>
        digits.Length == 11 && digits[0] == '9' && digits.All(char.IsDigit);

    private static string DigitsOnly(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static string PassportNormalize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static string OtherNormalize(string value) => value.Trim().ToUpperInvariant();
}
