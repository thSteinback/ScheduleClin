using System;
using System.Threading.Tasks;
using ScheduleClin.Context;
using ScheduleClin.Models.Audit;

namespace ScheduleClin.Services.Audit;

public class DbAuditLogger : IAuditLogger
{
    private readonly AppDbContext _db;

    public DbAuditLogger(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(AuditEntry entry)
    {
        try
        {
            var entity = new ScheduleClin.Models.Audit.Audit
            {
                AuditId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                UserName = entry.UserName,
                Action = entry.Action,
                Path = entry.Path,
                Method = entry.Method,
                Details = entry.Details
            };

            if (Guid.TryParse(entry.UserId, out var uid))
                entity.UserId = uid;

            if (entry.Details != null && entry.Details.StartsWith("Status=", StringComparison.OrdinalIgnoreCase))
            {
                var parts = entry.Details.Split('=');
                if (parts.Length == 2 && int.TryParse(parts[1], out var code))
                    entity.StatusCode = code;
            }

            _db.Audits.Add(entity);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // swallow exceptions to avoid impacting request pipeline
        }
    }
}
