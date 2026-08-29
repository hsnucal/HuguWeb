using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class LeaveTypeAdminUseCase(IWorkforceStore store, IWorkforceClock clock, IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<IReadOnlyList<LeaveTypeDto>>> ListAsync(
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var types = await store.ListLeaveTypesAsync(workplace.Value.Organization.Id, cancellationToken);
        var filtered = activeOnly ? types.Where(item => item.IsActive) : types;
        return WorkforceResult<IReadOnlyList<LeaveTypeDto>>.Success(
            filtered.Select(LeaveTypeDto.From).ToArray());
    }

    public async Task<WorkforceResult<LeaveTypeDto>> CreateAsync(
        CreateLeaveTypeCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var organizationId = workplace.Value.Organization.Id;
        if (!LeaveType.TryNormalizeCode(command.Code, out var normalizedCode, out var codeField, out var codeError))
        {
            return WorkforceError.LeaveValidationField(codeField!, codeError!, "Leave type code is invalid.");
        }

        var existing = await store.FindLeaveTypeByCodeAsync(organizationId, normalizedCode, cancellationToken);
        if (existing is not null)
        {
            return WorkforceError.LeaveTypeCodeConflict();
        }

        if (!LeaveType.TryCreateCustom(
                Guid.CreateVersion7(),
                organizationId,
                command.Code,
                command.Name,
                command.TracksBalance,
                command.ActorUserId,
                clock.UtcNow,
                out var leaveType,
                out var field,
                out var errorCode))
        {
            return WorkforceError.LeaveValidationField(field!, errorCode!, "Leave type is invalid.");
        }

        store.AddLeaveType(leaveType!);
        await store.SaveChangesAsync(cancellationToken);
        return LeaveTypeDto.From(leaveType!);
    }

    public async Task<WorkforceResult<LeaveTypeDto>> UpdateAsync(
        UpdateLeaveTypeCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var leaveType = await store.GetLeaveTypeAsync(command.LeaveTypeId, cancellationToken);
        if (leaveType is null || leaveType.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.LeaveTypeNotFound();
        }

        if (command.Name is not null
            && !leaveType.TryRename(command.Name, command.ActorUserId, clock.UtcNow, out var nameField, out var nameError))
        {
            return WorkforceError.LeaveValidationField(nameField!, nameError!, "Leave type name is invalid.");
        }

        if (command.TracksBalance is { } tracksBalance && tracksBalance != leaveType.TracksBalance)
        {
            var hasUsage = await store.LeaveTypeHasUsageAsync(leaveType.Id, cancellationToken);
            if (!leaveType.TrySetTracksBalance(
                    tracksBalance,
                    hasUsage,
                    command.ActorUserId,
                    clock.UtcNow,
                    out var balanceField,
                    out var balanceError))
            {
                return WorkforceError.LeaveValidationField(
                    balanceField!,
                    balanceError!,
                    "Balance tracking cannot change after the type has historical usage.");
            }
        }

        if (command.IsActive is { } isActive)
        {
            leaveType.SetActive(isActive, command.ActorUserId, clock.UtcNow);
        }

        await store.SaveChangesAsync(cancellationToken);
        return LeaveTypeDto.From(leaveType);
    }
}

public sealed record CreateLeaveTypeCommand(string? Code, string? Name, bool TracksBalance, string ActorUserId);

public sealed record UpdateLeaveTypeCommand(
    Guid LeaveTypeId,
    string? Name,
    bool? TracksBalance,
    bool? IsActive,
    string ActorUserId);

public sealed record LeaveTypeDto(
    Guid Id,
    string Code,
    string Name,
    LeaveTypeSystemKind? SystemKind,
    bool TracksBalance,
    bool IsActive)
{
    public static LeaveTypeDto From(LeaveType leaveType) =>
        new(
            leaveType.Id,
            leaveType.Code,
            leaveType.Name,
            leaveType.SystemKind,
            leaveType.TracksBalance,
            leaveType.IsActive);
}
