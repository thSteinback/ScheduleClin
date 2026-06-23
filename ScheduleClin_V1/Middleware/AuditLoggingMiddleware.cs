using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ScheduleClin.Services.Audit;

namespace ScheduleClin.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public AuditLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // proceed first so authentication has a chance to run
        await _next(context);

        var user = context.User;
        var entry = new AuditEntry
        {
            UserId = user?.FindFirst("sub")?.Value ?? user?.Identity?.Name,
            UserName = user?.Identity?.Name,
            Action = "HTTP",
            Path = context.Request?.Path,
            Method = context.Request?.Method,
            Details = $"Status={context.Response?.StatusCode}"
        };

        // resolve logger from the request scope to avoid resolving scoped service from root provider
        var logger = context.RequestServices.GetService<IAuditLogger>();
        if (logger != null)
        {
            _ = logger.LogAsync(entry);
        }
    }
}
