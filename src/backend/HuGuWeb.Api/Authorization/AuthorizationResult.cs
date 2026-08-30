namespace HuGuWeb.Api.Authorization;

public sealed record AccessSnapshot(
    string UserId,
    Guid? MembershipId,
    Guid? OrganizationId,
    Guid? PropertyId,
    AuthorizationScopeType? ScopeType,
    Guid? MembershipPropertyId,
    Guid? EmployeeId,
    IReadOnlyList<string> Permissions);

public sealed record AuthorizationError(
    string Code,
    string Title,
    string Detail,
    int StatusCode,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static AuthorizationError InvalidRequest(string code, string detail) =>
        new(code, "The request is invalid.", detail, 400);

    public static AuthorizationError NotFound(string code, string detail) =>
        new(code, "The requested resource was not found.", detail, 404);

    public static AuthorizationError Conflict(string code, string detail) =>
        new(code, "The request conflicts with existing data.", detail, 409);

    public static AuthorizationError UserNotFound() =>
        NotFound("user-not-found", "The user was not found.");

    public static AuthorizationError MembershipNotFound() =>
        NotFound("membership-not-found", "The membership was not found.");

    public static AuthorizationError RoleNotFound() =>
        NotFound("role-not-found", "The role was not found.");

    public static AuthorizationError RoleInUse() =>
        Conflict("role-in-use", "A role assigned to users cannot be deleted. Deactivate it instead.");

    public static AuthorizationError DuplicateMembership() =>
        Conflict("duplicate-membership", "This user already has a membership for the requested organization and property.");

    public static AuthorizationError DuplicateRoleCode() =>
        Conflict("duplicate-role-code", "A role with this code already exists in the organization.");

    public static AuthorizationError EmailInUse() =>
        Conflict("email-in-use", "This email already belongs to a user.");

    public static AuthorizationError EmployeeAlreadyLinked() =>
        Conflict("employee-already-linked", "This employee already has an ERP account.");

    public static AuthorizationError InvalidPermissionCode() =>
        InvalidRequest("invalid-permission-code", "One or more permission codes are not in the application catalogue.");

    public static AuthorizationError ScopeMismatch() =>
        InvalidRequest("scope-mismatch", "The role scope does not match the membership organization/property scope.");

    public static AuthorizationError RoleInactive() =>
        InvalidRequest("role-inactive", "An inactive role cannot be assigned.");

    public static AuthorizationError PasswordRejected() =>
        InvalidRequest("invalid-password", "The password does not meet Identity requirements.");

    public static AuthorizationError LastAdministrator() =>
        new(
            "last-administrator",
            "The request conflicts with existing data.",
            "This change would remove the last user who can manage users or roles in the organization.",
            409);

    public static AuthorizationError PropertyContextRequired() =>
        InvalidRequest(
            "property-context-required",
            "Select an explicit Property before performing this operation.");

    public static AuthorizationError PropertyNotAccessible() =>
        InvalidRequest(
            "property-not-accessible",
            "The selected Property is not in the active membership scope.");

    public static AuthorizationError PropertyNotInOrganization() =>
        InvalidRequest(
            "property-not-in-organization",
            "The Property does not belong to the membership organization.");

    public static AuthorizationError DepartmentScopesRequirePropertyMembership() =>
        InvalidRequest(
            "department-scopes-require-property",
            "Department scopes can only be configured on a Property membership.");

    public static AuthorizationError DepartmentNotFound() =>
        InvalidRequest("department-not-found", "One or more departments were not found.");

    public static AuthorizationError DepartmentNotInMembershipProperty() =>
        InvalidRequest(
            "department-not-in-membership-property",
            "Department scope must belong to the membership Property and Organization.");
}

public sealed class AuthorizationResult<T>
{
    private AuthorizationResult(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private AuthorizationResult(AuthorizationError error)
    {
        Error = error;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public AuthorizationError? Error { get; }

    public static AuthorizationResult<T> Success(T value) => new(value);
    public static implicit operator AuthorizationResult<T>(AuthorizationError error) => new(error);
}

public sealed class AuthorizationResult
{
    private AuthorizationResult() => IsSuccess = true;
    private AuthorizationResult(AuthorizationError error) => Error = error;

    public bool IsSuccess { get; }
    public AuthorizationError? Error { get; }

    public static AuthorizationResult Success() => new();
    public static implicit operator AuthorizationResult(AuthorizationError error) => new(error);
}
