using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class SaveEmployeePaymentProfileUseCase(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<EmployeePaymentProfile>> ExecuteAsync(
        SaveEmployeePaymentProfileCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.CanWriteSensitive)
        {
            return WorkforceError.SensitiveWriteForbidden();
        }

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

        var existing = await store.GetPaymentProfileAsync(command.EmployeeId, cancellationToken);
        EmployeePaymentProfile profile;
        if (existing is null)
        {
            if (!EmployeePaymentProfile.TryCreate(
                    Guid.CreateVersion7(),
                    command.EmployeeId,
                    workplace.Value.Organization.Id,
                    command.Iban,
                    command.BankName,
                    out var created,
                    out var createError)
                || created is null)
            {
                return createError?.Contains("IBAN", StringComparison.OrdinalIgnoreCase) == true
                    ? WorkforceError.PaymentProfileInvalidIban()
                    : WorkforceError.InvalidRequest("payment-profile-invalid", createError ?? "Payment profile is invalid.");
            }

            profile = created;
            store.AddPaymentProfile(profile);
        }
        else
        {
            profile = existing;
            if (!profile.TryUpdate(command.Iban, command.BankName, out var updateError))
            {
                return updateError?.Contains("IBAN", StringComparison.OrdinalIgnoreCase) == true
                    ? WorkforceError.PaymentProfileInvalidIban()
                    : WorkforceError.InvalidRequest("payment-profile-invalid", updateError ?? "Payment profile is invalid.");
            }
        }

        if (command.ChangeContext is not null)
        {
            PersonnelProfileChangeRecorder.RecordDiff(
                store,
                command.EmployeeId,
                workplace.Value.Organization.Id,
                workplaceContext.HasProperty ? workplaceContext.PropertyId : null,
                command.ChangeContext,
                PersonnelProfileChangeRecorder.DiffPaymentProfile(existing, profile));
        }

        await store.SaveChangesAsync(cancellationToken);
        return profile;
    }
}

public sealed record SaveEmployeePaymentProfileCommand(
    Guid EmployeeId,
    string Iban,
    string? BankName,
    bool CanWriteSensitive,
    PersonnelChangeContext? ChangeContext = null);
