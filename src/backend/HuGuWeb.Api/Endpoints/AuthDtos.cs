using System.ComponentModel.DataAnnotations;

namespace HuGuWeb.Api.Endpoints;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Password { get; set; } = string.Empty;
}

public sealed class UpdateLanguageRequest
{
    [Required]
    [MaxLength(8)]
    public string Language { get; set; } = string.Empty;
}

public sealed class SelectPropertyRequest
{
    public Guid PropertyId { get; set; }
}

public sealed record AccessiblePropertyResponse(Guid Id, string Name, string TimeZoneId);

public sealed record CurrentUserResponse(
    string Id,
    string? Email,
    string? PreferredLanguage,
    IReadOnlyList<string> Permissions,
    Guid? MembershipId,
    Guid? OrganizationId,
    string? OrganizationName,
    Guid? PropertyId,
    string? ScopeType,
    Guid? EmployeeId,
    IReadOnlyList<AccessiblePropertyResponse> AccessibleProperties,
    bool PropertySelectionRequired);

public sealed record SessionResponse(bool Authenticated, CurrentUserResponse? User);

public sealed record CsrfResponse(string Token);
