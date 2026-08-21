namespace HuGuWeb.Workforce.Domain;

public readonly record struct DatePeriod(DateOnly Start, DateOnly? End)
{
    public bool IsValid => End is null || End >= Start;

    public bool Contains(DateOnly date) =>
        date >= Start && (End is null || date <= End);

    public bool Overlaps(DatePeriod other)
    {
        var thisEnd = End ?? DateOnly.MaxValue;
        var otherEnd = other.End ?? DateOnly.MaxValue;
        return Start <= otherEnd && other.Start <= thisEnd;
    }
}
