using System;

namespace ScheduleClin.Services.Audit;

public class AuditEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Action { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public string? Details { get; set; }

    public override string ToString()
    {
        return $"[{Timestamp:O}] UserId={UserId} UserName={UserName} Action={Action} Method={Method} Path={Path} Details={Details}";
    }
}
