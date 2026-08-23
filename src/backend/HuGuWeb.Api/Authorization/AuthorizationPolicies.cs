namespace HuGuWeb.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string Authenticated = "Authenticated";
    public const string WorkforceRead = "WorkforceRead";
    public const string WorkforceManage = "WorkforceManage";
    public const string RoomOperationsRead = "RoomOperationsRead";
    public const string RoomOperationsManage = "RoomOperationsManage";
    public const string RoomOperationsInspect = "RoomOperationsInspect";
}
