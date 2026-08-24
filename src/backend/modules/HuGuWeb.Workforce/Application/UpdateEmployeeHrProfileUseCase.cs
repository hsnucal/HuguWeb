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
    bool CanWriteSensitive);
