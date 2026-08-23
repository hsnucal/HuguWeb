using System.Security.Claims;
using HuGuWeb.RoomOperations.Application;

namespace HuGuWeb.Api.Authorization;

public static class RoomOperationsExposure
{
    public static bool CanReadMaintenance(ClaimsPrincipal user) =>
        user.HasClaim(MaintenancePermissions.ClaimType, MaintenancePermissions.Read)
        || user.HasClaim(MaintenancePermissions.ClaimType, MaintenancePermissions.Manage)
        || user.HasClaim(MaintenancePermissions.ClaimType, MaintenancePermissions.Resolve);

    public static RoomOperationsDetail ForCaller(RoomOperationsDetail detail, bool canReadMaintenance) =>
        canReadMaintenance
            ? detail
            : detail with
            {
                GoverningIssueId = null,
                ActiveTechnicalIssueDescription = null
            };
}
