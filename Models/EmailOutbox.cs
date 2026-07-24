using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.Models;

public sealed class EmailOutbox
{
    public long Id { get; set; }

    [Required, EmailAddress, StringLength(256)]
    public string Recipient { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? ActionUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }
}
