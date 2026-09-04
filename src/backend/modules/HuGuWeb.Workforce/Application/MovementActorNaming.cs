namespace HuGuWeb.Workforce.Application;

public static class MovementActorNaming
{
    public static bool LooksLikeRawUserId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Guid.TryParse(value.Trim(), out _);

    public static MovementActorDto Resolve(
        string? userId,
        string? personDisplayName,
        string? applicationUserDisplayName,
        string? emailOrUserName)
    {
        var id = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
        if (id is null)
        {
            return new MovementActorDto(null, null);
        }

        var person = UsableLabel(personDisplayName);
        if (person is not null)
        {
            return new MovementActorDto(id, person);
        }

        var applicationName = UsableLabel(applicationUserDisplayName);
        if (applicationName is not null)
        {
            return new MovementActorDto(id, applicationName);
        }

        var login = UsableLabel(emailOrUserName);
        if (login is not null)
        {
            return new MovementActorDto(id, login);
        }

        return new MovementActorDto(id, null);
    }

    public static MovementActorDto Unresolved(string? userId) => Resolve(userId, null, null, null);

    public static PersonnelMovementListItemDto WithActor(
        PersonnelMovementListItemDto item,
        MovementActorDto actor) =>
        item with { Actor = actor };

    public static PersonnelMovementDetailDto WithActors(
        PersonnelMovementDetailDto item,
        MovementActorDto actor,
        MovementActorDto? cancelledBy) =>
        item with { Actor = actor, CancelledBy = cancelledBy };

    private static string? UsableLabel(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || LooksLikeRawUserId(trimmed))
        {
            return null;
        }

        return trimmed;
    }
}
