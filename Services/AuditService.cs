using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;

namespace Webpcvaphukienkethopchatbot.Services;

public sealed class AuditService : IAuditService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task WriteAsync(
        string action,
        string? userId = null,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        bool succeeded = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            _dbContext.AuditLogs.Add(new AuditLog
            {
                ApplicationUserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context?.Request.Headers.UserAgent.ToString(),
                Succeeded = succeeded,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to write audit log for {Action}.", action);
        }
    }
}
