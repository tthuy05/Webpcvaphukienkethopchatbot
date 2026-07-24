using Microsoft.AspNetCore.Identity;

namespace Webpcvaphukienkethopchatbot.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public Cart? Cart { get; set; }

    public UserPreference? Preference { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ICollection<ChatbotLog> ChatbotLogs { get; set; } = new List<ChatbotLog>();

    public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}
