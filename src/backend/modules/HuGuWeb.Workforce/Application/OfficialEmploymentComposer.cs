using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed record OfficialEmploymentWriteModel(
    Guid? SgkWorkplaceRegistrationId,
    string? DocumentTypeCode,
    string? ApplicableLawCode,
    string? InsuranceBranchCode,
    string? OccupationCode,
    string? DutyCode)
{
    public static OfficialEmploymentWriteModel Empty { get; } = new(null, null, null, null, null, null);

    public OfficialEmploymentProfileValues ToValues() =>
        new(
            SgkWorkplaceRegistrationId,
            NormalizeCode(DocumentTypeCode),
            NormalizeCode(ApplicableLawCode),
            NormalizeCode(InsuranceBranchCode),
            NormalizeCode(OccupationCode),
            NormalizeCode(DutyCode));

    private static string? NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

public static class OfficialEmploymentComposer
{
    public static async Task<WorkforceResult<OfficialEmploymentProfile?>> ApplyAsync(
        IWorkforceStore store,
        Employment employment,
        OfficialEmploymentWriteModel model,
        DateOnly today,
        bool createIfEmpty,
        CancellationToken cancellationToken,
        IReadOnlyList<Assignment>? knownAssignments = null)
    {
        var values = model.ToValues();
        var existing = await store.GetOfficialEmploymentProfileAsync(employment.Id, cancellationToken);
        if (values.IsEmpty && existing is null && !createIfEmpty)
        {
            return WorkforceResult<OfficialEmploymentProfile?>.Success(null);
        }

        var validated = await ValidateAsync(
            store,
            employment,
            existing,
            values,
            today,
            cancellationToken,
            knownAssignments);
        if (!validated.IsSuccess)
        {
            return validated.Error!;
        }

        var profile = existing ?? OfficialEmploymentProfile.Create(Guid.CreateVersion7(), employment.Id);
        var isNew = existing is null;
        profile.Apply(values);
        if (isNew)
        {
            store.AddOfficialEmploymentProfile(profile);
        }

        return profile;
    }

    private static async Task<WorkforceResult<OfficialEmploymentProfileValues>> ValidateAsync(
        IWorkforceStore store,
        Employment employment,
        OfficialEmploymentProfile? existing,
        OfficialEmploymentProfileValues values,
        DateOnly today,
        CancellationToken cancellationToken,
        IReadOnlyList<Assignment>? knownAssignments)
    {
        if (values.SgkWorkplaceRegistrationId is { } workplaceId)
        {
            var registration = await store.GetSgkWorkplaceRegistrationAsync(workplaceId, cancellationToken);
            if (registration is null)
            {
                return WorkforceError.SgkWorkplaceNotFound();
            }

            var assignments = knownAssignments
                ?? await store.ListAssignmentsAsync(employment.Id, cancellationToken);
            var departmentIds = assignments.Select(item => item.DepartmentId).Distinct().ToArray();
            var departments = new Dictionary<Guid, Department>();
            foreach (var departmentId in departmentIds)
            {
                var department = await store.GetDepartmentAsync(departmentId, cancellationToken);
                if (department is not null)
                {
                    departments[department.Id] = department;
                }
            }

            var property = EmploymentPropertyContext.Resolve(employment, assignments, departments, today);
            if (!property.IsSuccess)
            {
                return property.Error!;
            }

            if (registration.PropertyId != property.Value)
            {
                return WorkforceError.SgkWorkplaceNotForProperty();
            }

            if (!registration.IsActive && existing?.SgkWorkplaceRegistrationId != workplaceId)
            {
                return WorkforceError.SgkWorkplaceInactive();
            }
        }

        if (!await LookupAllowsAsync(
                values.DocumentTypeCode,
                existing?.DocumentTypeCode,
                code => store.GetSgkDocumentTypeAsync(code, cancellationToken),
                item => item.IsActive))
        {
            return WorkforceError.InvalidDocumentTypeCode();
        }

        if (!await LookupAllowsAsync(
                values.ApplicableLawCode,
                existing?.ApplicableLawCode,
                code => store.GetApplicableLawCodeAsync(code, cancellationToken),
                item => item.IsActive))
        {
            return WorkforceError.InvalidApplicableLawCode();
        }

        if (!await LookupAllowsAsync(
                values.InsuranceBranchCode,
                existing?.InsuranceBranchCode,
                code => store.GetInsuranceBranchAsync(code, cancellationToken),
                item => item.IsActive))
        {
            return WorkforceError.InvalidInsuranceBranchCode();
        }

        if (values.OccupationCode is not null)
        {
            if (!SgkOccupationCode.IsValidFormat(values.OccupationCode))
            {
                return WorkforceError.InvalidOccupationCode();
            }

            if (!await LookupAllowsAsync(
                    values.OccupationCode,
                    existing?.OccupationCode,
                    code => store.GetSgkOccupationCodeAsync(code, cancellationToken),
                    item => item.IsActive))
            {
                return WorkforceError.InvalidOccupationCode();
            }
        }

        if (!await LookupAllowsAsync(
                values.DutyCode,
                existing?.DutyCode,
                code => store.GetEmploymentDutyCodeAsync(code, cancellationToken),
                item => item.IsActive))
        {
            return WorkforceError.InvalidDutyCode();
        }

        return values;
    }

    private static async Task<bool> LookupAllowsAsync<T>(
        string? requested,
        string? currentlyStored,
        Func<string, Task<T?>> load,
        Func<T, bool> isActive)
        where T : class
    {
        if (requested is null)
        {
            return true;
        }

        var row = await load(requested);
        if (row is null)
        {
            return false;
        }

        if (string.Equals(requested, currentlyStored, StringComparison.Ordinal))
        {
            return true;
        }

        return isActive(row);
    }
}
