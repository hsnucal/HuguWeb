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

        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        var profile = await store.GetHrProfileAsync(employeeId, cancellationToken);
        var contacts = canReadSensitive
            ? await store.ListEmergencyContactsAsync(employeeId, cancellationToken)
            : [];
        var photo = await store.GetEmployeePhotoAsync(employeeId, cancellationToken);

        return HrEmployeeCardFactory.Create(
            employee!,
            history.Value!,
            workplace.Value.Organization.Name,
            workplace.Value.Property.Name,
            profile,
            contacts,
            photo is not null,
            canReadSensitive);
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
        bool hasPhoto,
        bool canReadSensitive)
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
                profile?.HrNotes,
                profile?.Nationality,
                profile?.Gender,
                profile?.BirthDate,
                profile?.BirthPlace,
                profile?.MaritalStatus,
                profile?.BloodType,
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
            canReadSensitive);
    }
}

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
    bool CanReadSensitive);

public sealed record HrProfileReadModel(
    EducationLevel? EducationLevel,
    string? HrNotes,
    string? Nationality,
    Gender? Gender,
    DateOnly? BirthDate,
    string? BirthPlace,
    MaritalStatus? MaritalStatus,
    BloodType? BloodType,
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
