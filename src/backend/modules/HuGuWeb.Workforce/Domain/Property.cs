namespace HuGuWeb.Workforce.Domain;

public sealed class Property
{
    public const int TimeZoneIdMaxLength = 64;

    private Property()
    {
        Name = string.Empty;
        TimeZoneId = string.Empty;
    }

    public Property(Guid id, Guid organizationId, string name, string timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (trimmed.Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "Property name is too long.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        var zone = timeZoneId.Trim();
        if (zone.Length > TimeZoneIdMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(timeZoneId), "Time zone identifier is too long.");
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(zone, out _))
        {
            throw new ArgumentOutOfRangeException(nameof(timeZoneId), "Time zone identifier is not recognized.");
        }

        Id = id;
        OrganizationId = organizationId;
        Name = trimmed;
        TimeZoneId = zone;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; }
    public string TimeZoneId { get; private set; }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (trimmed.Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "Property name is too long.");
        }

        Name = trimmed;
    }

    public void SetTimeZoneId(string timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        var zone = timeZoneId.Trim();
        if (zone.Length > TimeZoneIdMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(timeZoneId), "Time zone identifier is too long.");
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(zone, out _))
        {
            throw new ArgumentOutOfRangeException(nameof(timeZoneId), "Time zone identifier is not recognized.");
        }

        TimeZoneId = zone;
    }
}
