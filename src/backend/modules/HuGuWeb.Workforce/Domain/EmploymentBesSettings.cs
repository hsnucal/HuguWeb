namespace HuGuWeb.Workforce.Domain;

public sealed class EmploymentBesSettings
{
    public const decimal RateMin = 0m;
    public const decimal RateMax = 100m;
    public const decimal ExtraMin = 0m;

    private EmploymentBesSettings()
    {
    }

    private EmploymentBesSettings(Guid id, Guid employmentId)
    {
        Id = id;
        EmploymentId = employmentId;
    }

    public Guid Id { get; private set; }
    public Guid EmploymentId { get; private set; }
    public bool DeductionEnabled { get; private set; }
    public decimal? RatePercent { get; private set; }
    public decimal? ExtraAmount { get; private set; }

    public static EmploymentBesSettings Create(Guid id, Guid employmentId) =>
        new(id, employmentId);

    public bool IsEmpty => !DeductionEnabled && RatePercent is null && ExtraAmount is null;

    public bool TryApply(EmploymentBesSettingsValues values, out string? field, out string? code)
    {
        field = null;
        code = null;
        if (!values.DeductionEnabled)
        {
            DeductionEnabled = false;
            RatePercent = null;
            ExtraAmount = null;
            return true;
        }

        if (values.RatePercent is { } rate && (rate < RateMin || rate > RateMax))
        {
            field = HrValidation.Fields.BesRatePercent;
            code = HrValidation.Codes.BesRateInvalid;
            return false;
        }

        if (values.ExtraAmount is { } extra && extra < ExtraMin)
        {
            field = HrValidation.Fields.BesExtraAmount;
            code = HrValidation.Codes.BesExtraAmountInvalid;
            return false;
        }

        DeductionEnabled = true;
        RatePercent = values.RatePercent;
        ExtraAmount = values.ExtraAmount;
        return true;
    }
}

public sealed record EmploymentBesSettingsValues(
    bool DeductionEnabled,
    decimal? RatePercent,
    decimal? ExtraAmount)
{
    public bool IsEmpty => !DeductionEnabled && RatePercent is null && ExtraAmount is null;
}
