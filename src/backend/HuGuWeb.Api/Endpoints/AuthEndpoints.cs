using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication");

        group.MapGet("/csrf", GetCsrfToken)
            .WithName("GetCsrfToken")
            .AllowAnonymous();

        group.MapGet("/session", GetSession)
            .WithName("GetSession")
            .AllowAnonymous();

        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .RequireAuthorization(AuthorizationPolicies.Authenticated);

        group.MapPost("/login", Login)
            .WithName("Login")
            .AllowAnonymous()
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static CsrfResponse GetCsrfToken(IAntiforgery antiforgery, HttpContext httpContext)
    {
        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        return new CsrfResponse(tokens.RequestToken!);
    }

    private static SessionResponse GetSession(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return new SessionResponse(false, null);
        }

        return new SessionResponse(true, ToUserResponse(principal));
    }

    private static CurrentUserResponse GetCurrentUser(ClaimsPrincipal principal) =>
        ToUserResponse(principal);

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("HuGuWeb.Api.Authentication");

        if (!MiniValidator.TryValidate(request, out var validationProblem))
        {
            return validationProblem;
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            logger.LogInformation("Sign-in failed for an unknown or invalid account.");
            return AuthenticationFailed();
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            logger.LogInformation("Sign-in failed for {UserId}.", user.Id);
            return AuthenticationFailed();
        }

        logger.LogInformation("Sign-in succeeded for {UserId}.", user.Id);
        return Results.Ok(new CurrentUserResponse(user.Id, user.Email));
    }

    private static async Task<IResult> Logout(SignInManager<IdentityUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static CurrentUserResponse ToUserResponse(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.Identity?.Name;
        return new CurrentUserResponse(id, email);
    }

    private static IResult AuthenticationFailed() =>
        Results.Problem(
            title: "Authentication failed.",
            detail: "Invalid email or password.",
            statusCode: StatusCodes.Status401Unauthorized);
}

internal sealed class ValidateAntiforgeryFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        await antiforgery.ValidateRequestAsync(context.HttpContext);
        return await next(context);
    }
}

internal static class MiniValidator
{
    public static bool TryValidate(LoginRequest request, out IResult problem)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            problem = Results.Problem(
                title: "The request is invalid.",
                detail: "Email and password are required.",
                statusCode: StatusCodes.Status400BadRequest);
            return false;
        }

        problem = Results.Empty;
        return true;
    }
}
