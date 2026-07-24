namespace Webpcvaphukienkethopchatbot.Models;

public sealed class WishlistItem
{
    public int Id { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser? ApplicationUser { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
