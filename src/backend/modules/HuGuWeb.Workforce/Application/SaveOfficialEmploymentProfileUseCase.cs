using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class SaveOfficialEmploymentProfileUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<OfficialEmploymentProfileReadModel>> ExecuteAsync(
        SaveOfficialEmploymentProfileCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employee = await store.GetEmployeeAsync(command.EmployeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var employments = await store.ListEmploymentsAsync(employee.Id, cancellationToken);
        var employment = OfficialEmploymentSelection.ForEmployee(employments);
        if (!employment.IsSuccess)
        {
            return employment.Error!;
        }

        var official = await OfficialEmploymentComposer.ApplyAsync(
            store,
            employment.Value,
            command.OfficialProfile,
            clock.Today,
            createIfEmpty: true,
            cancellationToken);
        if (!official.IsSuccess)
        {
            return official.Error!;
        }

        await store.SaveChangesAsync(cancellationToken);
        return await OfficialEmploymentProfileFactory.CreateAsync(
            store,
            employment.Value,
            official.Value,
            maskWorkplace: true,
            cancellationToken);
    }
}

public sealed record SaveOfficialEmploymentProfileCommand(
    Guid EmployeeId,
    OfficialEmploymentWriteModel OfficialProfile);

public static class OfficialEmploymentProfileFactory
{
    public static async Task<OfficialEmploymentProfileReadModel> CreateAsync(
        IWorkforceStore store,
        Employment employment,
        OfficialEmploymentProfile? profile,
        bool maskWorkplace,
        CancellationToken cancellationToken)
    {
        SgkWorkplaceRegistrationRecord? workplace = null;
        OfficialLookupItem? occupation = null;
        if (profile?.SgkWorkplaceRegistrationId is { } workplaceId)
        {
            var registration = await store.GetSgkWorkplaceRegistrationAsync(workplaceId, cancellationToken);
            if (registration is not null)
            {
                workplace = MaintainSgkWorkplaceRegistrationsUseCase.ToRecord(registration, maskWorkplace);
            }
        }

        if (profile?.OccupationCode is { } occupationCode)
        {
            var row = await store.GetSgkOccupationCodeAsync(occupationCode, cancellationToken);
            if (row is not null)
            {
                occupation = new OfficialLookupItem(row.Code, row.Description, row.IsActive);
            }
        }

        return new OfficialEmploymentProfileReadModel(
            employment.Id,
            profile?.SgkWorkplaceRegistrationId,
            workplace,
            profile?.DocumentTypeCode,
            profile?.ApplicableLawCode,
            profile?.InsuranceBranchCode,
            profile?.OccupationCode,
            occupation,
            profile?.DutyCode);
    }
}

public sealed record OfficialEmploymentProfileReadModel(
    Guid EmploymentId,
    Guid? SgkWorkplaceRegistrationId,
    SgkWorkplaceRegistrationRecord? SgkWorkplace,
    string? DocumentTypeCode,
    string? ApplicableLawCode,
    string? InsuranceBranchCode,
    string? OccupationCode,
    OfficialLookupItem? Occupation,
    string? DutyCode);
