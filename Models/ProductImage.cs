using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.Models;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Required, StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [StringLength(220)]
    public string? AltText { get; set; }

    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }
}
