using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleClin.Services.Audit;

public class FileAuditLogger : IAuditLogger
{
    private readonly string _path;

    public FileAuditLogger()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Logs");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "audit.log");
    }

    public async Task LogAsync(AuditEntry entry)
    {
        var line = entry.ToString();
        try
        {
            await File.AppendAllTextAsync(_path, line + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // falha ao gravar log de auditoria não deve interromper a aplicação
        }
    }
}
