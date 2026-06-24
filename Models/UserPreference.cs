using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.Models;

public class UserPreference
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser? ApplicationUser { get; set; }

    [Required, StringLength(10)]
    public string PreferredLanguage { get; set; } = "vi-VN";

    [Required, StringLength(20)]
    public string PreferredTheme { get; set; } = "light";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
