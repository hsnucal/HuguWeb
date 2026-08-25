namespace HuGuWeb.Workforce.Domain;

public sealed class Organization
{
    private Organization()
    {
        Name = string.Empty;
    }

    public Organization(Guid id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (trimmed.Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "Organization name is too long.");
        }

        Id = id;
        Name = trimmed;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (trimmed.Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "Organization name is too long.");
        }

        Name = trimmed;
    }
}
