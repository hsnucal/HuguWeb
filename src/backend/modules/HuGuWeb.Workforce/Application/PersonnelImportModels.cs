using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public static class PersonnelImportTemplateColumns
{
    public const string PersonnelNumber = PersonnelImportColumnCatalog.Ids.PersonnelNumber;
    public const string GivenName = PersonnelImportColumnCatalog.Ids.GivenName;
    public const string FamilyName = PersonnelImportColumnCatalog.Ids.FamilyName;
    public const string Department = PersonnelImportColumnCatalog.Ids.Department;
    public const string DepartmentCode = PersonnelImportColumnCatalog.Ids.DepartmentCode;
    public const string Position = PersonnelImportColumnCatalog.Ids.Position;
    public const string PositionCode = PersonnelImportColumnCatalog.Ids.PositionCode;
    public const string EmploymentStartDate = PersonnelImportColumnCatalog.Ids.EmploymentStartDate;
    public const string MobilePhone = PersonnelImportColumnCatalog.Ids.MobilePhone;
    public const string Email = PersonnelImportColumnCatalog.Ids.Email;
    public const string EducationLevel = PersonnelImportColumnCatalog.Ids.EducationLevel;
    public const string BloodType = PersonnelImportColumnCatalog.Ids.BloodType;
    public const string Nationality = PersonnelImportColumnCatalog.Ids.Nationality;
    public const string NationalIdentityScheme = PersonnelImportColumnCatalog.Ids.NationalIdentityScheme;
    public const string NationalIdentityNumber = PersonnelImportColumnCatalog.Ids.NationalIdentityNumber;

    public static IReadOnlyDictionary<string, string[]> HeaderAliases =>
        PersonnelImportColumnCatalog.HeaderAliases;
}

public sealed record PersonnelImportRowPreview(
    int RowNumber,
    PersonnelImportAction Action,
    string? PersonnelNumber,
    string GivenName,
    string FamilyName,
    string DepartmentLabel,
    string PositionLabel,
    DateOnly EmploymentStartDate,
    IReadOnlyList<string> ChangedFields,
    IReadOnlyList<PersonnelImportRowError> Errors);

public enum PersonnelImportAction
{
    Create,
    Update
}

public sealed record PersonnelImportRowError(string Field, string Code, string Message);

public sealed record PersonnelImportPreviewResult(
    string PreviewToken,
    int TotalRows,
    int CreateCount,
    int UpdateCount,
    int InvalidCount,
    IReadOnlyList<PersonnelImportRowPreview> Rows,
    bool CanConfirm);

public sealed record PersonnelImportConfirmResult(
    int CreatedCount,
    int UpdatedCount,
    int FailedCount,
    IReadOnlyList<PersonnelImportRowPreview> Rows);

public sealed record PersonnelImportConfirmCommand(
    string PreviewToken,
    PersonnelChangeContext Actor,
    bool CanWriteSensitive);

public sealed class PersonnelImportPreviewStore
{
    private readonly Dictionary<string, StoredPreview> _entries = new(StringComparer.Ordinal);
    private readonly TimeProvider _time;

    public PersonnelImportPreviewStore(TimeProvider time) => _time = time;

    public void Store(string token, StoredPreview preview) => _entries[token] = preview;

    public bool TryGet(string token, out StoredPreview? preview)
    {
        if (_entries.TryGetValue(token, out preview)
            && preview.ExpiresAtUtc > _time.GetUtcNow())
        {
            return true;
        }

        preview = null;
        return false;
    }

    public void Remove(string token) => _entries.Remove(token);

    public sealed record StoredPreview(
        Guid OrganizationId,
        Guid PropertyId,
        string ActorUserId,
        string FileName,
        int TotalRows,
        IReadOnlyList<ValidatedImportRow> ValidRows,
        IReadOnlyList<PersonnelImportRowPreview> InvalidRows,
        DateTimeOffset ExpiresAtUtc,
        bool CanWriteSensitive);

    public sealed record ValidatedImportRow(
        int RowNumber,
        PersonnelImportAction Action,
        Guid? ExistingEmployeeId,
        HireEmployeeWithProfileCommand? CreateCommand,
        UpdateEmployeeHrProfileCommand? UpdateCommand,
        IReadOnlyList<string> ChangedFields,
        string? PaymentIban = null,
        string? PaymentBankName = null);
}
