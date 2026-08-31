using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// Idempotently ensures an organization owns the ten default leave types plus optional custom seeds.
/// Safe to call for existing and new organizations; inserts only missing system codes and never
/// revives a hotel-deactivated system type or overwrites Name/TracksBalance for an existing code.
/// Applies missing product <see cref="LeaveType.DefaultRequestAmount"/> values when unset.
/// Invoked from organization initialization/seeding — never as a side effect of a GET.
/// </summary>
public sealed class EnsureDefaultLeaveTypesUseCase(IWorkforceStore store, IWorkforceClock clock)
{
    public const string SeedActorUserId = "system";

    public async Task<int> ExecuteAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var existing = await store.ListLeaveTypesAsync(organizationId, cancellationToken);
        var createdAtUtc = clock.UtcNow;
        var added = 0;
        var changed = false;
        var presentCodes = existing
            .Select(item => LeaveType.NormalizeCodeForLookup(item.Code))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var leaveType in existing)
        {
            var definition = LeaveTypeDefaults.All.FirstOrDefault(item =>
                item.Code == LeaveType.NormalizeCodeForLookup(leaveType.Code));
            if (definition?.DefaultRequestAmount is { } productDefault
                && leaveType.DefaultRequestAmount is null
                && leaveType.TrySetDefaultRequestAmount(
                    productDefault,
                    SeedActorUserId,
                    createdAtUtc,
                    out _,
                    out _))
            {
                changed = true;
            }
        }

        foreach (var definition in LeaveTypeDefaults.Missing(presentCodes))
        {
            var leaveType = LeaveType.CreateSystemDefault(
                Guid.CreateVersion7(),
                organizationId,
                definition.Code,
                definition.DefaultName,
                definition.SystemKind,
                definition.TracksBalance,
                SeedActorUserId,
                createdAtUtc,
                definition.DefaultRequestAmount);
            store.AddLeaveType(leaveType);
            presentCodes.Add(definition.Code);
            added += 1;
            changed = true;
        }

        if (!presentCodes.Contains(LeaveTypeDefaults.OptionalCustom.BirthdayCode))
        {
            if (LeaveType.TryCreateCustom(
                    Guid.CreateVersion7(),
                    organizationId,
                    LeaveTypeDefaults.OptionalCustom.BirthdayCode,
                    LeaveTypeDefaults.OptionalCustom.BirthdayDefaultName,
                    tracksBalance: false,
                    SeedActorUserId,
                    createdAtUtc,
                    out var birthday,
                    out _,
                    out _,
                    LeaveTypeDefaults.OptionalCustom.BirthdayDefaultRequestAmount))
            {
                store.AddLeaveType(birthday!);
                added += 1;
                changed = true;
            }
        }
        else
        {
            var birthday = existing.FirstOrDefault(item =>
                LeaveType.NormalizeCodeForLookup(item.Code) == LeaveTypeDefaults.OptionalCustom.BirthdayCode);
            if (birthday is not null
                && birthday.DefaultRequestAmount is null
                && birthday.TrySetDefaultRequestAmount(
                    LeaveTypeDefaults.OptionalCustom.BirthdayDefaultRequestAmount,
                    SeedActorUserId,
                    createdAtUtc,
                    out _,
                    out _))
            {
                changed = true;
            }
        }

        if (changed)
        {
            await store.SaveChangesAsync(cancellationToken);
        }

        return added;
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
