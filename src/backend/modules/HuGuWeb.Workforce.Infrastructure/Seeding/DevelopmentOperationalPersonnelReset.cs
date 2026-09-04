using HuGuWeb.Workforce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HuGuWeb.Workforce.Infrastructure.Seeding;

/// <summary>
/// DEVELOPMENT-ONLY: clears operational personnel data while preserving org/property/dept/position structure.
/// Must never run outside Development.
/// </summary>
public static class DevelopmentOperationalPersonnelReset
{
    public static async Task ClearAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        bool isDevelopment,
        CancellationToken cancellationToken = default)
    {
        if (!isDevelopment)
        {
            throw new InvalidOperationException(
                "Development operational personnel reset is blocked outside Development.");
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "MaintenanceIssueHistory";
            DELETE FROM "MaintenanceIssues";
            UPDATE "RoomReadinessHistory" SET "ActorEmployeeId" = NULL WHERE "ActorEmployeeId" IS NOT NULL;
            UPDATE "HousekeepingWorkItems" SET "CompletedByEmployeeId" = NULL WHERE "CompletedByEmployeeId" IS NOT NULL;
            DELETE FROM "HousekeepingWorkItems";
            """,
            cancellationToken);

        await dbContext.LeaveRequestDecisions.ExecuteDeleteAsync(cancellationToken);
        await dbContext.LeaveRequests.ExecuteDeleteAsync(cancellationToken);
        await dbContext.LeaveRecords.ExecuteDeleteAsync(cancellationToken);
        await dbContext.LeaveEntitlements.ExecuteDeleteAsync(cancellationToken);
        await dbContext.ScheduleEntryChanges.ExecuteDeleteAsync(cancellationToken);
        await dbContext.ScheduleEntries.ExecuteDeleteAsync(cancellationToken);
        await dbContext.OfficialEmploymentProfiles.ExecuteDeleteAsync(cancellationToken);
        await dbContext.EmploymentBesSettings.ExecuteDeleteAsync(cancellationToken);
        await dbContext.EmergencyContacts.ExecuteDeleteAsync(cancellationToken);
        await dbContext.EmployeePhotos.ExecuteDeleteAsync(cancellationToken);
        await dbContext.EmployeeHrProfiles.ExecuteDeleteAsync(cancellationToken);
        await dbContext.EmployeePaymentProfiles.ExecuteDeleteAsync(cancellationToken);
        await dbContext.PersonnelProfileChanges.ExecuteDeleteAsync(cancellationToken);
        await dbContext.PersonnelMovements.ExecuteDeleteAsync(cancellationToken);
        await dbContext.WorkforceReportingLines.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Assignments.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Employments.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Employees.ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("Development operational personnel data was cleared.");
    }
}
