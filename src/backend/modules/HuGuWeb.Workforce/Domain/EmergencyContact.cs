namespace HuGuWeb.Workforce.Domain;

public sealed class EmergencyContact
{
    public const int NameMaxLength = 100;
    public const int RelationshipMaxLength = 64;

    private EmergencyContact()
    {
        Name = string.Empty;
        Phone = string.Empty;
    }

    private EmergencyContact(
        Guid id,
        Guid employeeId,
        string name,
        string? relationship,
        string phone,
        bool isPrimary,
        int sortOrder)
    {
        Id = id;
        EmployeeId = employeeId;
        Name = name;
        Relationship = relationship;
        Phone = phone;
        IsPrimary = isPrimary;
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string Name { get; private set; }
    public string? Relationship { get; private set; }
    public string Phone { get; private set; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }

    public static bool TryCreate(
        Guid id,
        Guid employeeId,
        string? name,
        string? relationship,
        string? phone,
        bool isPrimary,
        int sortOrder,
        out EmergencyContact? contact,
        out string? fieldSuffix,
        out string? error)
    {
        contact = null;
        fieldSuffix = "name";
        if (!Employee.TryNormalizePersonName(name, "emergency-name", out var normalizedName, out error))
        {
            return false;
        }

        fieldSuffix = "relationship";
        if (!ContactValue.TryNormalizeOptionalText(
                relationship,
                RelationshipMaxLength,
                out var normalizedRelationship,
                out error))
        {
            return false;
        }

        fieldSuffix = "phone";
        if (!ContactValue.TryNormalizePhone(phone, required: true, out var normalizedPhone, out error)
            || normalizedPhone is null)
        {
            return false;
        }

        contact = new EmergencyContact(
            id,
            employeeId,
            normalizedName,
            normalizedRelationship,
            normalizedPhone,
            isPrimary,
            sortOrder);
        fieldSuffix = null;
        return true;
    }

    public static bool TryCreateCollection(
        Guid employeeId,
        IReadOnlyList<EmergencyContactDraft> drafts,
        out IReadOnlyList<EmergencyContact> contacts,
        out string? field,
        out string? error)
    {
        contacts = [];
        field = null;
        if (drafts.Count == 0)
        {
            error = null;
            return true;
        }

        if (drafts.Count(item => item.IsPrimary) > 1)
        {
            field = HrValidation.Fields.EmergencyContacts;
            error = HrValidation.Codes.EmergencyPrimaryMultiple;
            return false;
        }

        var created = new List<EmergencyContact>(drafts.Count);
        for (var index = 0; index < drafts.Count; index++)
        {
            var draft = drafts[index];
            if (!TryCreate(
                    draft.Id == Guid.Empty ? Guid.CreateVersion7() : draft.Id,
                    employeeId,
                    draft.Name,
                    draft.Relationship,
                    draft.Phone,
                    draft.IsPrimary,
                    index,
                    out var contact,
                    out var suffix,
                    out error)
                || contact is null)
            {
                field = suffix == "relationship"
                    ? HrValidation.Fields.EmergencyRelationship(index)
                    : suffix == "phone"
                        ? HrValidation.Fields.EmergencyPhone(index)
                        : HrValidation.Fields.EmergencyName(index);
                return false;
            }

            created.Add(contact);
        }

        contacts = created;
        error = null;
        return true;
    }
}

public sealed record EmergencyContactDraft(
    Guid Id,
    string? Name,
    string? Relationship,
    string? Phone,
    bool IsPrimary);
