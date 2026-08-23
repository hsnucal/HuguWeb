using HuGuWeb.RoomOperations.Application;
using HuGuWeb.TechnicalService.Application;

namespace HuGuWeb.TechnicalService.Infrastructure;

public sealed class RoomOperationsPreparationImpactConsumer(
    EnsurePreparationRequiredUseCase ensurePreparation) : IRoomPreparationImpactConsumer
{
    public async Task<TechnicalServiceResult<bool>> EnsurePreparationRequiredAsync(
        Guid roomId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var result = await ensurePreparation.ExecuteAsync(
            new EnsurePreparationRequiredCommand(roomId, actorUserId, "Repair required room preparation."),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return TechnicalServiceError.PreparationImpactFailed(
                result.Error?.Detail ?? "Room Operations could not ensure preparation.");
        }

        return true;
    }
}
