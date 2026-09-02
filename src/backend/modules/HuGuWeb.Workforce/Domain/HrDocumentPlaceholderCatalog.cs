using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace HuGuWeb.Workforce.Domain;

public static class HrDocumentPlaceholderCatalog
{
    public const string EmployeeFullName = "{{Employee.FullName}}";
    public const string EmployeeGivenName = "{{Employee.GivenName}}";
    public const string EmployeeFamilyName = "{{Employee.FamilyName}}";
    public const string EmployeePersonnelNumber = "{{Employee.PersonnelNumber}}";
    public const string EmployeeBirthDate = "{{Employee.BirthDate}}";
    public const string EmploymentStartDate = "{{Employment.StartDate}}";
    public const string AssignmentDepartmentName = "{{Assignment.DepartmentName}}";
    public const string AssignmentPositionName = "{{Assignment.PositionName}}";
    public const string OrganizationName = "{{Organization.Name}}";
    public const string PropertyName = "{{Property.Name}}";
    public const string CurrentDate = "{{CurrentDate}}";

    public static readonly FrozenSet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        EmployeeFullName,
        EmployeeGivenName,
        EmployeeFamilyName,
        EmployeePersonnelNumber,
        EmployeeBirthDate,
        EmploymentStartDate,
        AssignmentDepartmentName,
        AssignmentPositionName,
        OrganizationName,
        PropertyName,
        CurrentDate
    }.ToFrozenSet(StringComparer.Ordinal);

    private static readonly Regex PlaceholderPattern = new(
        @"\{\{[A-Za-z0-9_.]+\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryValidatePlaceholders(string content, out string? unknownPlaceholder)
    {
        unknownPlaceholder = null;
        foreach (Match match in PlaceholderPattern.Matches(content ?? string.Empty))
        {
            if (!Allowed.Contains(match.Value))
            {
                unknownPlaceholder = match.Value;
                return false;
            }
        }

        return true;
    }

    public static IReadOnlyCollection<string> PlaceholdersInContent(string content)
    {
        var placeholders = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in PlaceholderPattern.Matches(content ?? string.Empty))
        {
            placeholders.Add(match.Value);
        }

        return placeholders;
    }
}

public sealed record HrDocumentRenderContext(
    string EmployeeFullName,
    string EmployeeGivenName,
    string EmployeeFamilyName,
    string EmployeePersonnelNumber,
    DateOnly? EmployeeBirthDate,
    DateOnly EmploymentStartDate,
    string AssignmentDepartmentName,
    string AssignmentPositionName,
    string OrganizationName,
    string PropertyName,
    DateOnly CurrentDate);
