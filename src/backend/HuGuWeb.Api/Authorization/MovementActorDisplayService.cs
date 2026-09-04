using HuGuWeb.Api.Identity;
using HuGuWeb.Workforce.Application;
using Microsoft.AspNetCore.Identity;

namespace HuGuWeb.Api.Authorization;

public sealed class MovementActorDisplayService(
    UserManager<ApplicationUser> userManager,
    IAuthorizationStore store,
    IWorkforceStore workforce)
{
    public async Task<IReadOnlyList<PersonnelMovementListItemDto>> EnrichListAsync(
        IReadOnlyList<PersonnelMovementListItemDto> items,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveManyAsync(
            items.Select(item => item.CreatedByUserId),
            cancellationToken);
        return items
            .Select(item => MovementActorNaming.WithActor(item, Lookup(resolved, item.CreatedByUserId)))
            .ToArray();
    }

    public async Task<PersonnelMovementDetailDto> EnrichDetailAsync(
        PersonnelMovementDetailDto item,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveManyAsync(
            [item.CreatedByUserId, item.CancelledByUserId],
            cancellationToken);
        return MovementActorNaming.WithActors(
            item,
            Lookup(resolved, item.CreatedByUserId),
            item.CancelledByUserId is null ? null : Lookup(resolved, item.CancelledByUserId));
    }

    private async Task<IReadOnlyDictionary<string, MovementActorDto>> ResolveManyAsync(
        IEnumerable<string?> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var result = new Dictionary<string, MovementActorDto>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            result[id] = await ResolveOneAsync(id, cancellationToken);
        }

        return result;
    }

    private async Task<MovementActorDto> ResolveOneAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await FindApplicationUserAsync(userId);
        var lookupId = user?.Id ?? userId;
        var link = await store.FindLinkByUserAsync(lookupId, cancellationToken);
        string? personName = null;
        if (link is not null)
        {
            var employee = await workforce.GetEmployeeAsync(link.EmployeeId, cancellationToken);
            if (employee is not null)
            {
                personName = $"{employee.GivenName} {employee.FamilyName}".Trim();
            }
        }

        return MovementActorNaming.Resolve(userId, personName, user?.UserName, user?.Email);
    }

    private async Task<ApplicationUser?> FindApplicationUserAsync(string userId)
    {
        var byId = await userManager.FindByIdAsync(userId);
        if (byId is not null)
        {
            return byId;
        }

        if (userId.Contains('@', StringComparison.Ordinal))
        {
            var byEmail = await userManager.FindByEmailAsync(userId);
            if (byEmail is not null)
            {
                return byEmail;
            }
        }

        return await userManager.FindByNameAsync(userId);
    }

    private static MovementActorDto Lookup(
        IReadOnlyDictionary<string, MovementActorDto> resolved,
        string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return MovementActorNaming.Unresolved(null);
        }

        return resolved.TryGetValue(userId.Trim(), out var actor)
            ? actor
            : MovementActorNaming.Unresolved(userId);
    }
}
