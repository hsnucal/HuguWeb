using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HuGuWeb.Workforce.Domain;

public static class HrDocumentTemplateRenderer
{
    private static readonly Regex ScriptTagPattern = new(
        @"<script\b[^>]*>[\s\S]*?</script>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex IframeTagPattern = new(
        @"<iframe\b[^>]*>[\s\S]*?</iframe>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EventHandlerPattern = new(
        @"\s+on\w+\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex JavascriptUrlPattern = new(
        @"javascript\s*:",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryRender(
        string content,
        HrDocumentRenderContext context,
        CultureInfo? culture,
        out string rendered,
        out string? field,
        out string? errorCode)
    {
        rendered = string.Empty;
        field = HrValidation.Fields.DocumentTemplateContent;
        if (!HrDocumentPlaceholderCatalog.TryValidatePlaceholders(content, out var unknown))
        {
            errorCode = HrValidation.Codes.DocumentTemplateUnknownPlaceholder;
            field = HrValidation.Fields.DocumentTemplatePlaceholder;
            rendered = unknown ?? string.Empty;
            return false;
        }

        var formatCulture = culture ?? CultureInfo.GetCultureInfo("tr-TR");
        const string dateFormat = "dd.MM.yyyy";
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [HrDocumentPlaceholderCatalog.EmployeeFullName] = context.EmployeeFullName,
            [HrDocumentPlaceholderCatalog.EmployeeGivenName] = context.EmployeeGivenName,
            [HrDocumentPlaceholderCatalog.EmployeeFamilyName] = context.EmployeeFamilyName,
            [HrDocumentPlaceholderCatalog.EmployeePersonnelNumber] = context.EmployeePersonnelNumber,
            [HrDocumentPlaceholderCatalog.EmployeeBirthDate] = FormatDate(context.EmployeeBirthDate, dateFormat, formatCulture),
            [HrDocumentPlaceholderCatalog.EmploymentStartDate] = FormatDate(context.EmploymentStartDate, dateFormat, formatCulture),
            [HrDocumentPlaceholderCatalog.AssignmentDepartmentName] = context.AssignmentDepartmentName,
            [HrDocumentPlaceholderCatalog.AssignmentPositionName] = context.AssignmentPositionName,
            [HrDocumentPlaceholderCatalog.OrganizationName] = context.OrganizationName,
            [HrDocumentPlaceholderCatalog.PropertyName] = context.PropertyName,
            [HrDocumentPlaceholderCatalog.CurrentDate] = FormatDate(context.CurrentDate, dateFormat, formatCulture)
        };

        var builder = new StringBuilder(content ?? string.Empty);
        foreach (var (placeholder, value) in map)
        {
            builder.Replace(placeholder, value);
        }

        var output = builder.ToString();
        output = SanitizeHtml(output);
        if (!LooksLikeHtml(content))
        {
            output = output.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\n", "<br/>", StringComparison.Ordinal);
        }

        rendered = output;
        field = null;
        errorCode = null;
        return true;
    }

    private static string FormatDate(DateOnly? value, string format, CultureInfo culture) =>
        value is { } date ? date.ToString(format, culture) : string.Empty;

    private static bool LooksLikeHtml(string? content) =>
        !string.IsNullOrWhiteSpace(content)
        && content.Contains('<', StringComparison.Ordinal)
        && content.Contains('>', StringComparison.Ordinal);

    private static string SanitizeHtml(string content)
    {
        var sanitized = ScriptTagPattern.Replace(content, string.Empty);
        sanitized = IframeTagPattern.Replace(sanitized, string.Empty);
        sanitized = EventHandlerPattern.Replace(sanitized, string.Empty);
        sanitized = JavascriptUrlPattern.Replace(sanitized, string.Empty);
        return sanitized;
    }
}
