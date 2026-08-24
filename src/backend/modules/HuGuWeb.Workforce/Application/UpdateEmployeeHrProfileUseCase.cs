using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class UpdateEmployeeHrProfileUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<EmployeeHrProfile>> ExecuteAsync(
        UpdateEmployeeHrProfileCommand command,
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

        if (!employee.TryRename(command.GivenName, command.FamilyName, out var nameError))
        {
            return WorkforceError.InvalidFields(
                "invalid-employee",
                nameError ?? "Employee is invalid.",
                WorkforceError.FieldForEmployeeCode(nameError),
                nameError ?? "invalid-employee");
        }

        var profile = await HrProfileComposer.ApplyAsync(
            store,
            employee,
            command.Profile,
            clock.Today,
            command.CanWriteSensitive,
            cancellationToken);
        if (!profile.IsSuccess)
        {
            return profile.Error!;
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
            command.OfficialProfile ?? OfficialEmploymentWriteModel.Empty,
            clock.Today,
            createIfEmpty: false,
            cancellationToken);
        if (!official.IsSuccess)
        {
            return official.Error!;
        }

        var workforce = EmploymentWorkforceComposer.Apply(
            employment.Value,
            command.WorkforceTerms ?? EmploymentWorkforceWriteModel.Empty);
        if (!workforce.IsSuccess)
        {
            return workforce.Error!;
        }

        var existingBes = await store.GetEmploymentBesSettingsAsync(employment.Value.Id, cancellationToken);
        var bes = EmploymentBesComposer.Apply(
            store,
            employment.Value,
            existingBes,
            command.BesSettings ?? EmploymentBesWriteModel.Empty,
            createIfEmpty: false);
        if (!bes.IsSuccess)
        {
            return bes.Error!;
        }

        try
        {
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (PersonnelNumberConflictException)
        {
            return WorkforceError.PersonnelNumberInUse();
        }
        catch (NationalIdentityConflictException)
        {
            return WorkforceError.NationalIdentityInUse();
        }

        return profile.Value!;
    }
}

public sealed record UpdateEmployeeHrProfileCommand(
    Guid EmployeeId,
    string GivenName,
    string FamilyName,
    HrProfileWriteModel Profile,
    bool CanWriteSensitive,
    OfficialEmploymentWriteModel? OfficialProfile = null,
    EmploymentWorkforceWriteModel? WorkforceTerms = null,
    EmploymentBesWriteModel? BesSettings = null);
