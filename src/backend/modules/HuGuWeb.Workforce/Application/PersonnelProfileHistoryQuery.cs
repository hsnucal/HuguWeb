using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class PersonnelProfileHistoryQuery(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<IReadOnlyList<PersonnelProfileChangeRecord>>> ExecuteAsync(
        Guid employeeId,
        bool canReadSensitive,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var changes = await store.ListPersonnelProfileChangesAsync(employeeId, cancellationToken);
        var records = changes
            .OrderByDescending(item => item.ChangedAtUtc)
            .Select(item => new PersonnelProfileChangeRecord(
                item.Id,
                item.FieldCode,
                canReadSensitive || !IsSensitiveField(item.FieldCode) ? item.OldValue : Redact(item.OldValue),
                canReadSensitive || !IsSensitiveField(item.FieldCode) ? item.NewValue : Redact(item.NewValue),
                item.ChangedAtUtc,
                item.ChangedByUserId,
                item.ChangedByEmployeeId,
                item.ChangeSource))
            .ToArray();

        return records;
    }

    private static bool IsSensitiveField(string fieldCode) =>
        fieldCode is PersonnelProfileFieldCodes.NationalIdentityNumber
            or PersonnelProfileFieldCodes.PaymentIban
            or PersonnelProfileFieldCodes.ResidenceAddress
            or PersonnelProfileFieldCodes.NotificationAddress
            or PersonnelProfileFieldCodes.EmergencyContacts;

    private static string? Redact(string? value) => value is null ? null : "••••";
}

public sealed record PersonnelProfileChangeRecord(
    Guid Id,
    string FieldCode,
    string? OldValue,
    string? NewValue,
    DateTimeOffset ChangedAtUtc,
    string ChangedByUserId,
    Guid? ChangedByEmployeeId,
    string? ChangeSource);
