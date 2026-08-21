using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Identity;
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

        group.MapPatch("/preferences/language", UpdatePreferredLanguage)
            .WithName("UpdatePreferredLanguage")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static CsrfResponse GetCsrfToken(IAntiforgery antiforgery, HttpContext httpContext)
    {
        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        return new CsrfResponse(tokens.RequestToken!);
    }

    private static async Task<SessionResponse> GetSession(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return new SessionResponse(false, null);
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return new SessionResponse(false, null);
        }

        return new SessionResponse(true, await ToUserResponseAsync(user, userManager, principal));
    }

    private static async Task<IResult> GetCurrentUser(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(await ToUserResponseAsync(user, userManager, principal));
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
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
        return Results.Ok(await ToUserResponseAsync(user, userManager, principal: null));
    }

    private static async Task<IResult> Logout(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> UpdatePreferredLanguage(
        [FromBody] UpdateLanguageRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("HuGuWeb.Api.Authentication");

        if (!SupportedLanguages.TryNormalize(request.Language, out var language))
        {
            return Results.Problem(
                title: "The request is invalid.",
                detail: "Language must be one of: tr, en, ru.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!string.Equals(user.PreferredLanguage, language, StringComparison.Ordinal))
        {
            user.PreferredLanguage = language;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                logger.LogWarning("Language preference could not be saved for {UserId}.", user.Id);
                return Results.Problem(
                    title: "Language preference could not be saved.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        return Results.Ok(await ToUserResponseAsync(user, userManager, principal));
    }

    private static async Task<CurrentUserResponse> ToUserResponseAsync(
        ApplicationUser user,
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal? principal)
    {
        IReadOnlyList<string> sessionPermissions = [];
        if (principal?.Identity?.IsAuthenticated == true)
        {
            sessionPermissions = principal.Claims
                .Where(claim => claim.Type == WorkforcePermissions.ClaimType)
                .Select(claim => claim.Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        var permissions = sessionPermissions.Count > 0
            ? sessionPermissions
            : (await userManager.GetClaimsAsync(user))
                .Where(claim => claim.Type == WorkforcePermissions.ClaimType)
                .Select(claim => claim.Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        return new CurrentUserResponse(user.Id, user.Email, user.PreferredLanguage, permissions);
    }

    private static IResult AuthenticationFailed() =>
        Results.Problem(
            title: "Authentication failed.",
            detail: "Invalid email or password.",
            statusCode: StatusCodes.Status401Unauthorized);
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
