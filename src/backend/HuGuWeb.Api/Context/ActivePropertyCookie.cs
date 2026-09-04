namespace HuGuWeb.Api.Context;

/// <summary>
/// Session operational Property. Not stored on <c>ApplicationUser</c>.
/// <see cref="Bind"/> writes the response cookie and the same-request selection
/// so <c>RefreshSignIn</c> can issue claims without re-reading the old request cookie.
/// </summary>
public static class ActivePropertyCookie
{
    public const string Name = "HuGuWeb.ActiveProperty";

    internal const string SelectionItemKey = "HuGuWeb.ActiveProperty.Selection";

    public static Guid? Read(HttpContext httpContext)
    {
        var raw = httpContext.Request.Cookies[Name];
        return Guid.TryParse(raw, out var propertyId) && propertyId != Guid.Empty
            ? propertyId
            : null;
    }

    /// <summary>
    /// Cookie on this request, or the Property bound earlier in the same request
    /// (response cookie is not visible to <see cref="Read"/> yet).
    /// </summary>
    public static Guid? ResolveSelection(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(SelectionItemKey, out var bound) && bound is Guid boundId)
        {
            return boundId == Guid.Empty ? null : boundId;
        }

        return Read(httpContext);
    }

    public static void Write(HttpContext httpContext, Guid propertyId, CookieSecurePolicy securePolicy) =>
        Bind(httpContext, propertyId, securePolicy);

    public static void Clear(HttpContext httpContext, CookieSecurePolicy securePolicy) =>
        Bind(httpContext, propertyId: null, securePolicy);

    public static void Bind(HttpContext httpContext, Guid? propertyId, CookieSecurePolicy securePolicy)
    {
        httpContext.Items[SelectionItemKey] = propertyId ?? Guid.Empty;
        if (propertyId is Guid id && id != Guid.Empty)
        {
            httpContext.Response.Cookies.Append(Name, id.ToString(), CreateOptions(httpContext, securePolicy));
            return;
        }

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
