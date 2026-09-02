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
    DateOnly? WorkPermitEndDate,
    WorkType? WorkType = null,
    int? ProbationPeriodMonths = null,
    DateOnly? ProbationStartDate = null,
    Guid? RecruitmentSourceId = null)
{
    public static EmploymentWorkforceWriteModel Empty { get; } =
        new(null, null, null, null, null, null, null, null, null, null, null, null, null);

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
            WorkPermitEndDate,
            WorkType,
            ProbationPeriodMonths,
            ProbationStartDate,
            RecruitmentSourceId);
}

public static class EmploymentWorkforceComposer
{
    public static async Task<WorkforceResult<Employment>> ApplyAsync(
        IWorkforceStore store,
        Employment employment,
        EmploymentWorkforceWriteModel model,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (model.RecruitmentSourceId is { } sourceId)
        {
            var source = await store.GetRecruitmentSourceAsync(sourceId, cancellationToken);
            if (source is null || source.OrganizationId != organizationId)
            {
                return WorkforceError.InvalidFields(
                    HrValidation.Codes.RecruitmentSourceNotFound,
                    "Recruitment source was not found.",
                    HrValidation.Fields.RecruitmentSourceId,
                    HrValidation.Codes.RecruitmentSourceNotFound);
            }

            // Keep current inactive selection readable; block only newly chosen inactive sources.
            var isKeepingCurrent = employment.RecruitmentSourceId == sourceId;
            if (!source.IsActive && !isKeepingCurrent)
            {
                return WorkforceError.InvalidFields(
                    HrValidation.Codes.RecruitmentSourceInactive,
                    "Recruitment source is inactive.",
                    HrValidation.Fields.RecruitmentSourceId,
                    HrValidation.Codes.RecruitmentSourceInactive);
            }
        }

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
    public static EmploymentWorkforceReadModel From(
        Employment employment,
        string? recruitmentSourceName = null,
        bool? recruitmentSourceIsActive = null) =>
        new(
            employment.ContractType,
            employment.ContractEndDate,
            employment.PartTimeMonthlyHours,
            employment.IskurStatus,
            employment.IncentiveStartDate,
            employment.IncentiveEndDate,
            employment.IskurWorkforceStatus,
            employment.WorkPermitStartDate,
            employment.WorkPermitEndDate,
            employment.WorkType,
            employment.ProbationPeriodMonths,
            employment.ProbationStartDate,
            employment.ProbationEndDate,
            employment.RecruitmentSourceId,
            recruitmentSourceName,
            recruitmentSourceIsActive);
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
    DateOnly? WorkPermitEndDate,
    WorkType WorkType,
    int? ProbationPeriodMonths,
    DateOnly? ProbationStartDate,
    DateOnly? ProbationEndDate,
    Guid? RecruitmentSourceId,
    string? RecruitmentSourceName = null,
    bool? RecruitmentSourceIsActive = null);
