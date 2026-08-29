using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// Idempotently ensures an organization owns the ten default leave types. Safe to call for existing
/// and new organizations; inserts only missing system codes and never revives a hotel-deactivated
/// system type or overwrites Name/TracksBalance for an existing code. This is invoked from
/// organization initialization/seeding — never as a side effect of a GET.
/// </summary>
public sealed class EnsureDefaultLeaveTypesUseCase(IWorkforceStore store, IWorkforceClock clock)
{
    public const string SeedActorUserId = "system";

    public async Task<int> ExecuteAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var existing = await store.ListLeaveTypesAsync(organizationId, cancellationToken);
        var missing = LeaveTypeDefaults.Missing(existing.Select(item => item.Code));
        if (missing.Count == 0)
        {
            return 0;
        }

        var createdAtUtc = clock.UtcNow;
        foreach (var definition in missing)
        {
            var leaveType = LeaveType.CreateSystemDefault(
                Guid.CreateVersion7(),
                organizationId,
                definition.Code,
                definition.DefaultName,
                definition.SystemKind,
                definition.TracksBalance,
                SeedActorUserId,
                createdAtUtc);
            store.AddLeaveType(leaveType);
        }

        await store.SaveChangesAsync(cancellationToken);
        return missing.Count;
    }

    public async Task<int> ExecuteForAllOrganizationsAsync(CancellationToken cancellationToken)
    {
        var organizationIds = await store.ListOrganizationIdsAsync(cancellationToken);
        var added = 0;
        foreach (var organizationId in organizationIds)
        {
            added += await ExecuteAsync(organizationId, cancellationToken);
        }

        return added;
    }
}
