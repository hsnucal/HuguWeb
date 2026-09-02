using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Context;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class HrEmployeeEndpoints
{
    public static IEndpointRouteBuilder MapHrEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/hr/employees")
            .WithTags("HR Employees")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeRead);

        group.MapGet("/", ListEmployees)
            .WithName("ListHrEmployees");
        group.MapGet("/{id:guid}", GetEmployee)
            .WithName("GetHrEmployee");
        group.MapGet("/{id:guid}/photo", GetPhoto)
            .WithName("GetHrEmployeePhoto");
        group.MapPost("/", CreateEmployee)
            .WithName("CreateHrEmployee")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeHire)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPut("/{id:guid}", UpdateEmployee)
            .WithName("UpdateHrEmployee")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPut("/{id:guid}/official-profile", UpdateOfficialProfile)
            .WithName("UpdateHrEmployeeOfficialProfile")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/{id:guid}/photo", UploadPhoto)
            .WithName("UploadHrEmployeePhoto")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage)
            .DisableAntiforgery()
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapDelete("/{id:guid}/photo", RemovePhoto)
            .WithName("RemoveHrEmployeePhoto")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        var lookups = endpoints.MapGroup("/api/hr")
            .WithTags("HR Official Employment")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeRead);
        lookups.MapGet("/official-lookups", ListOfficialLookups)
            .WithName("ListHrOfficialLookups");
        lookups.MapGet("/occupation-codes", SearchOccupationCodes)
            .WithName("SearchHrOccupationCodes");
        lookups.MapGet("/sgk-workplace-registrations", ListHrSgkWorkplaces)
            .WithName("ListHrSgkWorkplaceRegistrations");

        return endpoints;
    }

    private static async Task<IResult> ListEmployees(
        ClaimsPrincipal user,
        HrEmployeeDirectoryQuery query,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(CanReadSensitive(user), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToHttp();
        }

        if (tenant.IsOrganizationWide(user))
        {
            return Results.Ok(result.Value);
        }

        var scoped = new List<HrEmployeeListItem>();
        foreach (var item in result.Value!)
        {
            if (await tenant.AllowsEmployeeAsync(user, item.EmployeeId, cancellationToken))
            {
                scoped.Add(item);
            }
        }

        return Results.Ok(scoped);
    }

    private static async Task<IResult> GetEmployee(
        Guid id,
        ClaimsPrincipal user,
        HrEmployeeCardQuery query,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, id, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var result = await query.ExecuteAsync(id, CanReadSensitive(user), cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetPhoto(
        Guid id,
        ClaimsPrincipal user,
        EmployeePhotoUseCases useCases,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, id, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var result = await useCases.OpenAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!.ToHttp();
        }

        return Results.File(result.Value.Content, result.Value.ContentType);
    }

    private static async Task<IResult> CreateEmployee(
        ClaimsPrincipal user,
        [FromBody] CreateHrEmployeeRequest request,
        HireEmployeeWithProfileUseCase hire,
        HrEmployeeCardQuery cardQuery,
        CancellationToken cancellationToken)
    {
        var canWriteSensitive = CanReadSensitive(user);
        var hired = await hire.ExecuteAsync(
            new HireEmployeeWithProfileCommand(
                request.GivenName,
                request.FamilyName,
                request.EmploymentStartDate,
                request.DepartmentId,
                request.PositionId,
                request.ToProfileWriteModel(),
                canWriteSensitive,
                request.OfficialProfile.ToWriteModel(),
                request.WorkforceTerms.ToWriteModel(),
                request.BesSettings.ToWriteModel(),
                request.SeniorityStartDate,
                request.ToCertificateDrafts()),
            cancellationToken);
        if (!hired.IsSuccess)
        {
            return hired.Error!.ToHttp();
        }

        var card = await cardQuery.ExecuteAsync(hired.Value.EmployeeId, canWriteSensitive, cancellationToken);
        return card.IsSuccess
            ? Results.Created($"/api/hr/employees/{hired.Value.EmployeeId}", card.Value)
            : card.Error!.ToHttp();
    }

    private static async Task<IResult> UpdateEmployee(
        Guid id,
        ClaimsPrincipal user,
        [FromBody] UpdateHrEmployeeRequest request,
        UpdateEmployeeHrProfileUseCase update,
        HrEmployeeCardQuery cardQuery,
        EmployeeTenantGuard tenant,
        IRequestActorContext actorContext,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, id, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var canWriteSensitive = CanReadSensitive(user);
        var updated = await update.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                id,
                request.GivenName,
                request.FamilyName,
                request.ToProfileWriteModel(),
                canWriteSensitive,
                request.OfficialProfile.ToWriteModel(),
                request.WorkforceTerms.ToWriteModel(),
                request.BesSettings.ToWriteModel(),
                ToChangeContext(user, actorContext),
                request.SeniorityStartDate,
                ApplySeniorityStartDate: true,
                request.ToCertificateDrafts()),
            cancellationToken);
        if (!updated.IsSuccess)
        {
            return updated.Error!.ToHttp();
        }

        var card = await cardQuery.ExecuteAsync(id, canWriteSensitive, cancellationToken);
        return card.ToHttp();
    }

    private static async Task<IResult> UpdateOfficialProfile(
        Guid id,
        ClaimsPrincipal user,
        [FromBody] OfficialEmploymentRequest request,
        SaveOfficialEmploymentProfileUseCase update,
        HrEmployeeCardQuery cardQuery,
        CancellationToken cancellationToken)
    {
        var saved = await update.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(id, request.ToWriteModel()),
            cancellationToken);
        if (!saved.IsSuccess)
        {
            return saved.Error!.ToHttp();
        }

        var card = await cardQuery.ExecuteAsync(id, CanReadSensitive(user), cancellationToken);
        return card.ToHttp();
    }

    private static async Task<IResult> ListOfficialLookups(
        OfficialLookupsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ListAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> SearchOccupationCodes(
        OfficialLookupsQuery query,
        string? q,
        CancellationToken cancellationToken)
    {
        var result = await query.SearchOccupationsAsync(q, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ListHrSgkWorkplaces(
        MaintainSgkWorkplaceRegistrationsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ListAsync(maskRegistration: true, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> UploadPhoto(
        Guid id,
        HttpRequest request,
        EmployeePhotoUseCases useCases,
        HrEmployeeCardQuery cardQuery,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return WorkforceError.InvalidPhoto("Photo file is required.").ToHttp();
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            return WorkforceError.InvalidPhoto("Photo file is required.").ToHttp();
        }

        await using var stream = file.OpenReadStream();
        var uploaded = await useCases.UploadAsync(
            id,
            stream,
            file.ContentType,
            file.Length,
            cancellationToken);
        if (!uploaded.IsSuccess)
        {
            return uploaded.Error!.ToHttp();
        }

        var card = await cardQuery.ExecuteAsync(id, CanReadSensitive(user), cancellationToken);
        return card.ToHttp();
    }

    private static async Task<IResult> RemovePhoto(
        Guid id,
        EmployeePhotoUseCases useCases,
        CancellationToken cancellationToken)
    {
        var result = await useCases.RemoveAsync(id, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToHttp();
    }

    private static bool CanReadSensitive(ClaimsPrincipal user) =>
        user.HasClaim(HrEmployeePermissions.ClaimType, HrEmployeePermissions.SensitiveRead);

    private static PersonnelChangeContext? ToChangeContext(
        ClaimsPrincipal user,
        IRequestActorContext actorContext)
    {
        var actor = actorContext.Current;
        return actor is null
            ? null
            : new PersonnelChangeContext(
                actor.UserId,
                actor.EmployeeId,
                actor.OrganizationId,
                actor.PropertyId,
                actor.OccurredAtUtc,
                PersonnelChangeSources.Manual);
    }
}

public sealed record CreateHrEmployeeRequest(
    string GivenName,
    string FamilyName,
    string? PersonnelNumber,
    DateOnly EmploymentStartDate,
    Guid DepartmentId,
    Guid PositionId,
    NationalIdentityScheme? NationalIdentityScheme,
    string? NationalIdentityNumber,
    string? Nationality,
    Gender? Gender,
    DateOnly? BirthDate,
    string? BirthPlace,
    MaritalStatus? MaritalStatus,
    BloodType? BloodType,
    EducationLevel? EducationLevel,
    string? EducationDescription,
    string? SchoolName,
    DateOnly? GraduationDate,
    ForeignLanguageSummary? ForeignLanguage,
    DrivingLicenceCategory? DrivingLicenceCategory,
    MilitaryServiceStatus? MilitaryServiceStatus,
    string? MilitaryExemptionReason,
    string? MilitaryDefermentReason,
    string? KepAddress,
    string? MobilePhone,
    string? HomePhone,
    string? Email,
    string? ResidenceAddress,
    string? ResidenceCity,
    string? ResidenceDistrict,
    string? NotificationAddress,
    string? HrNotes,
    IReadOnlyList<EmergencyContactRequest>? EmergencyContacts,
    IReadOnlyList<EmployeeCertificateRequest>? Certificates,
    OfficialEmploymentRequest? OfficialProfile,
    EmploymentWorkforceRequest? WorkforceTerms,
    EmploymentBesRequest? BesSettings,
    DateOnly? SeniorityStartDate);

public sealed record UpdateHrEmployeeRequest(
    string GivenName,
    string FamilyName,
    string? PersonnelNumber,
    NationalIdentityScheme? NationalIdentityScheme,
    string? NationalIdentityNumber,
    string? Nationality,
    Gender? Gender,
    DateOnly? BirthDate,
    string? BirthPlace,
    MaritalStatus? MaritalStatus,
    BloodType? BloodType,
    EducationLevel? EducationLevel,
    string? EducationDescription,
    string? SchoolName,
    DateOnly? GraduationDate,
    ForeignLanguageSummary? ForeignLanguage,
    DrivingLicenceCategory? DrivingLicenceCategory,
    MilitaryServiceStatus? MilitaryServiceStatus,
    string? MilitaryExemptionReason,
    string? MilitaryDefermentReason,
    string? KepAddress,
    string? MobilePhone,
    string? HomePhone,
    string? Email,
    string? ResidenceAddress,
    string? ResidenceCity,
    string? ResidenceDistrict,
    string? NotificationAddress,
    string? HrNotes,
    IReadOnlyList<EmergencyContactRequest>? EmergencyContacts,
    IReadOnlyList<EmployeeCertificateRequest>? Certificates,
    OfficialEmploymentRequest? OfficialProfile,
    EmploymentWorkforceRequest? WorkforceTerms,
    EmploymentBesRequest? BesSettings,
    DateOnly? SeniorityStartDate);

public sealed record EmergencyContactRequest(
    Guid? Id,
    string? Name,
    string? Relationship,
    string? Phone,
    bool IsPrimary);

public sealed record EmployeeCertificateRequest(Guid? Id, string? Name);

public sealed record OfficialEmploymentRequest(
    Guid? SgkWorkplaceRegistrationId,
    string? DocumentTypeCode,
    string? ApplicableLawCode,
    string? InsuranceBranchCode,
    string? OccupationCode,
    string? DutyCode);

public sealed record EmploymentWorkforceRequest(
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
    Guid? RecruitmentSourceId = null);

public sealed record EmploymentBesRequest(
    bool DeductionEnabled,
    decimal? RatePercent,
    decimal? ExtraAmount);

internal static class HrEmployeeRequestMapping
{
    public static OfficialEmploymentWriteModel ToWriteModel(this OfficialEmploymentRequest? request) =>
        request is null
            ? OfficialEmploymentWriteModel.Empty
            : new OfficialEmploymentWriteModel(
                request.SgkWorkplaceRegistrationId,
                request.DocumentTypeCode,
                request.ApplicableLawCode,
                request.InsuranceBranchCode,
                request.OccupationCode,
                request.DutyCode);

    public static EmploymentWorkforceWriteModel ToWriteModel(this EmploymentWorkforceRequest? request) =>
        request is null
            ? EmploymentWorkforceWriteModel.Empty
            : new EmploymentWorkforceWriteModel(
                request.ContractType,
                request.ContractEndDate,
                request.PartTimeMonthlyHours,
                request.IskurStatus,
                request.IncentiveStartDate,
                request.IncentiveEndDate,
                request.IskurWorkforceStatus,
                request.WorkPermitStartDate,
                request.WorkPermitEndDate,
                request.WorkType,
                request.ProbationPeriodMonths,
                request.ProbationStartDate,
                request.RecruitmentSourceId);

    public static IReadOnlyList<EmployeeCertificateDraft> ToCertificateDrafts(
        this CreateHrEmployeeRequest request) =>
        (request.Certificates ?? [])
            .Select(item => new EmployeeCertificateDraft(item.Id ?? Guid.Empty, item.Name))
            .ToArray();

    public static IReadOnlyList<EmployeeCertificateDraft> ToCertificateDrafts(
        this UpdateHrEmployeeRequest request) =>
        (request.Certificates ?? [])
            .Select(item => new EmployeeCertificateDraft(item.Id ?? Guid.Empty, item.Name))
            .ToArray();

    public static EmploymentBesWriteModel ToWriteModel(this EmploymentBesRequest? request) =>
        request is null
            ? EmploymentBesWriteModel.Empty
            : new EmploymentBesWriteModel(request.DeductionEnabled, request.RatePercent, request.ExtraAmount);

    public static HrProfileWriteModel ToProfileWriteModel(this CreateHrEmployeeRequest request) =>
        ToProfileWriteModel(
            request.NationalIdentityScheme,
            request.NationalIdentityNumber,
            request.Nationality,
            request.Gender,
            request.BirthDate,
            request.BirthPlace,
            request.MaritalStatus,
            request.BloodType,
            request.EducationLevel,
            request.MobilePhone,
            request.HomePhone,
            request.Email,
            request.ResidenceAddress,
            request.ResidenceCity,
            request.ResidenceDistrict,
            request.NotificationAddress,
            request.HrNotes,
            request.DrivingLicenceCategory,
            request.MilitaryServiceStatus,
            request.MilitaryExemptionReason,
            request.MilitaryDefermentReason,
            request.KepAddress,
            request.EducationDescription,
            request.SchoolName,
            request.GraduationDate,
            request.ForeignLanguage,
            request.EmergencyContacts);

    public static HrProfileWriteModel ToProfileWriteModel(this UpdateHrEmployeeRequest request) =>
        ToProfileWriteModel(
            request.NationalIdentityScheme,
            request.NationalIdentityNumber,
            request.Nationality,
            request.Gender,
            request.BirthDate,
            request.BirthPlace,
            request.MaritalStatus,
            request.BloodType,
            request.EducationLevel,
            request.MobilePhone,
            request.HomePhone,
            request.Email,
            request.ResidenceAddress,
            request.ResidenceCity,
            request.ResidenceDistrict,
            request.NotificationAddress,
            request.HrNotes,
            request.DrivingLicenceCategory,
            request.MilitaryServiceStatus,
            request.MilitaryExemptionReason,
            request.MilitaryDefermentReason,
            request.KepAddress,
            request.EducationDescription,
            request.SchoolName,
            request.GraduationDate,
            request.ForeignLanguage,
            request.EmergencyContacts);

    private static HrProfileWriteModel ToProfileWriteModel(
        NationalIdentityScheme? scheme,
        string? number,
        string? nationality,
        Gender? gender,
        DateOnly? birthDate,
        string? birthPlace,
        MaritalStatus? maritalStatus,
        BloodType? bloodType,
        EducationLevel? educationLevel,
        string? mobilePhone,
        string? homePhone,
        string? email,
        string? residenceAddress,
        string? residenceCity,
        string? residenceDistrict,
        string? notificationAddress,
        string? hrNotes,
        DrivingLicenceCategory? drivingLicenceCategory,
        MilitaryServiceStatus? militaryServiceStatus,
        string? militaryExemptionReason,
        string? militaryDefermentReason,
        string? kepAddress,
        string? educationDescription,
        string? schoolName,
        DateOnly? graduationDate,
        ForeignLanguageSummary? foreignLanguage,
        IReadOnlyList<EmergencyContactRequest>? contacts) =>
        new(
            scheme,
            number,
            nationality,
            gender,
            birthDate,
            birthPlace,
            maritalStatus,
            bloodType,
            educationLevel,
            mobilePhone,
            homePhone,
            email,
            residenceAddress,
            residenceCity,
            residenceDistrict,
            notificationAddress,
            hrNotes,
            drivingLicenceCategory,
            militaryServiceStatus,
            militaryExemptionReason,
            militaryDefermentReason,
            kepAddress,
            educationDescription,
            schoolName,
            graduationDate,
            foreignLanguage,
            (contacts ?? []).Select(item => new EmergencyContactDraft(
                item.Id ?? Guid.Empty,
                item.Name,
                item.Relationship,
                item.Phone,
                item.IsPrimary)).ToArray());
}
