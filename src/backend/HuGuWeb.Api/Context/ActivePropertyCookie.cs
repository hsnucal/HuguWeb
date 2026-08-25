namespace HuGuWeb.Api.Context;

public static class ActivePropertyCookie
{
    public const string Name = "HuGuWeb.ActiveProperty";

    public static Guid? Read(HttpContext httpContext)
    {
        var raw = httpContext.Request.Cookies[Name];
        return Guid.TryParse(raw, out var propertyId) && propertyId != Guid.Empty
            ? propertyId
            : null;
    }

    public static void Write(HttpContext httpContext, Guid propertyId, CookieSecurePolicy securePolicy)
    {
        httpContext.Response.Cookies.Append(Name, propertyId.ToString(), CreateOptions(httpContext, securePolicy));
    }

    public static void Clear(HttpContext httpContext, CookieSecurePolicy securePolicy)
    {
        httpContext.Response.Cookies.Delete(Name, CreateOptions(httpContext, securePolicy));
    }

    private static CookieOptions CreateOptions(HttpContext httpContext, CookieSecurePolicy securePolicy) =>
        new()
        {
            HttpOnly = true,
            Secure = securePolicy switch
            {
                CookieSecurePolicy.Always => true,
                CookieSecurePolicy.SameAsRequest => httpContext.Request.IsHttps,
                _ => false
            },
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true
        };
}
