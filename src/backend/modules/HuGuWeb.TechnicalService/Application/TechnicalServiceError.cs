namespace HuGuWeb.TechnicalService.Application;

public sealed record TechnicalServiceError(string Code, string Title, string Detail, int StatusCode)
{
    public static TechnicalServiceError InvalidRequest(string code, string detail) =>
        new(code, "The request is invalid.", detail, 400);

    public static TechnicalServiceError NotFound(string code, string detail) =>
        new(code, "The requested resource was not found.", detail, 404);

    public static TechnicalServiceError Conflict(string code, string title, string detail) =>
        new(code, title, detail, 409);

    public static TechnicalServiceError IssueNotFound() =>
        NotFound("issue-not-found", "The technical issue was not found.");

    public static TechnicalServiceError RoomNotFound() =>
        NotFound("room-not-found", "The room was not found.");

    public static TechnicalServiceError CategoryNotFound() =>
        NotFound("category-not-found", "The issue category was not found.");

    public static TechnicalServiceError EmployeeNotFound() =>
        NotFound("employee-not-found", "The employee was not found or is not currently employed.");

    public static TechnicalServiceError InvalidTransition(string detail) =>
        InvalidRequest("invalid-transition", detail);

    public static TechnicalServiceError AssignmentRequired() =>
        InvalidRequest("assignment-required", "An assigned employee is required.");

    public static TechnicalServiceError InvalidPriority() =>
        InvalidRequest("invalid-priority", "Priority must be Normal, High, or Urgent.");

    public static TechnicalServiceError InvalidBlocking() =>
        InvalidRequest("invalid-blocking", "A blocking issue requires Out of Order or Out of Service.");

    public static TechnicalServiceError NoteRequired() =>
        InvalidRequest("note-required", "A note is required.");

    public static TechnicalServiceError InvalidPreparationImpact() =>
        InvalidRequest("invalid-preparation-impact", "Preparation impact must be None or RequiresPreparation.");

    public static TechnicalServiceError RoomInactive() =>
        InvalidRequest("room-inactive", "An inactive room cannot receive a technical issue.");

    public static TechnicalServiceError StaleIssue() =>
        Conflict(
            "stale-issue",
            "The issue was changed by someone else.",
            "Reload the issue and try the action again.");

    public static TechnicalServiceError WorkplaceNotConfigured() =>
        new(
            "workplace-not-configured",
            "Workplace is not configured.",
            "Organization and Property must be configured for technical service.",
            500);

    public static TechnicalServiceError PreparationImpactFailed(string detail) =>
        Conflict(
            "preparation-impact-failed",
            "Preparation could not be requested.",
            detail);
}
