using Microsoft.Extensions.Localization;

namespace HuGuWeb.Api.Localization;

public sealed class CommonMessages;
public sealed class AuthMessages;
public sealed class AuthorizationMessages;
public sealed class HrMessages;
public sealed class WorkforceMessages;
public sealed class RoomOperationsMessages;
public sealed class TechnicalServiceMessages;

public sealed class ApiErrorLocalizer(
    IStringLocalizer<CommonMessages> common,
    IStringLocalizer<AuthMessages> auth,
    IStringLocalizer<AuthorizationMessages> authorization,
    IStringLocalizer<HrMessages> hr,
    IStringLocalizer<WorkforceMessages> workforce,
    IStringLocalizer<RoomOperationsMessages> roomOperations,
    IStringLocalizer<TechnicalServiceMessages> technicalService)
{
    public LocalizedString this[string key]
    {
        get
        {
            foreach (var localizer in Localizers)
            {
                var value = localizer[key];
                if (!value.ResourceNotFound)
                {
                    return value;
                }
            }

            return new LocalizedString(key, key, resourceNotFound: true);
        }
    }

    private IStringLocalizer[] Localizers =>
    [
        common,
        auth,
        authorization,
        hr,
        workforce,
        roomOperations,
        technicalService
    ];
}
