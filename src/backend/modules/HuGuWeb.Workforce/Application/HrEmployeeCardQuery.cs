using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class HrEmployeeCardQuery(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<HrEmployeeCard>> ExecuteAsync(
        Guid employeeId,
        bool canReadSensitive,
        CancellationToken cancellationToken)
    {
        var history = await new EmployeeHistoryQuery(store, clock, workplaceContext)
            .ExecuteAsync(employeeId, cancellationToken);
        if (!history.IsSuccess)
        {
            return history.Error!;
        }

        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        var profile = await store.GetHrProfileAsync(employeeId, cancellationToken);
        var contacts = canReadSensitive
            ? await store.ListEmergencyContactsAsync(employeeId, cancellationToken)
            : [];
        var certificates = await store.ListEmployeeCertificatesAsync(employeeId, cancellationToken);
        var photo = await store.GetEmployeePhotoAsync(employeeId, cancellationToken);
        var paymentProfile = canReadSensitive
            ? await store.GetPaymentProfileAsync(employeeId, cancellationToken)
            : null;
        var employments = await store.ListEmploymentsAsync(employeeId, cancellationToken);
        var targetEmployment = OfficialEmploymentSelection.ForEmployee(employments);
        OfficialEmploymentProfileReadModel? official = null;
        EmploymentWorkforceReadModel? workforce = null;
        EmploymentBesReadModel? bes = null;
        if (targetEmployment.IsSuccess)
        {
            var profileRow = await store.GetOfficialEmploymentProfileAsync(
                targetEmployment.Value.Id,
                cancellationToken);
            official = await OfficialEmploymentProfileFactory.CreateAsync(
                store,
                targetEmployment.Value,
                profileRow,
                maskWorkplace: true,
                cancellationToken);
            workforce = EmploymentWorkforceRead.From(targetEmployment.Value);
            if (targetEmployment.Value.RecruitmentSourceId is { } recruitmentSourceId)
            {
                var source = await store.GetRecruitmentSourceAsync(recruitmentSourceId, cancellationToken);
                if (source is not null)
                {
                    workforce = EmploymentWorkforceRead.From(
                        targetEmployment.Value,
                        source.Name,
                        source.IsActive);
                }
            }
            var besRow = await store.GetEmploymentBesSettingsAsync(
                targetEmployment.Value.Id,
                cancellationToken);
            bes = EmploymentBesRead.From(besRow);
        }

        var propertyName = await ResolvePropertyNameAsync(
            store,
            workplaceContext,
            history.Value,
            cancellationToken);

        return HrEmployeeCardFactory.Create(
            employee!,
            history.Value!,
            workplace.Value.Organization.Name,
            propertyName,
            profile,
            contacts,
            certificates,
            photo is not null,
            canReadSensitive,
            official,
            workforce,
            bes,
            paymentProfile is null
                ? null
                : new EmployeePaymentProfileReadModel(paymentProfile.Iban, paymentProfile.BankName));
    }

    private static async Task<string> ResolvePropertyNameAsync(
        IWorkforceStore store,
        IWorkplaceContext workplaceContext,
        EmployeeHistory? history,
        CancellationToken cancellationToken)
    {
        if (workplaceContext.HasProperty)
        {
            var selected = await store.GetPropertyAsync(workplaceContext.PropertyId, cancellationToken);
            return selected?.Name ?? string.Empty;
        }

        var departmentId = history?.CurrentPrimaryAssignment?.DepartmentId;
        if (departmentId is null)
        {
            return string.Empty;
        }

        var department = await store.GetDepartmentAsync(departmentId.Value, cancellationToken);
        if (department is null)
        {
            return string.Empty;
        }

        var property = await store.GetPropertyAsync(department.PropertyId, cancellationToken);
        return property?.Name ?? string.Empty;
    }
}

public static class HrEmployeeCardFactory
{
    public static HrEmployeeCard Create(
        Employee employee,
        EmployeeHistory history,
        string organizationName,
        string propertyName,
        EmployeeHrProfile? profile,
        IReadOnlyList<EmergencyContact> contacts,
        IReadOnlyList<EmployeeCertificate> certificates,
        bool hasPhoto,
        bool canReadSensitive,
        OfficialEmploymentProfileReadModel? officialProfile = null,
        EmploymentWorkforceReadModel? workforceTerms = null,
        EmploymentBesReadModel? besSettings = null,
        EmployeePaymentProfileReadModel? paymentProfile = null)
    {
        return new HrEmployeeCard(
            employee.Id,
            employee.PersonnelNumber,
            employee.GivenName,
            employee.FamilyName,
            hasPhoto,
            history.CurrentEmployment,
            history.CurrentPrimaryAssignment,
            organizationName,
            propertyName,
            history.Employments,
            new HrProfileReadModel(
                profile?.EducationLevel,
                profile?.EducationDescription,
                profile?.SchoolName,
                profile?.GraduationDate,
                profile?.ForeignLanguage,
                profile?.HrNotes,
                profile?.Nationality,
                profile?.Gender,
                profile?.BirthDate,
                profile?.BirthPlace,
                profile?.MaritalStatus,
                profile?.BloodType,
                profile?.DrivingLicenceCategory,
                profile?.MilitaryServiceStatus,
                profile?.MilitaryExemptionReason,
                profile?.MilitaryDefermentReason,
                profile?.KepAddress,
                profile?.MobilePhone,
                profile?.HomePhone,
                profile?.Email,
                canReadSensitive ? profile?.NationalIdentityScheme : null,
                canReadSensitive ? profile?.NationalIdentityNumber : null,
                canReadSensitive ? profile?.ResidenceAddress : null,
                canReadSensitive ? profile?.ResidenceCity : null,
                canReadSensitive ? profile?.ResidenceDistrict : null,
                canReadSensitive ? profile?.NotificationAddress : null,
                canReadSensitive
                    ? contacts
                        .OrderBy(item => item.SortOrder)
                        .Select(item => new EmergencyContactReadModel(
                            item.Id,
                            item.Name,
                            item.Relationship,
                            item.Phone,
                            item.IsPrimary))
                        .ToArray()
                    : []),
            certificates
                .OrderBy(item => item.SortOrder)
                .Select(item => new EmployeeCertificateReadModel(item.Id, item.Name))
                .ToArray(),
            canReadSensitive,
            officialProfile,
            workforceTerms,
            besSettings,
            paymentProfile);
    }
}

public sealed record EmployeePaymentProfileReadModel(string Iban, string? BankName);

public sealed record HrEmployeeCard(
    Guid EmployeeId,
    string PersonnelNumber,
    string GivenName,
    string FamilyName,
    bool HasPhoto,
    EmploymentHistoryRecord? CurrentEmployment,
    AssignmentHistoryRecord? CurrentPrimaryAssignment,
    string OrganizationName,
    string PropertyName,
    IReadOnlyList<EmploymentHistoryRecord> Employments,
    HrProfileReadModel Profile,
    IReadOnlyList<EmployeeCertificateReadModel> Certificates,
    bool CanReadSensitive,
    OfficialEmploymentProfileReadModel? OfficialProfile,
    EmploymentWorkforceReadModel? WorkforceTerms,
    EmploymentBesReadModel? BesSettings,
    EmployeePaymentProfileReadModel? PaymentProfile);

public sealed record HrProfileReadModel(
    EducationLevel? EducationLevel,
    string? EducationDescription,
    string? SchoolName,
    DateOnly? GraduationDate,
    ForeignLanguageSummary? ForeignLanguage,
    string? HrNotes,
    string? Nationality,
    Gender? Gender,
    DateOnly? BirthDate,
    string? BirthPlace,
    MaritalStatus? MaritalStatus,
    BloodType? BloodType,
    DrivingLicenceCategory? DrivingLicenceCategory,
    MilitaryServiceStatus? MilitaryServiceStatus,
    string? MilitaryExemptionReason,
    string? MilitaryDefermentReason,
    string? KepAddress,
    string? MobilePhone,
    string? HomePhone,
    string? Email,
    NationalIdentityScheme? NationalIdentityScheme,
    string? NationalIdentityNumber,
    string? ResidenceAddress,
    string? ResidenceCity,
    string? ResidenceDistrict,
    string? NotificationAddress,
    IReadOnlyList<EmergencyContactReadModel> EmergencyContacts);

public sealed record EmergencyContactReadModel(
    Guid Id,
    string Name,
    string? Relationship,
    string Phone,
    bool IsPrimary);

public sealed record EmployeeCertificateReadModel(Guid Id, string Name);
