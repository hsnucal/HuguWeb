namespace HuGuWeb.Workforce.Domain;

public sealed class OfficialEmploymentProfile
{
    private OfficialEmploymentProfile()
    {
    }

    private OfficialEmploymentProfile(Guid id, Guid employmentId)
    {
        Id = id;
        EmploymentId = employmentId;
    }

    public Guid Id { get; private set; }
    public Guid EmploymentId { get; private set; }
    public Guid? SgkWorkplaceRegistrationId { get; private set; }
    public string? DocumentTypeCode { get; private set; }
    public string? ApplicableLawCode { get; private set; }
    public string? InsuranceBranchCode { get; private set; }
    public string? OccupationCode { get; private set; }
    public string? DutyCode { get; private set; }

    public static OfficialEmploymentProfile Create(Guid id, Guid employmentId) =>
        new(id, employmentId);

    public void Apply(OfficialEmploymentProfileValues values)
    {
        SgkWorkplaceRegistrationId = values.SgkWorkplaceRegistrationId;
        DocumentTypeCode = values.DocumentTypeCode;
        ApplicableLawCode = values.ApplicableLawCode;
        InsuranceBranchCode = values.InsuranceBranchCode;
        OccupationCode = values.OccupationCode;
        DutyCode = values.DutyCode;
    }

    public bool IsEmpty =>
        SgkWorkplaceRegistrationId is null
        && DocumentTypeCode is null
        && ApplicableLawCode is null
        && InsuranceBranchCode is null
        && OccupationCode is null
        && DutyCode is null;
}

public sealed record OfficialEmploymentProfileValues(
    Guid? SgkWorkplaceRegistrationId,
    string? DocumentTypeCode,
    string? ApplicableLawCode,
    string? InsuranceBranchCode,
    string? OccupationCode,
    string? DutyCode)
{
    public bool IsEmpty =>
        SgkWorkplaceRegistrationId is null
        && DocumentTypeCode is null
        && ApplicableLawCode is null
        && InsuranceBranchCode is null
        && OccupationCode is null
        && DutyCode is null;
}
