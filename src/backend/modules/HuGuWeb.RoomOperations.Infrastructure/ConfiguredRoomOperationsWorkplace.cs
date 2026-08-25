using HuGuWeb.RoomOperations.Application;
using HuGuWeb.Workforce.Application;

namespace HuGuWeb.RoomOperations.Infrastructure;

public sealed class ConfiguredRoomOperationsWorkplace(IWorkplaceContext workplace) : IRoomOperationsWorkplace
{
    public Guid PropertyId => workplace.PropertyId;

    public bool IsConfigured => workplace.HasProperty;
}
