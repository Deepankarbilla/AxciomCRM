using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entity, string? recordId, object? oldValue = null, object? newValue = null, string? userId = null);
}

public static class JsonOpts
{
    public static readonly JsonSerializerOptions Audit = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };
    public static string? Ser(object? o) => o == null ? null : o as string ?? JsonSerializer.Serialize(o, Audit);
}

public class AuditService(ApplicationDbContext db, IHttpContextAccessor http) : IAuditService
{
    public async Task LogAsync(string action, string entity, string? recordId, object? oldValue = null, object? newValue = null, string? userId = null)
    {
        var ctx = http.HttpContext;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId ?? ctx?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            Action = action, EntityName = entity, RecordId = recordId,
            OldValue = JsonOpts.Ser(oldValue), NewValue = JsonOpts.Ser(newValue),
            CreatedDate = DateTime.UtcNow,
            IpAddress = ctx?.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync();
    }
}
