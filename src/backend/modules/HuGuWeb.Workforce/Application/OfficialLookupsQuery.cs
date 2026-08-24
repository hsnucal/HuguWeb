using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class OfficialLookupsQuery(IWorkforceStore store)
{
    public const int OccupationSearchLimit = 50;

    public async Task<WorkforceResult<OfficialLookups>> ListAsync(CancellationToken cancellationToken)
    {
        var documentTypes = await store.ListSgkDocumentTypesAsync(cancellationToken);
        var laws = await store.ListApplicableLawCodesAsync(cancellationToken);
        var branches = await store.ListInsuranceBranchesAsync(cancellationToken);
        var dutyCodes = await store.ListEmploymentDutyCodesAsync(cancellationToken);
        return new OfficialLookups(
            documentTypes.Select(ToLookup).ToArray(),
            laws.Select(ToLookup).ToArray(),
            branches.Select(ToLookup).ToArray(),
            dutyCodes.Select(ToLookup).ToArray(),
            Iso3166Alpha2Catalog.Codes);
    }

    public async Task<WorkforceResult<IReadOnlyList<OfficialLookupItem>>> SearchOccupationsAsync(
        string? query,
        CancellationToken cancellationToken)
    {
        var rows = await store.SearchSgkOccupationCodesAsync(
            query,
            OccupationSearchLimit,
            cancellationToken);
        return rows.Select(ToLookup).ToArray();
    }

    public async Task<WorkforceResult<OfficialLookupItem?>> GetOccupationAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var row = await store.GetSgkOccupationCodeAsync(code, cancellationToken);
        return row is null ? null : ToLookup(row);
    }

    private static OfficialLookupItem ToLookup(SgkDocumentType item) =>
        new(item.Code, item.Description, item.IsActive);

    private static OfficialLookupItem ToLookup(ApplicableLawCode item) =>
        new(item.Code, item.Description, item.IsActive);

    private static OfficialLookupItem ToLookup(InsuranceBranch item) =>
        new(item.Code, item.Description, item.IsActive);

    private static OfficialLookupItem ToLookup(SgkOccupationCode item) =>
        new(item.Code, item.Description, item.IsActive);

    private static OfficialLookupItem ToLookup(EmploymentDutyCode item) =>
        new(item.Code, item.Description, item.IsActive);
}

public sealed record OfficialLookups(
    IReadOnlyList<OfficialLookupItem> DocumentTypes,
    IReadOnlyList<OfficialLookupItem> ApplicableLaws,
    IReadOnlyList<OfficialLookupItem> InsuranceBranches,
    IReadOnlyList<OfficialLookupItem> DutyCodes,
    IReadOnlyList<string> Nationalities);

public sealed record OfficialLookupItem(string Code, string Description, bool IsActive);
