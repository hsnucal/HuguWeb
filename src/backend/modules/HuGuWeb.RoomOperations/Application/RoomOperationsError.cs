namespace HuGuWeb.RoomOperations.Application;

public sealed record RoomOperationsError(string Code, string Title, string Detail, int StatusCode)
{
    public static RoomOperationsError InvalidRequest(string code, string detail) =>
        new(code, "The request is invalid.", detail, 400);

    public static RoomOperationsError NotFound(string code, string detail) =>
        new(code, "The requested resource was not found.", detail, 404);

    public static RoomOperationsError Conflict(string code, string title, string detail) =>
        new(code, title, detail, 409);

    public static RoomOperationsError RoomNotFound() =>
        NotFound("room-not-found", "The room was not found.");

    public static RoomOperationsError EmployeeNotFound() =>
        NotFound("employee-not-found", "The employee was not found or is not currently employed.");

    public static RoomOperationsError WorkItemNotFound() =>
        NotFound("work-item-not-found", "The housekeeping work item was not found.");

    public static RoomOperationsError InvalidReadinessTransition(string detail) =>
        InvalidRequest("invalid-readiness-transition", detail);

    public static RoomOperationsError ActiveWorkAlreadyExists() =>
        Conflict(
            "active-work-already-exists",
            "Active housekeeping work already exists.",
            "This room already has current housekeeping work. Complete or wait for that work before requesting cleaning again.");

    public static RoomOperationsError StaleWorkItem() =>
        Conflict(
            "stale-work-item",
            "The work item is no longer current.",
            "This housekeeping work belongs to an earlier readiness cycle and cannot change the room.");

    public static RoomOperationsError WorkItemNotCurrent() =>
        InvalidRequest("work-item-not-current", "This housekeeping work item is not the current open work for the room.");

    public static RoomOperationsError RejectionReasonRequired() =>
        InvalidRequest("rejection-reason-required", "A rejection reason is required.");

    public static RoomOperationsError InspectionNotAllowed() =>
        InvalidRequest("inspection-not-allowed", "Inspection is only allowed when the room is Clean.");

    public static RoomOperationsError WorkplaceNotConfigured() =>
        new(
            "workplace-not-configured",
            "Workplace is not configured.",
            "Organization and Property must be configured for room operations.",
            500);

    public static RoomOperationsError RoomInactive() =>
        InvalidRequest("room-inactive", "An inactive room cannot receive new preparation work.");

    public static RoomOperationsError AssignmentRequired() =>
        InvalidRequest("assignment-required", "An assigned employee is required.");

    public static RoomOperationsError InvalidPriority() =>
        InvalidRequest("invalid-priority", "Priority must be Normal, High, or Urgent.");
}
