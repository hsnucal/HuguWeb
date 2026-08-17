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

public sealed record CurrentUserResponse(string Id, string? Email);

public sealed record SessionResponse(bool Authenticated, CurrentUserResponse? User);

public sealed record CsrfResponse(string Token);
