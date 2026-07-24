using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.Models;

public sealed class ProductReview
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser? ApplicationUser { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required, StringLength(1000, MinimumLength = 3)]
    public string Comment { get; set; } = string.Empty;

    public bool IsVerifiedPurchase { get; set; }

    public bool IsVisible { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
