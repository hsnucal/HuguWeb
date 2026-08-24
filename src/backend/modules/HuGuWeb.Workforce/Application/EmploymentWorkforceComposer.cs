using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed record EmploymentWorkforceWriteModel(
    EmploymentContractType? ContractType,
    DateOnly? ContractEndDate,
    decimal? PartTimeMonthlyHours,
    IskurStatus? IskurStatus,
    DateOnly? IncentiveStartDate,
    DateOnly? IncentiveEndDate,
    IskurWorkforceStatus? IskurWorkforceStatus,
    DateOnly? WorkPermitStartDate,
    DateOnly? WorkPermitEndDate)
{
    public static EmploymentWorkforceWriteModel Empty { get; } =
        new(null, null, null, null, null, null, null, null, null);

    public EmploymentWorkforceTermsValues ToValues() =>
        new(
            ContractType,
            ContractEndDate,
            PartTimeMonthlyHours,
            IskurStatus,
            IncentiveStartDate,
            IncentiveEndDate,
            IskurWorkforceStatus,
            WorkPermitStartDate,
            WorkPermitEndDate);
}

public static class EmploymentWorkforceComposer
{
    public static WorkforceResult<Employment> Apply(Employment employment, EmploymentWorkforceWriteModel model)
    {
        if (!employment.TryApplyWorkforceTerms(model.ToValues(), out var field, out var code))
        {
            return WorkforceError.InvalidFields(
                code ?? "invalid-workforce-terms",
                "Employment workforce terms are invalid.",
                field ?? HrValidation.Fields.ContractType,
                code ?? "invalid-workforce-terms");
        }

        return employment;
    }
}

public static class EmploymentWorkforceRead
{
    public static EmploymentWorkforceReadModel From(Employment employment) =>
        new(
            employment.ContractType,
            employment.ContractEndDate,
            employment.PartTimeMonthlyHours,
            employment.IskurStatus,
            employment.IncentiveStartDate,
            employment.IncentiveEndDate,
            employment.IskurWorkforceStatus,
            employment.WorkPermitStartDate,
            employment.WorkPermitEndDate);
}

public sealed record EmploymentWorkforceReadModel(
    EmploymentContractType? ContractType,
    DateOnly? ContractEndDate,
    decimal? PartTimeMonthlyHours,
    IskurStatus? IskurStatus,
    DateOnly? IncentiveStartDate,
    DateOnly? IncentiveEndDate,
    IskurWorkforceStatus? IskurWorkforceStatus,
    DateOnly? WorkPermitStartDate,
    DateOnly? WorkPermitEndDate);
