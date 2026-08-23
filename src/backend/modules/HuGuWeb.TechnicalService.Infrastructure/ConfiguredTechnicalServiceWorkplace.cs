using HuGuWeb.TechnicalService.Application;
using HuGuWeb.Workforce.Application;

namespace HuGuWeb.TechnicalService.Infrastructure;

public sealed class ConfiguredTechnicalServiceWorkplace(IWorkplaceContext workplace) : ITechnicalServiceWorkplace
{
    public Guid PropertyId => workplace.PropertyId;

    public bool IsConfigured => workplace.IsConfigured;
}
