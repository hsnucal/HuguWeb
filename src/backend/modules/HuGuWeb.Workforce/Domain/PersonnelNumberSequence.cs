namespace HuGuWeb.Workforce.Domain;

public sealed class PersonnelNumberSequence
{
    public const int StartingValue = 1001;

    private PersonnelNumberSequence()
    {
    }

    public PersonnelNumberSequence(Guid organizationId, int nextValue)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nextValue, 1);
        OrganizationId = organizationId;
        NextValue = nextValue;
    }

    public Guid OrganizationId { get; private set; }
    public int NextValue { get; private set; }

    public int ReserveNext()
    {
        var reserved = NextValue;
        NextValue = checked(NextValue + 1);
        return reserved;
    }
}
