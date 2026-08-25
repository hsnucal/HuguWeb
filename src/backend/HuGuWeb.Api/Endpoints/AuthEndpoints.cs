using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Context;
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

        group.MapPut("/property", SelectProperty)
            .WithName("SelectActiveProperty")
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
        HttpContext httpContext,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AccessSnapshotService accessSnapshot,
        PropertyAccessService propertyAccess)
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

        var response = await ToUserResponseAsync(httpContext, user, accessSnapshot, propertyAccess, signInManager);
        return new SessionResponse(true, response);
    }

    private static async Task<IResult> GetCurrentUser(
        HttpContext httpContext,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AccessSnapshotService accessSnapshot,
        PropertyAccessService propertyAccess)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: new Dictionary<string, object?> { ["code"] = "authentication-required" });
        }

        return Results.Ok(await ToUserResponseAsync(httpContext, user, accessSnapshot, propertyAccess, signInManager));
    }

    private static async Task<IResult> Login(
        HttpContext httpContext,
        [FromBody] LoginRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AccessSnapshotService accessSnapshot,
        PropertyAccessService propertyAccess,
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
        var autoProperty = await propertyAccess.AutoSelectPropertyIdAsync(user.Id, CancellationToken.None);
        WriteActiveProperty(httpContext, autoProperty);
        await signInManager.RefreshSignInAsync(user);
        return Results.Ok(await ToUserResponseAsync(httpContext, user, accessSnapshot, propertyAccess, signInManager));
    }

    private static async Task<IResult> Logout(
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager)
    {
        ActivePropertyCookie.Clear(httpContext, CookiePolicy(httpContext));
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> UpdatePreferredLanguage(
        HttpContext httpContext,
        [FromBody] UpdateLanguageRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AccessSnapshotService accessSnapshot,
        PropertyAccessService propertyAccess,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("HuGuWeb.Api.Authentication");

        if (!SupportedLanguages.TryNormalize(request.Language, out var language))
        {
            return Results.Problem(
                title: "The request is invalid.",
                detail: "Language must be one of: tr, en, ru.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = "invalid-language" });
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: new Dictionary<string, object?> { ["code"] = "authentication-required" });
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
                    statusCode: StatusCodes.Status500InternalServerError,
                    extensions: new Dictionary<string, object?> { ["code"] = "preference-save-failed" });
            }
        }

        return Results.Ok(await ToUserResponseAsync(httpContext, user, accessSnapshot, propertyAccess, signInManager));
    }

    private static async Task<IResult> SelectProperty(
        HttpContext httpContext,
        [FromBody] SelectPropertyRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AccessSnapshotService accessSnapshot,
        PropertyAccessService propertyAccess)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: new Dictionary<string, object?> { ["code"] = "authentication-required" });
        }

        if (!await propertyAccess.CanAccessPropertyAsync(user.Id, request.PropertyId, CancellationToken.None))
        {
            return AuthorizationError.PropertyNotAccessible().ToHttp();
        }

        WriteActiveProperty(httpContext, request.PropertyId);
        await signInManager.RefreshSignInAsync(user);
        return Results.Ok(await ToUserResponseAsync(httpContext, user, accessSnapshot, propertyAccess, signInManager));
    }

    private static async Task<CurrentUserResponse> ToUserResponseAsync(
        HttpContext httpContext,
        ApplicationUser user,
        AccessSnapshotService accessSnapshot,
        PropertyAccessService propertyAccess,
        SignInManager<ApplicationUser> signInManager)
    {
        var selected = ActivePropertyCookie.Read(httpContext);
        var accessible = await propertyAccess.ListAccessiblePropertiesAsync(user.Id, CancellationToken.None);
        if (selected is Guid selectedId && accessible.All(item => item.Id != selectedId))
        {
            ActivePropertyCookie.Clear(httpContext, CookiePolicy(httpContext));
            selected = null;
            await signInManager.RefreshSignInAsync(user);
        }

        var snapshot = await accessSnapshot.GetSnapshotAsync(user.Id, selected, CancellationToken.None);
        var cookiePermissions = httpContext.User.FindAll(AuthorizationClaims.Permission)
            .Select(claim => claim.Value)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();
        if (!cookiePermissions.SequenceEqual(snapshot.Permissions, StringComparer.Ordinal)
            || snapshot.PropertyId != selected && snapshot.PropertyId != ReadClaimProperty(httpContext.User))
        {
            await signInManager.RefreshSignInAsync(user);
        }

        var propertySelectionRequired = snapshot.ScopeType == AuthorizationScopeType.Organization
            && snapshot.PropertyId is null
            && accessible.Count > 0;

        if (accessible.Count > 1 && snapshot.PropertyId is null)
        {
            propertySelectionRequired = true;
        }

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.PreferredLanguage,
            snapshot.Permissions,
            snapshot.MembershipId,
            snapshot.OrganizationId,
            snapshot.PropertyId,
            snapshot.ScopeType?.ToString(),
            snapshot.EmployeeId,
            accessible.Select(item => new AccessiblePropertyResponse(item.Id, item.Name, item.TimeZoneId)).ToArray(),
            propertySelectionRequired);
    }

    private static Guid? ReadClaimProperty(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(AuthorizationClaims.PropertyId), out var value) ? value : null;

    private static void WriteActiveProperty(HttpContext httpContext, Guid? propertyId)
    {
        var policy = CookiePolicy(httpContext);
        if (propertyId is Guid id)
        {
            ActivePropertyCookie.Write(httpContext, id, policy);
        }
        else
        {
            ActivePropertyCookie.Clear(httpContext, policy);
        }
    }

    private static CookieSecurePolicy CookiePolicy(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

    private static IResult AuthenticationFailed() =>
        Results.Problem(
            title: "Authentication failed.",
            detail: "Invalid email or password.",
            statusCode: StatusCodes.Status401Unauthorized,
            extensions: new Dictionary<string, object?> { ["code"] = "authentication-failed" });
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
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = "invalid-request" });
            return false;
        }

        problem = Results.Empty;
        return true;
    }
}
