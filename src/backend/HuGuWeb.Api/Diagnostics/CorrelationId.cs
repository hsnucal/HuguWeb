namespace HuGuWeb.Api.Diagnostics;

public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";
    public const int MaxLength = 128;

    public static string Resolve(string? incoming, string fallback)
    {
        if (IsSafe(incoming))
        {
            return incoming!;
        }

        return string.IsNullOrWhiteSpace(fallback)
            ? Guid.NewGuid().ToString("N")
            : fallback;
    }

    public static bool IsSafe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_' and not '.')
            {
                return false;
            }
        }

        return true;
    }
}
