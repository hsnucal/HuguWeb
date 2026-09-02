namespace HuGuWeb.Workforce.Domain;

public sealed class EmployeeCertificate
{
    public const int NameMaxLength = 200;

    private EmployeeCertificate()
    {
        Name = string.Empty;
    }

    private EmployeeCertificate(
        Guid id,
        Guid employeeId,
        string name,
        int sortOrder,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        EmployeeId = employeeId;
        Name = name;
        SortOrder = sortOrder;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string Name { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static bool TryCreate(
        Guid id,
        Guid employeeId,
        string? name,
        int sortOrder,
        DateTimeOffset utcNow,
        out EmployeeCertificate? certificate,
        out string? fieldSuffix,
        out string? error)
    {
        certificate = null;
        fieldSuffix = "name";
        if (string.IsNullOrWhiteSpace(name))
        {
            error = HrValidation.Codes.CertificateNameRequired;
            return false;
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            error = HrValidation.Codes.CertificateNameTooLong;
            return false;
        }

        certificate = new EmployeeCertificate(id, employeeId, trimmed, sortOrder, utcNow);
        fieldSuffix = null;
        error = null;
        return true;
    }

    public static bool TryCreateCollection(
        Guid employeeId,
        IReadOnlyList<EmployeeCertificateDraft> drafts,
        DateTimeOffset utcNow,
        out IReadOnlyList<EmployeeCertificate> certificates,
        out string? field,
        out string? error)
    {
        certificates = [];
        field = null;
        if (drafts.Count == 0)
        {
            error = null;
            return true;
        }

        var created = new List<EmployeeCertificate>(drafts.Count);
        for (var index = 0; index < drafts.Count; index++)
        {
            var draft = drafts[index];
            if (!TryCreate(
                    draft.Id == Guid.Empty ? Guid.CreateVersion7() : draft.Id,
                    employeeId,
                    draft.Name,
                    index,
                    utcNow,
                    out var certificate,
                    out _,
                    out error)
                || certificate is null)
            {
                field = HrValidation.Fields.CertificateName(index);
                return false;
            }

            created.Add(certificate);
        }

        certificates = created;
        error = null;
        return true;
    }
}

public sealed record EmployeeCertificateDraft(Guid Id, string? Name);
