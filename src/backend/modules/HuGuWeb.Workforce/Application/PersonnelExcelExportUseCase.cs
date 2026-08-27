using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class PersonnelExcelExportUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext,
    IPersonnelSpreadsheetService spreadsheet)
{
    public async Task<WorkforceResult<byte[]>> ExecuteAsync(
        PersonnelExportQuery query,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var directory = await new HrEmployeeDirectoryQuery(store, clock, workplaceContext)
            .ExecuteAsync(query.CanReadSensitive, cancellationToken);
        if (!directory.IsSuccess)
        {
            return directory.Error!;
        }

        var rows = FilterRows(directory.Value!, query);
        var columns = BuildColumns(query);
        var exportRows = rows.Select(item => ToExportRow(item, query.CanReadSensitive)).ToArray();
        return spreadsheet.BuildExportWorkbook(columns, exportRows);
    }

    private static IReadOnlyList<HrEmployeeListItem> FilterRows(
        IReadOnlyList<HrEmployeeListItem> source,
        PersonnelExportQuery query)
    {
        IEnumerable<HrEmployeeListItem> filtered = source;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var needle = query.Search.Trim().ToLowerInvariant();
            filtered = filtered.Where(item =>
                item.GivenName.ToLowerInvariant().Contains(needle)
                || item.FamilyName.ToLowerInvariant().Contains(needle)
                || item.PersonnelNumber.ToLowerInvariant().Contains(needle));
        }

        if (query.DepartmentId is Guid departmentId)
        {
            filtered = filtered.Where(item => item.DepartmentId == departmentId);
        }

        if (query.PositionId is Guid positionId)
        {
            filtered = filtered.Where(item => item.PositionId == positionId);
        }

        if (query.Status is EmploymentStatus status)
        {
            filtered = filtered.Where(item => item.EmploymentStatus == status);
        }

        if (query.StartFrom is DateOnly startFrom)
        {
            filtered = filtered.Where(item => item.EmploymentStartDate >= startFrom);
        }

        if (query.StartTo is DateOnly startTo)
        {
            filtered = filtered.Where(item => item.EmploymentStartDate <= startTo);
        }

        if (query.EmployeeIds is { Count: > 0 })
        {
            var allowed = query.EmployeeIds.ToHashSet();
            filtered = filtered.Where(item => allowed.Contains(item.EmployeeId));
        }

        return filtered.ToArray();
    }

    private static IReadOnlyList<PersonnelExportColumn> BuildColumns(PersonnelExportQuery query)
    {
        var selected = query.VisibleColumns?.Count > 0
            ? query.VisibleColumns
            :
            [
                "personnelNumber", "name", "department", "position", "startDate", "status", "mobilePhone", "email"
            ];

        var columns = new List<PersonnelExportColumn>();
        foreach (var id in selected.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (TryGetColumn(id, query.CanReadSensitive, out var column))
            {
                columns.Add(column!);
            }
        }

        EnsureIdentityColumns(columns);
        return columns;
    }

    private static void EnsureIdentityColumns(List<PersonnelExportColumn> columns)
    {
        if (!columns.Any(item => item.Id == "personnelNumber"))
        {
            columns.Insert(0, new PersonnelExportColumn("personnelNumber", "Sicil No"));
        }

        if (!columns.Any(item => item.Id is "givenName" or "name"))
        {
            columns.Insert(1, new PersonnelExportColumn("givenName", "Ad"));
        }

        if (!columns.Any(item => item.Id is "familyName" or "name"))
        {
            columns.Insert(2, new PersonnelExportColumn("familyName", "Soyad"));
        }
    }

    private static bool TryGetColumn(string id, bool canReadSensitive, out PersonnelExportColumn? column)
    {
        column = id.ToLowerInvariant() switch
        {
            "personnelnumber" or "personnel_number" => new("personnelNumber", "Sicil No"),
            "givenname" or "given_name" => new("givenName", "Ad"),
            "familyname" or "family_name" => new("familyName", "Soyad"),
            "name" => null,
            "department" => new("department", "Departman"),
            "position" => new("position", "Pozisyon"),
            "startdate" or "start_date" => new("startDate", "İşe Giriş Tarihi"),
            "status" => new("status", "Durum"),
            "mobilephone" or "mobile_phone" => new("mobilePhone", "Cep Telefonu"),
            "email" => new("email", "E-posta"),
            "educationlevel" => new("educationLevel", "Öğrenim Durumu"),
            "bloodtype" => new("bloodType", "Kan Grubu"),
            "nationalidentity" when canReadSensitive => new("nationalIdentityNumber", "Kimlik No"),
            "residenceaddress" when canReadSensitive => new("residenceAddress", "Adres"),
            "paymentiban" when canReadSensitive => new("paymentIban", "IBAN"),
            _ => null
        };
        return column is not null;
    }

    private static PersonnelExportRow ToExportRow(HrEmployeeListItem item, bool canReadSensitive)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["personnelNumber"] = SpreadsheetSafety.SanitizeCellValue(item.PersonnelNumber),
            ["givenName"] = SpreadsheetSafety.SanitizeCellValue(item.GivenName),
            ["familyName"] = SpreadsheetSafety.SanitizeCellValue(item.FamilyName),
            ["department"] = SpreadsheetSafety.SanitizeCellValue(item.DepartmentName),
            ["position"] = SpreadsheetSafety.SanitizeCellValue(item.PositionName),
            ["startDate"] = item.EmploymentStartDate.ToString("yyyy-MM-dd"),
            ["status"] = item.EmploymentStatus.ToString(),
            ["mobilePhone"] = SpreadsheetSafety.SanitizeCellValue(item.MobilePhone),
            ["email"] = SpreadsheetSafety.SanitizeCellValue(item.Email),
            ["educationLevel"] = item.EducationLevel?.ToString(),
            ["bloodType"] = item.BloodType?.ToString(),
        };

        if (canReadSensitive)
        {
            values["nationalIdentityNumber"] = SpreadsheetSafety.SanitizeCellValue(item.NationalIdentityNumber);
        }

        return new PersonnelExportRow(values);
    }
}

public sealed record PersonnelExportQuery(
    bool CanReadSensitive,
    string? Search = null,
    Guid? DepartmentId = null,
    Guid? PositionId = null,
    EmploymentStatus? Status = null,
    DateOnly? StartFrom = null,
    DateOnly? StartTo = null,
    IReadOnlyList<string>? VisibleColumns = null,
    IReadOnlyList<Guid>? EmployeeIds = null);
