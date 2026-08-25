using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Api.Authorization;

public sealed class PropertyAccessService(IAuthorizationStore store, IWorkforceStore workforce)
{
    public async Task<IReadOnlyList<Property>> ListAccessiblePropertiesAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var memberships = (await store.ListMembershipsForUserAsync(userId, cancellationToken))
            .Where(item => item.IsActive)
            .ToArray();
        if (memberships.Length == 0)
        {
            return [];
        }

        var properties = new Dictionary<Guid, Property>();
        foreach (var group in memberships.GroupBy(item => item.OrganizationId))
        {
            var organizationProperties = await workforce.ListPropertiesAsync(group.Key, cancellationToken);
            var hasOrganizationWide = group.Any(item => item.PropertyId is null);
            foreach (var property in organizationProperties)
            {
                if (hasOrganizationWide || group.Any(item => item.PropertyId == property.Id))
                {
                    properties[property.Id] = property;
                }
            }
        }

        return properties.Values.OrderBy(item => item.Name).ToArray();
    }

    public async Task<bool> CanAccessPropertyAsync(
        string userId,
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var accessible = await ListAccessiblePropertiesAsync(userId, cancellationToken);
        return accessible.Any(item => item.Id == propertyId);
    }

    public async Task<Guid?> AutoSelectPropertyIdAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var memberships = (await store.ListMembershipsForUserAsync(userId, cancellationToken))
            .Where(item => item.IsActive)
            .ToArray();
        if (memberships.Any(item => item.PropertyId is null))
        {
            return null;
        }

        var accessible = await ListAccessiblePropertiesAsync(userId, cancellationToken);
        return accessible.Count == 1 ? accessible[0].Id : null;
    }
}
