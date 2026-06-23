using System;

namespace ScheduleClin.Models.Audit;

public class Audit
{
    public Guid AuditId { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Action { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public int? StatusCode { get; set; }
    public string? Details { get; set; }
}
