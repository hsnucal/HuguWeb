namespace HuGuWeb.Workforce.Application;

public static class SpreadsheetSafety
{
    private static readonly char[] FormulaPrefixes = ['=', '+', '-', '@', '\t', '\r'];

    public static string SanitizeCellValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var trimmed = value.TrimStart();
        if (trimmed.Length == 0)
        {
            return value;
        }

        if (FormulaPrefixes.Contains(trimmed[0]))
        {
            return "'" + value;
        }

        return value;
    }

    public static string SanitizeImportedCell(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 0 && FormulaPrefixes.Contains(trimmed[0]))
        {
            trimmed = trimmed[1..].TrimStart();
        }

        return trimmed;
    }
}

public static class PersonnelImportLimits
{
    public const int MaxRows = 5000;
    public const long MaxWorkbookBytes = 5 * 1024 * 1024;
}
