namespace Webpcvaphukienkethopchatbot.Services;

public interface IDemoEmailService
{
    Task QueueAsync(
        string recipient,
        string subject,
        string body,
        string? actionUrl = null,
        CancellationToken cancellationToken = default);
}
