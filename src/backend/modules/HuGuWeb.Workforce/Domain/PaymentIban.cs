namespace HuGuWeb.Workforce.Domain;

public static class PaymentIban
{
    public static bool TryNormalize(string? input, out string normalized, out string? error)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            error = "IBAN is required.";
            return false;
        }

        var compact = new string(input.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToUpperInvariant();
        if (compact.Length < 15 || compact.Length > EmployeePaymentProfile.IbanMaxLength)
        {
            error = "IBAN length is invalid.";
            return false;
        }

        if (!compact.All(ch => char.IsAsciiLetterOrDigit(ch)))
        {
            error = "IBAN contains invalid characters.";
            return false;
        }

        if (!IsValidChecksum(compact))
        {
            error = "IBAN checksum is invalid.";
            return false;
        }

        normalized = compact;
        error = null;
        return true;
    }

    private static bool IsValidChecksum(string iban)
    {
        var rearranged = iban[4..] + iban[..4];
        var remainder = 0;
        foreach (var ch in rearranged)
        {
            var expanded = char.IsAsciiDigit(ch)
                ? ch.ToString()
                : (ch - 'A' + 10).ToString();
            foreach (var digit in expanded)
            {
                remainder = (remainder * 10 + (digit - '0')) % 97;
            }
        }

        return remainder == 1;
    }
}
