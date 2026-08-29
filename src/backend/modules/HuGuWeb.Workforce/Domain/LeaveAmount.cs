namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Leave amounts are measured in days at half-day (0.5) quantum and persisted as numeric(6,1).
/// This helper is the single source of truth for that quantum rule.
/// </summary>
public static class LeaveAmount
{
    /// <summary>Maximum magnitude for numeric(6,1): 99999.9.</summary>
    public const decimal MaxMagnitude = 99999.9m;

    /// <summary>True when the amount is a finite multiple of 0.5 within numeric(6,1) range.</summary>
    public static bool IsHalfDayQuantum(decimal amount)
    {
        if (Math.Abs(amount) > MaxMagnitude)
        {
            return false;
        }

        var doubled = amount * 2m;
        return doubled == decimal.Truncate(doubled);
    }

    /// <summary>Valid positive record/grant amount: &gt; 0 and half-day quantum.</summary>
    public static bool IsValidPositive(decimal amount) => amount > 0m && IsHalfDayQuantum(amount);

    /// <summary>Valid signed adjustment amount: != 0 and half-day quantum.</summary>
    public static bool IsValidNonZero(decimal amount) => amount != 0m && IsHalfDayQuantum(amount);
}
