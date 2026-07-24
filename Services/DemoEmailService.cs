using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;

namespace Webpcvaphukienkethopchatbot.Services;

public sealed class DemoEmailService : IDemoEmailService
{
    private readonly ApplicationDbContext _dbContext;

    public DemoEmailService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task QueueAsync(
        string recipient,
        string subject,
        string body,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        _dbContext.EmailOutbox.Add(new EmailOutbox
        {
            Recipient = recipient.Trim(),
            Subject = subject.Trim(),
            Body = body,
            ActionUrl = actionUrl,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
