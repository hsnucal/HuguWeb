using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed record EmploymentBesWriteModel(
    bool DeductionEnabled,
    decimal? RatePercent,
    decimal? ExtraAmount)
{
    public static EmploymentBesWriteModel Empty { get; } = new(false, null, null);

    public EmploymentBesSettingsValues ToValues() =>
        new(DeductionEnabled, RatePercent, ExtraAmount);
}

public static class EmploymentBesComposer
{
    public static WorkforceResult<EmploymentBesSettings?> Apply(
        IWorkforceStore store,
        Employment employment,
        EmploymentBesSettings? existing,
        EmploymentBesWriteModel model,
        bool createIfEmpty)
    {
        var values = model.ToValues();
        if (values.IsEmpty && existing is null && !createIfEmpty)
        {
            return WorkforceResult<EmploymentBesSettings?>.Success(null);
        }

        var settings = existing ?? EmploymentBesSettings.Create(Guid.CreateVersion7(), employment.Id);
        if (!settings.TryApply(values, out var field, out var code))
        {
            return WorkforceError.InvalidFields(
                code ?? "invalid-bes-settings",
                "BES configuration is invalid.",
                field ?? HrValidation.Fields.BesRatePercent,
                code ?? "invalid-bes-settings");
        }

        if (existing is null)
        {
            store.AddEmploymentBesSettings(settings);
        }

        return settings;
    }
}

public static class EmploymentBesRead
{
    public static EmploymentBesReadModel? From(EmploymentBesSettings? settings) =>
        settings is null
            ? null
            : new EmploymentBesReadModel(settings.DeductionEnabled, settings.RatePercent, settings.ExtraAmount);
}

public sealed record EmploymentBesReadModel(
    bool DeductionEnabled,
    decimal? RatePercent,
    decimal? ExtraAmount);
