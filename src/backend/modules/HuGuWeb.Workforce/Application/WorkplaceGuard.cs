using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

internal static class WorkplaceGuard
{
    public static async Task<WorkforceResult<ConfiguredOrganization>> GetOrganizationAsync(
        IWorkforceStore store,
        IWorkplaceContext workplace,
        CancellationToken cancellationToken)
    {
        if (!workplace.HasOrganization)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        var organization = await store.GetOrganizationAsync(workplace.OrganizationId, cancellationToken);
        if (organization is null)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        return new ConfiguredOrganization(organization);
    }

    public static async Task<WorkforceResult<ConfiguredWorkplace>> GetAsync(
        IWorkforceStore store,
        IWorkplaceContext workplace,
        CancellationToken cancellationToken)
    {
        var organization = await GetOrganizationAsync(store, workplace, cancellationToken);
        if (!organization.IsSuccess)
        {
            return organization.Error!;
        }

        if (!workplace.HasProperty)
        {
            return WorkforceError.PropertyContextRequired();
        }

        var property = await store.GetPropertyAsync(workplace.PropertyId, cancellationToken);
        if (property is null || property.OrganizationId != organization.Value!.Organization.Id)
        {
            return WorkforceError.PropertyContextRequired();
        }

        return new ConfiguredWorkplace(organization.Value.Organization, property);
    }
}

internal sealed record ConfiguredOrganization(Organization Organization);

internal sealed record ConfiguredWorkplace(Organization Organization, Property Property);
