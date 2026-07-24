using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.Models;

public sealed class AuditLog
{
    public long Id { get; set; }

    public string? ApplicationUserId { get; set; }

    [Required, StringLength(80)]
    public string Action { get; set; } = string.Empty;

    [StringLength(80)]
    public string? EntityType { get; set; }

    [StringLength(100)]
    public string? EntityId { get; set; }

    [StringLength(1000)]
    public string? Details { get; set; }

    [StringLength(80)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public bool Succeeded { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
