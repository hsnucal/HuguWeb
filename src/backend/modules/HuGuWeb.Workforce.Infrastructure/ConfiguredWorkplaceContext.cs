using HuGuWeb.Workforce.Application;
using Microsoft.Extensions.Options;

namespace HuGuWeb.Workforce.Infrastructure;

public sealed class WorkplaceOptions
{
    public const string SectionName = "Workforce";

    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
}

public sealed class ConfiguredWorkplaceContext(IOptions<WorkplaceOptions> options) : IWorkplaceContext
{
    public Guid OrganizationId => options.Value.OrganizationId;

    public Guid PropertyId => options.Value.PropertyId;

    public bool HasOrganization => OrganizationId != Guid.Empty;

    public bool HasProperty => PropertyId != Guid.Empty;

    public bool IsConfigured => HasOrganization && HasProperty;
}
