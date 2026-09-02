using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public static class CertificatesComposer
{
    public static async Task<WorkforceResult<IReadOnlyList<EmployeeCertificate>>> ReplaceAllAsync(
        IWorkforceStore store,
        Guid employeeId,
        IReadOnlyList<EmployeeCertificateDraft> drafts,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (!EmployeeCertificate.TryCreateCollection(
                employeeId,
                drafts,
                utcNow,
                out var certificates,
                out var field,
                out var error))
        {
            return WorkforceError.InvalidFields(
                error ?? "invalid-certificate",
                "Employee certificate is invalid.",
                field ?? HrValidation.Fields.Certificates,
                error ?? "invalid-certificate");
        }

        var current = await store.ListEmployeeCertificatesAsync(employeeId, cancellationToken);
        foreach (var certificate in current)
        {
            store.RemoveEmployeeCertificate(certificate);
        }

        foreach (var certificate in certificates)
        {
            store.AddEmployeeCertificate(certificate);
        }

        return WorkforceResult<IReadOnlyList<EmployeeCertificate>>.Success(certificates);
    }
}
