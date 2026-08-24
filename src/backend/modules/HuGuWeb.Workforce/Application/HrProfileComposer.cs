using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed record HrProfileWriteModel(
    NationalIdentityScheme? NationalIdentityScheme,
    string? NationalIdentityNumber,
    string? Nationality,
    Gender? Gender,
    DateOnly? BirthDate,
    string? BirthPlace,
    MaritalStatus? MaritalStatus,
    BloodType? BloodType,
    EducationLevel? EducationLevel,
    string? MobilePhone,
    string? HomePhone,
    string? Email,
    string? ResidenceAddress,
    string? ResidenceCity,
    string? ResidenceDistrict,
    string? NotificationAddress,
    string? HrNotes,
    DrivingLicenceCategory? DrivingLicenceCategory,
    MilitaryServiceStatus? MilitaryServiceStatus,
    string? MilitaryExemptionReason,
    string? MilitaryDefermentReason,
    string? KepAddress,
    string? EducationDescription,
    string? SchoolName,
    DateOnly? GraduationDate,
    ForeignLanguageSummary? ForeignLanguage,
    string? ArgeProjectCode,
    IReadOnlyList<EmergencyContactDraft> EmergencyContacts);

public static class HrProfileAccess
{
    public static bool HasHighlySensitiveWrite(HrProfileWriteModel model) =>
        model.NationalIdentityScheme is not null
        || !string.IsNullOrWhiteSpace(model.NationalIdentityNumber)
        || !string.IsNullOrWhiteSpace(model.ResidenceAddress)
        || !string.IsNullOrWhiteSpace(model.ResidenceCity)
        || !string.IsNullOrWhiteSpace(model.ResidenceDistrict)
        || !string.IsNullOrWhiteSpace(model.NotificationAddress)
        || model.EmergencyContacts.Count > 0;

    public static EmployeeHrProfileValues ToValues(HrProfileWriteModel model) =>
        new(
            model.NationalIdentityScheme,
            model.NationalIdentityNumber,
            model.Nationality,
            model.Gender,
            model.BirthDate,
            model.BirthPlace,
            model.MaritalStatus,
            model.BloodType,
            model.EducationLevel,
            model.MobilePhone,
            model.HomePhone,
            model.Email,
            model.ResidenceAddress,
            model.ResidenceCity,
            model.ResidenceDistrict,
            model.NotificationAddress,
            model.HrNotes,
            model.DrivingLicenceCategory,
            model.MilitaryServiceStatus,
            model.MilitaryExemptionReason,
            model.MilitaryDefermentReason,
            model.KepAddress,
            model.EducationDescription,
            model.SchoolName,
            model.GraduationDate,
            model.ForeignLanguage,
            model.ArgeProjectCode);
}

public static class HrProfileComposer
{
    public static async Task<WorkforceResult<EmployeeHrProfile>> ApplyAsync(
        IWorkforceStore store,
        Employee employee,
        HrProfileWriteModel model,
        DateOnly today,
        bool canWriteSensitive,
        CancellationToken cancellationToken)
    {
        if (!canWriteSensitive && HrProfileAccess.HasHighlySensitiveWrite(model))
        {
            return WorkforceError.SensitiveWriteForbidden();
        }

        var profile = await store.GetHrProfileAsync(employee.Id, cancellationToken);
        var isNew = profile is null;
        profile ??= EmployeeHrProfile.Create(Guid.CreateVersion7(), employee.Id, employee.OrganizationId);

        var values = canWriteSensitive
            ? HrProfileAccess.ToValues(model)
            : NonSensitiveValues(model, profile);

        if (!profile.TryApply(values, today, out var field, out var profileError))
        {
            return WorkforceError.InvalidFields(
                "invalid-hr-profile",
                profileError ?? "HR profile is invalid.",
                field ?? HrValidation.Fields.NationalIdentityNumber,
                profileError ?? "invalid-hr-profile");
        }

        IReadOnlyList<EmergencyContact> contacts = [];
        if (canWriteSensitive)
        {
            if (!EmergencyContact.TryCreateCollection(
                    employee.Id,
                    model.EmergencyContacts,
                    out contacts,
                    out var contactField,
                    out var contactError))
            {
                return WorkforceError.InvalidFields(
                    "invalid-emergency-contact",
                    contactError ?? "Emergency contact is invalid.",
                    contactField ?? HrValidation.Fields.EmergencyContacts,
                    contactError ?? "invalid-emergency-contact");
            }
        }

        if (profile.HasNationalIdentity)
        {
            var other = await store.FindHrProfileByNationalIdentityAsync(
                employee.OrganizationId,
                profile.NationalIdentityScheme!.Value,
                profile.NormalizedNationalIdentityNumber!,
                cancellationToken);
            if (other is not null && other.EmployeeId != employee.Id)
            {
                return WorkforceError.NationalIdentityInUse();
            }
        }

        if (isNew)
        {
            store.AddHrProfile(profile);
        }

        if (canWriteSensitive)
        {
            var currentContacts = await store.ListEmergencyContactsAsync(employee.Id, cancellationToken);
            foreach (var contact in currentContacts)
            {
                store.RemoveEmergencyContact(contact);
            }

            foreach (var contact in contacts)
            {
                store.AddEmergencyContact(contact);
            }
        }

        return profile;
    }

    private static EmployeeHrProfileValues NonSensitiveValues(HrProfileWriteModel model, EmployeeHrProfile current) =>
        new(
            current.NationalIdentityScheme,
            current.NationalIdentityNumber,
            model.Nationality,
            model.Gender,
            model.BirthDate,
            model.BirthPlace,
            model.MaritalStatus,
            model.BloodType,
            model.EducationLevel,
            model.MobilePhone,
            model.HomePhone,
            model.Email,
            current.ResidenceAddress,
            current.ResidenceCity,
            current.ResidenceDistrict,
            current.NotificationAddress,
            model.HrNotes,
            model.DrivingLicenceCategory,
            model.MilitaryServiceStatus,
            model.MilitaryExemptionReason,
            model.MilitaryDefermentReason,
            model.KepAddress,
            model.EducationDescription,
            model.SchoolName,
            model.GraduationDate,
            model.ForeignLanguage,
            model.ArgeProjectCode);
}
