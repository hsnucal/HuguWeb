using Microsoft.AspNetCore.Identity;

namespace HuGuWeb.Api.Identity;

public sealed class ApplicationUser : IdentityUser
{
    /// <summary>
    /// UI language preference as a stable language code: tr, en, or ru.
    /// Null means the user has not saved a preference yet.
    /// </summary>
    public string? PreferredLanguage { get; set; }
}
