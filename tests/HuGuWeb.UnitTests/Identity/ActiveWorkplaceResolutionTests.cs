using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace HuGuWeb.UnitTests.Identity;

public class ActiveWorkplaceResolutionTests
{
    [Fact]
    public void Bind_MakesSelectionVisibleBeforeRequestCookieExists()
    {
        var http = new DefaultHttpContext();
        var propertyId = Guid.CreateVersion7();

        Assert.Null(ActivePropertyCookie.Read(http));
        ActivePropertyCookie.Bind(http, propertyId, CookieSecurePolicy.None);
        Assert.Equal(propertyId, ActivePropertyCookie.ResolveSelection(http));
        Assert.Null(ActivePropertyCookie.Read(http));
    }

    [Fact]
    public void OrgWideUser_SelectedCookie_IsOperationalProperty()
    {
        var organizationId = Guid.CreateVersion7();
        var propertyA = Guid.CreateVersion7();
        var http = Authenticated(organizationId, propertyId: null, AuthorizationScopeType.Organization);
        SetRequestCookie(http, propertyA);

        var workplace = Workplace(http);
        var tenant = Tenant(http);

        Assert.Equal(organizationId, workplace.OrganizationId);
        Assert.Equal(propertyA, workplace.PropertyId);
        Assert.True(workplace.HasProperty);
        Assert.Equal(propertyA, tenant.PropertyId);
        Assert.Equal(propertyA, ActiveWorkplaceResolution.ResolvePropertyId(http));
    }

    [Fact]
    public void OrgWideUser_WithoutSelection_HasNoProperty()
    {
        var organizationId = Guid.CreateVersion7();
        var http = Authenticated(organizationId, propertyId: null, AuthorizationScopeType.Organization);
        var workplace = Workplace(http);

        Assert.True(workplace.HasOrganization);
        Assert.False(workplace.HasProperty);
        Assert.Equal(Guid.Empty, workplace.PropertyId);
        Assert.Null(Tenant(http).PropertyId);
    }

    [Fact]
    public void OrgWideUser_SwitchCookie_ResolvesNewProperty()
    {
        var organizationId = Guid.CreateVersion7();
        var propertyA = Guid.CreateVersion7();
        var propertyB = Guid.CreateVersion7();
        var http = Authenticated(organizationId, propertyA, AuthorizationScopeType.Organization);
        SetRequestCookie(http, propertyB);

        Assert.Equal(propertyB, Workplace(http).PropertyId);
        Assert.Equal(propertyB, Tenant(http).PropertyId);
    }

    [Fact]
    public void PropertyScopedUser_IgnoresCookieForOtherProperty()
    {
        var organizationId = Guid.CreateVersion7();
        var membershipProperty = Guid.CreateVersion7();
        var otherProperty = Guid.CreateVersion7();
        var http = Authenticated(organizationId, membershipProperty, AuthorizationScopeType.Property);
        SetRequestCookie(http, otherProperty);

        Assert.Equal(membershipProperty, Workplace(http).PropertyId);
        Assert.Equal(membershipProperty, Tenant(http).PropertyId);
    }

    [Fact]
    public void BindClear_RemovesSameRequestSelection()
    {
        var http = new DefaultHttpContext();
        var propertyId = Guid.CreateVersion7();
        ActivePropertyCookie.Bind(http, propertyId, CookieSecurePolicy.None);
        ActivePropertyCookie.Bind(http, propertyId: null, CookieSecurePolicy.None);
        Assert.Null(ActivePropertyCookie.ResolveSelection(http));
    }

    [Fact]
    public void OrgWide_StaleTicketProperty_YieldsToCookie()
    {
        var organizationId = Guid.CreateVersion7();
        var ticketProperty = Guid.CreateVersion7();
        var cookieProperty = Guid.CreateVersion7();
        var http = Authenticated(organizationId, ticketProperty, AuthorizationScopeType.Organization);
        SetRequestCookie(http, cookieProperty);
        Assert.Equal(cookieProperty, ActiveWorkplaceResolution.ResolvePropertyId(http));
    }

    private static RequestWorkplaceContext Workplace(HttpContext http) =>
        new(new StaticAccessor(http), new HostEnv());

    private static CurrentTenantContext Tenant(HttpContext http) =>
        new(new StaticAccessor(http));

    private static HttpContext Authenticated(
        Guid organizationId,
        Guid? propertyId,
        AuthorizationScopeType scope)
    {
        var identity = new ClaimsIdentity("Test");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "user-1"));
        identity.AddClaim(new Claim(AuthorizationClaims.OrganizationId, organizationId.ToString()));
        identity.AddClaim(new Claim(AuthorizationClaims.MembershipId, Guid.CreateVersion7().ToString()));
        identity.AddClaim(new Claim(AuthorizationClaims.ScopeType, scope.ToString()));
        if (propertyId is Guid id)
        {
            identity.AddClaim(new Claim(AuthorizationClaims.PropertyId, id.ToString()));
        }

        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    private static void SetRequestCookie(HttpContext http, Guid propertyId)
    {
        http.Features.Set<IRequestCookiesFeature>(
            new RequestCookiesFeature(new DictionaryCookieCollection(ActivePropertyCookie.Name, propertyId.ToString())));
    }

    private sealed class DictionaryCookieCollection(string name, string value) : IRequestCookieCollection
    {
        public string? this[string key] => string.Equals(key, name, StringComparison.Ordinal) ? value : null;
        public int Count => 1;
        public ICollection<string> Keys => [name];
        public bool ContainsKey(string key) => string.Equals(key, name, StringComparison.Ordinal);
        public bool TryGetValue(string key, out string value)
        {
            if (ContainsKey(key))
            {
                value = this[key]!;
                return true;
            }

            value = string.Empty;
            return false;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            yield return new KeyValuePair<string, string>(name, value);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class StaticAccessor(HttpContext http) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = http;
    }

    private sealed class HostEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
