namespace HuGuWeb.Workforce.Domain;

public static class PaymentIban
{
    public const string TurkishCountryPrefix = "TR";
    public const int TurkishIbanBodyLength = 24;
    public const int TurkishIbanMaxLength = 26;

    public static bool TryNormalize(string? input, out string normalized, out string? error)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            error = "IBAN is required.";
            return false;
        }

        var digits = ExtractTurkishIbanDigits(input, maxDigits: null);
        if (digits.Length != TurkishIbanBodyLength)
        {
            error = "IBAN must be a Turkish IBAN with exactly 24 digits after TR.";
            return false;
        }

        normalized = TurkishCountryPrefix + digits;
        error = null;
        return true;
    }

    /// <summary>
    /// Digits after the fixed TR prefix (capped at 24 for UI helpers).
    /// Strips spaces, punctuation, letters, and leading TR prefixes so paste does not produce TRTR.
    /// </summary>
    public static string NormalizeTurkishIbanDigits(string input) =>
        ExtractTurkishIbanDigits(input, TurkishIbanBodyLength);

    public static string ToCanonical(string input)
    {
        var digits = NormalizeTurkishIbanDigits(input);
        return digits.Length == 0 ? string.Empty : TurkishCountryPrefix + digits;
    }

    private static string ExtractTurkishIbanDigits(string input, int? maxDigits)
    {
        var compact = new string(
            input
                .Where(ch => !char.IsWhiteSpace(ch))
                .Select(char.ToUpperInvariant)
                .Where(ch => char.IsAsciiLetterOrDigit(ch))
                .ToArray());

        while (compact.StartsWith(TurkishCountryPrefix, StringComparison.Ordinal))
        {
            compact = compact[TurkishCountryPrefix.Length..];
        }

        var digitQuery = compact.Where(char.IsAsciiDigit);
        if (maxDigits is int limit)
        {
            digitQuery = digitQuery.Take(limit);
        }

        return new string(digitQuery.ToArray());
    }
}
