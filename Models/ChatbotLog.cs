using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.Models;

public class ChatbotLog
{
    public int Id { get; set; }

    public string? ApplicationUserId { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }

    [Required, StringLength(1000)]
    public string Question { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Answer { get; set; } = string.Empty;

    public decimal? DetectedBudget { get; set; }

    [StringLength(120)]
    public string? DetectedUsageNeed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
