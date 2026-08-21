using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

internal static class WorkplaceGuard
{
    public static async Task<WorkforceResult<ConfiguredWorkplace>> GetAsync(
        IWorkforceStore store,
        IWorkplaceContext workplace,
        CancellationToken cancellationToken)
    {
        if (!workplace.IsConfigured)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        var organization = await store.GetOrganizationAsync(workplace.OrganizationId, cancellationToken);
        var property = await store.GetPropertyAsync(workplace.PropertyId, cancellationToken);
        if (organization is null || property is null || property.OrganizationId != organization.Id)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        return new ConfiguredWorkplace(organization, property);
    }
}

internal sealed record ConfiguredWorkplace(Organization Organization, Property Property);
