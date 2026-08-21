namespace HuGuWeb.Workforce.Domain;

public sealed class Property
{
    private Property()
    {
        Name = string.Empty;
    }

    public Property(Guid id, Guid organizationId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (trimmed.Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "Property name is too long.");
        }

        Id = id;
        OrganizationId = organizationId;
        Name = trimmed;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; }
}
