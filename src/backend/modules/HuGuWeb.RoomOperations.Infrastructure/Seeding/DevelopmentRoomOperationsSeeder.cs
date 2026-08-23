using HuGuWeb.RoomOperations.Domain;
using HuGuWeb.RoomOperations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HuGuWeb.RoomOperations.Infrastructure.Seeding;

public static class DevelopmentRoomOperationsSeeder
{
    public static readonly Guid Room101Id = Guid.Parse("a1e1c0de-0002-4000-8000-000000000101");
    public static readonly Guid Room102Id = Guid.Parse("a1e1c0de-0002-4000-8000-000000000102");
    public static readonly Guid Room103Id = Guid.Parse("a1e1c0de-0002-4000-8000-000000000103");
    public static readonly Guid Room201Id = Guid.Parse("a1e1c0de-0002-4000-8000-000000000201");
    public static readonly Guid Room202Id = Guid.Parse("a1e1c0de-0002-4000-8000-000000000202");

    private static readonly (Guid Id, string Number)[] Rooms =
    [
        (Room101Id, "101"),
        (Room102Id, "102"),
        (Room103Id, "103"),
        (Room201Id, "201"),
        (Room202Id, "202")
    ];

    public static async Task TrySeedAsync(
        RoomOperationsDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Same development Property as Workforce seed. Not a cross-module type dependency.
            var propertyId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000002");
            var seeded = 0;

            foreach (var (id, number) in Rooms)
            {
                if (await dbContext.Rooms.AnyAsync(room => room.Id == id, cancellationToken))
                {
                    continue;
                }

                var cycleId = Guid.CreateVersion7();
                if (!Room.TryCreate(id, propertyId, number, cycleId, out var room, out var error) || room is null)
                {
                    throw new InvalidOperationException($"Development room seed is invalid: {error}");
                }

                dbContext.Rooms.Add(room);
                dbContext.RoomReadinessHistory.Add(RoomReadinessHistoryEntry.Record(
                    Guid.CreateVersion7(),
                    room.Id,
                    cycleId,
                    RoomReadiness.Dirty,
                    ReadinessChangeCause.Seeded,
                    DateTimeOffset.UtcNow));
                seeded++;
            }

            if (seeded > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation(
                "Development rooms are available on Property {PropertyId}. Initial readiness is Dirty.",
                propertyId);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Development rooms were not seeded because the database is unavailable.");
        }
    }
}
