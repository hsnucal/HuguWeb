namespace HuGuWeb.Api.Authorization;

public sealed class EmployeeAccountLink
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
