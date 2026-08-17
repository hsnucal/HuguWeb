namespace HuGuWeb.Api.Identity;

public static class SupportedLanguages
{
    public const string Turkish = "tr";
    public const string English = "en";
    public const string Russian = "ru";
    public const string Default = Turkish;

    public static readonly IReadOnlyList<string> All = [Turkish, English, Russian];

    public static bool IsSupported(string? language) => TryNormalize(language, out _);

    public static bool TryNormalize(string? language, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            normalized = Default;
            return false;
        }

        var candidate = language.Trim().ToLowerInvariant();
        if (All.Contains(candidate, StringComparer.Ordinal))
        {
            normalized = candidate;
            return true;
        }

        normalized = Default;
        return false;
    }
}
