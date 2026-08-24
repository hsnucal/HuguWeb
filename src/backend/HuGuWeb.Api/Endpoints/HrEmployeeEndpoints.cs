using System.Security.Claims;
using HuGuWeb.Api.Authorization;
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
        group.MapPost("/{id:guid}/photo", UploadPhoto)
            .WithName("UploadHrEmployeePhoto")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage)
            .DisableAntiforgery()
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapDelete("/{id:guid}/photo", RemovePhoto)
            .WithName("RemoveHrEmployeePhoto")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static async Task<IResult> ListEmployees(
        ClaimsPrincipal user,
        HrEmployeeDirectoryQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(CanReadSensitive(user), cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetEmployee(
        Guid id,
        ClaimsPrincipal user,
        HrEmployeeCardQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(id, CanReadSensitive(user), cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetPhoto(
        Guid id,
        EmployeePhotoUseCases useCases,
        CancellationToken cancellationToken)
    {
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
                canWriteSensitive),
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
        CancellationToken cancellationToken)
    {
        var canWriteSensitive = CanReadSensitive(user);
        var updated = await update.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                id,
                request.GivenName,
                request.FamilyName,
                request.ToProfileWriteModel(),
                canWriteSensitive),
            cancellationToken);
        if (!updated.IsSuccess)
        {
            return updated.Error!.ToHttp();
        }

        var card = await cardQuery.ExecuteAsync(id, canWriteSensitive, cancellationToken);
        return card.ToHttp();
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
    string? MobilePhone,
    string? HomePhone,
    string? Email,
    string? ResidenceAddress,
    string? ResidenceCity,
    string? ResidenceDistrict,
    string? NotificationAddress,
    string? HrNotes,
    IReadOnlyList<EmergencyContactRequest>? EmergencyContacts);

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
    string? MobilePhone,
    string? HomePhone,
    string? Email,
    string? ResidenceAddress,
    string? ResidenceCity,
    string? ResidenceDistrict,
    string? NotificationAddress,
    string? HrNotes,
    IReadOnlyList<EmergencyContactRequest>? EmergencyContacts);

public sealed record EmergencyContactRequest(
    Guid? Id,
    string? Name,
    string? Relationship,
    string? Phone,
    bool IsPrimary);

internal static class HrEmployeeRequestMapping
{
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
            (contacts ?? []).Select(item => new EmergencyContactDraft(
                item.Id ?? Guid.Empty,
                item.Name,
                item.Relationship,
                item.Phone,
                item.IsPrimary)).ToArray());
}
