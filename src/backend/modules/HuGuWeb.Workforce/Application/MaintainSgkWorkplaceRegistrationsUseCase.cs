using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class MaintainSgkWorkplaceRegistrationsUseCase(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext,
    IWorkforceClock clock)
{
    public async Task<WorkforceResult<IReadOnlyList<SgkWorkplaceRegistrationRecord>>> ListAsync(
        bool maskRegistration,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var registrations = await store.ListSgkWorkplaceRegistrationsAsync(
            workplace.Value.Property.Id,
            cancellationToken);
        return registrations
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.DisplayName ?? item.RegistrationNumber)
            .Select(item => ToRecord(item, maskRegistration))
            .ToArray();
    }

    public async Task<WorkforceResult<SgkWorkplaceRegistrationRecord>> CreateAsync(
        CreateSgkWorkplaceRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        if (!SgkWorkplaceRegistration.TryCreate(
                Guid.CreateVersion7(),
                workplace.Value.Property.Id,
                command.RegistrationNumber,
                command.DisplayName,
                clock.UtcNow,
                out var registration,
                out var field,
                out var code)
            || registration is null)
        {
            return WorkforceError.InvalidSgkWorkplace(
                code ?? "invalid-sgk-workplace",
                field ?? HrValidation.Fields.RegistrationNumber,
                "The SGK workplace registration is invalid.");
        }

        store.AddSgkWorkplaceRegistration(registration);
        await store.SaveChangesAsync(cancellationToken);
        return ToRecord(registration, maskRegistration: false);
    }

    public async Task<WorkforceResult<SgkWorkplaceRegistrationRecord>> UpdateAsync(
        UpdateSgkWorkplaceRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var registration = await store.GetSgkWorkplaceRegistrationAsync(command.Id, cancellationToken);
        if (registration is null || registration.PropertyId != workplace.Value.Property.Id)
        {
            return WorkforceError.SgkWorkplaceNotFound();
        }

        if (command.IdentityProvided)
        {
            var number = command.RegistrationNumber ?? registration.RegistrationNumber;
            var name = command.DisplayNameProvided ? command.DisplayName : registration.DisplayName;
            if (!registration.TryUpdate(number, name, out var field, out var code))
            {
                return WorkforceError.InvalidSgkWorkplace(
                    code ?? "invalid-sgk-workplace",
                    field ?? HrValidation.Fields.RegistrationNumber,
                    "The SGK workplace registration is invalid.");
            }
        }

        if (command.IsActive is true)
        {
            registration.Activate();
        }
        else if (command.IsActive is false)
        {
            registration.Deactivate();
        }

        await store.SaveChangesAsync(cancellationToken);
        return ToRecord(registration, maskRegistration: false);
    }

    internal static SgkWorkplaceRegistrationRecord ToRecord(
        SgkWorkplaceRegistration registration,
        bool maskRegistration) =>
        new(
            registration.Id,
            registration.PropertyId,
            maskRegistration ? null : registration.RegistrationNumber,
            registration.DisplayName,
            registration.FormatPickerLabel(maskRegistration),
            registration.IsActive);
}

public sealed record CreateSgkWorkplaceRegistrationCommand(string RegistrationNumber, string? DisplayName);

public sealed record UpdateSgkWorkplaceRegistrationCommand(
    Guid Id,
    string? RegistrationNumber,
    string? DisplayName,
    bool IdentityProvided,
    bool DisplayNameProvided,
    bool? IsActive);

public sealed record SgkWorkplaceRegistrationRecord(
    Guid Id,
    Guid PropertyId,
    string? RegistrationNumber,
    string? DisplayName,
    string PickerLabel,
    bool IsActive);
