using System.Threading.Tasks;

namespace ScheduleClin.Services.Audit;

public interface IAuditLogger
{
    Task LogAsync(AuditEntry entry);
}
