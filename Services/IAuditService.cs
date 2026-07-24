namespace Webpcvaphukienkethopchatbot.Services;

public interface IAuditService
{
    Task WriteAsync(
        string action,
        string? userId = null,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        bool succeeded = true,
        CancellationToken cancellationToken = default);
}
