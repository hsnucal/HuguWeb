using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Context;
using HuGuWeb.Api.Identity;
using HuGuWeb.Workforce.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HuGuWeb.Api.Extensions;

public static class SecurityExtensions
{
    public const string CorsPolicyName = "HuGuWeb";

    public static WebApplicationBuilder AddHuGuWebSecurity(this WebApplicationBuilder builder)
    {
        var environment = builder.Environment;
        var cookieSecurePolicy = environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        builder.Services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<AppIdentityDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IAuthorizationStore, EfAuthorizationStore>();
        builder.Services.AddScoped<AccessSnapshotService>();
        builder.Services.AddScoped<SecurityStampRefreshService>();
        builder.Services.AddScoped<LastAdministratorProtectionService>();
        builder.Services.AddScoped<PropertyAccessService>();
        builder.Services.AddScoped<AuthorizationAdministrationService>();
        builder.Services.AddScoped<EmployeeTenantGuard>();
        builder.Services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();
        builder.Services.AddScoped<IRequestActorContext, RequestActorContext>();
        builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, HuGuUserClaimsPrincipalFactory>();
        builder.Services.Replace(ServiceDescriptor.Scoped<IWorkplaceContext, RequestWorkplaceContext>());
        builder.Services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.FromMinutes(1);
        });

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = environment.IsDevelopment()
                ? "HuGuWeb.Auth"
                : "__Host-HuGuWeb.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Path = "/";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Events.OnRedirectToLogin = context =>
                WriteApiAuthenticationProblem(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "Authentication required.",
                    "authentication-required");
            options.Events.OnRedirectToAccessDenied = context =>
                WriteApiAuthenticationProblem(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    "Access denied.",
                    "permission-denied");
        });

        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "HuGuWeb.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Path = "/";
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.Authenticated,
                policy => policy.RequireAuthenticatedUser());

            options.AddPolicy(
                AuthorizationPolicies.WorkforceRead,
                policy => policy.RequireAssertion(context =>
                    context.User.HasClaim(WorkforcePermissions.ClaimType, WorkforcePermissions.Read)
                    || context.User.HasClaim(WorkforcePermissions.ClaimType, WorkforcePermissions.Manage)));

            options.AddPolicy(
                AuthorizationPolicies.WorkforceManage,
                policy => policy.RequireClaim(WorkforcePermissions.ClaimType, WorkforcePermissions.Manage));

            options.AddPolicy(
                AuthorizationPolicies.RoomOperationsRead,
                policy => policy.RequireAssertion(context =>
                    context.User.HasClaim(RoomOperationsPermissions.ClaimType, RoomOperationsPermissions.Read)
                    || context.User.HasClaim(RoomOperationsPermissions.ClaimType, RoomOperationsPermissions.Manage)
                    || context.User.HasClaim(RoomOperationsPermissions.ClaimType, RoomOperationsPermissions.Inspect)));

            options.AddPolicy(
                AuthorizationPolicies.RoomOperationsManage,
                policy => policy.RequireClaim(RoomOperationsPermissions.ClaimType, RoomOperationsPermissions.Manage));

            options.AddPolicy(
                AuthorizationPolicies.RoomOperationsInspect,
                policy => policy.RequireClaim(RoomOperationsPermissions.ClaimType, RoomOperationsPermissions.Inspect));

            options.AddPolicy(
                AuthorizationPolicies.MaintenanceRead,
                policy => policy.RequireAssertion(context =>
                    context.User.HasClaim(MaintenancePermissions.ClaimType, MaintenancePermissions.Read)
                    || context.User.HasClaim(MaintenancePermissions.ClaimType, MaintenancePermissions.Manage)
                    || context.User.HasClaim(MaintenancePermissions.ClaimType, MaintenancePermissions.Resolve)));

            options.AddPolicy(
                AuthorizationPolicies.MaintenanceManage,
                policy => policy.RequireClaim(MaintenancePermissions.ClaimType, MaintenancePermissions.Manage));

            options.AddPolicy(
                AuthorizationPolicies.MaintenanceResolve,
                policy => policy.RequireClaim(MaintenancePermissions.ClaimType, MaintenancePermissions.Resolve));

            options.AddPolicy(
                AuthorizationPolicies.HrEmployeeRead,
                policy => policy.RequireAssertion(context =>
                    context.User.HasClaim(HrEmployeePermissions.ClaimType, HrEmployeePermissions.Read)
                    || context.User.HasClaim(HrEmployeePermissions.ClaimType, HrEmployeePermissions.Manage)));

            options.AddPolicy(
                AuthorizationPolicies.HrEmployeeManage,
                policy => policy.RequireClaim(HrEmployeePermissions.ClaimType, HrEmployeePermissions.Manage));

            options.AddPolicy(
                AuthorizationPolicies.HrEmployeeHire,
                policy => policy.RequireAssertion(context =>
                    context.User.HasClaim(HrEmployeePermissions.ClaimType, HrEmployeePermissions.Manage)
                    && context.User.HasClaim(WorkforcePermissions.ClaimType, WorkforcePermissions.Manage)));

            options.AddPolicy(
                AuthorizationPolicies.HrLeaveRead,
                policy => policy.RequireAssertion(context =>
                    context.User.HasClaim(HrLeavePermissions.ClaimType, HrLeavePermissions.Read)
                    || context.User.HasClaim(HrLeavePermissions.ClaimType, HrLeavePermissions.Manage)));

            options.AddPolicy(
                AuthorizationPolicies.HrLeaveManage,
                policy => policy.RequireClaim(HrLeavePermissions.ClaimType, HrLeavePermissions.Manage));

            options.AddPolicy(
                AuthorizationPolicies.AuthorizationUsersManage,
                policy => policy.RequireClaim(
                    AuthorizationPermissions.ClaimType,
                    AuthorizationPermissions.UsersManage));

            options.AddPolicy(
                AuthorizationPolicies.AuthorizationRolesManage,
                policy => policy.RequireClaim(
                    AuthorizationPermissions.ClaimType,
                    AuthorizationPermissions.RolesManage));
        });

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                if (allowedOrigins.Length == 0)
                {
                    policy.SetIsOriginAllowed(_ => false);
                }
                else
                {
                    policy.WithOrigins(allowedOrigins).AllowCredentials();
                }

                policy
                    .WithHeaders("Content-Type", "X-XSRF-TOKEN", "X-Correlation-ID", "Accept-Language")
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS");
            });
        });

        return builder;
    }

    private static async Task WriteApiAuthenticationProblem(
        HttpContext httpContext,
        int statusCode,
        string title,
        string code)
    {
        httpContext.Response.StatusCode = statusCode;

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Extensions = { ["code"] = code }
            }
        });
    }
}
