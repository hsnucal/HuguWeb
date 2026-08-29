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
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employee = await store.GetEmployeeAsync(command.EmployeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var profileBefore = await store.GetHrProfileAsync(command.EmployeeId, cancellationToken);
        var paymentBefore = await store.GetPaymentProfileAsync(command.EmployeeId, cancellationToken);
        var contactsBefore = command.CanWriteSensitive
            ? await store.ListEmergencyContactsAsync(command.EmployeeId, cancellationToken)
            : [];
        var snapshotBefore = PersonnelProfileChangeRecorder.Capture(employee, profileBefore, paymentBefore);

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

        var employmentTermsBefore = PersonnelProfileChangeRecorder.CaptureEmployment(employment.Value);

        if (command.ApplySeniorityStartDate)
        {
            if (!employment.Value.TryApplySeniorityStartDate(
                    command.SeniorityStartDate,
                    out var seniorityField,
                    out var seniorityCode))
            {
                return WorkforceError.InvalidFields(
                    seniorityCode ?? HrValidation.Codes.SeniorityStartDateInvalid,
                    "Seniority start date is invalid.",
                    seniorityField ?? HrValidation.Fields.SeniorityStartDate,
                    seniorityCode ?? HrValidation.Codes.SeniorityStartDateInvalid);
            }
        }

        if (command.OfficialProfile is not null)
        {
            var official = await OfficialEmploymentComposer.ApplyAsync(
                store,
                employment.Value,
                command.OfficialProfile,
                clock.Today,
                createIfEmpty: false,
                cancellationToken);
            if (!official.IsSuccess)
            {
                return official.Error!;
            }
        }

        if (command.WorkforceTerms is not null)
        {
            var workforce = EmploymentWorkforceComposer.Apply(employment.Value, command.WorkforceTerms);
            if (!workforce.IsSuccess)
            {
                return workforce.Error!;
            }
        }

        if (command.BesSettings is not null)
        {
            var existingBes = await store.GetEmploymentBesSettingsAsync(employment.Value.Id, cancellationToken);
            var bes = EmploymentBesComposer.Apply(
                store,
                employment.Value,
                existingBes,
                command.BesSettings,
                createIfEmpty: false);
            if (!bes.IsSuccess)
            {
                return bes.Error!;
            }
        }

        try
        {
            if (command.ChangeContext is not null)
            {
                var profileAfter = await store.GetHrProfileAsync(command.EmployeeId, cancellationToken);
                var paymentAfter = await store.GetPaymentProfileAsync(command.EmployeeId, cancellationToken);
                var contactsAfter = command.CanWriteSensitive
                    ? await store.ListEmergencyContactsAsync(command.EmployeeId, cancellationToken)
                    : contactsBefore;
                var changes = new List<(string FieldCode, string? OldValue, string? NewValue)>();
                changes.AddRange(PersonnelProfileChangeRecorder.Diff(
                    snapshotBefore,
                    PersonnelProfileChangeRecorder.Capture(employee, profileAfter, paymentAfter)));
                changes.AddRange(PersonnelProfileChangeRecorder.DiffEmergencyContacts(contactsBefore, contactsAfter));
                changes.AddRange(PersonnelProfileChangeRecorder.DiffEmployment(
                    employmentTermsBefore,
                    PersonnelProfileChangeRecorder.CaptureEmployment(employment.Value)));
                PersonnelProfileChangeRecorder.RecordDiff(
                    store,
                    command.EmployeeId,
                    workplace.Value.Organization.Id,
                    workplaceContext.HasProperty ? workplaceContext.PropertyId : null,
                    command.ChangeContext,
                    changes);
            }

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
    EmploymentBesWriteModel? BesSettings = null,
    PersonnelChangeContext? ChangeContext = null,
    DateOnly? SeniorityStartDate = null,
    bool ApplySeniorityStartDate = false);
