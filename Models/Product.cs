using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webpcvaphukienkethopchatbot.Models;

public class Product
{
    public int Id { get; set; }

    [Required, StringLength(220)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(240)]
    public string Slug { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public int BrandId { get; set; }

    public Brand? Brand { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 999999999)]
    public decimal Price { get; set; }

    [Required, StringLength(500)]
    public string MainImageUrl { get; set; } = "/images/products/placeholder.svg";

    [StringLength(120)]
    public string? Cpu { get; set; }

    [StringLength(80)]
    public string? Ram { get; set; }

    [StringLength(80)]
    public string? Ssd { get; set; }

    [StringLength(120)]
    public string? Gpu { get; set; }

    [StringLength(120)]
    public string? Screen { get; set; }

    [StringLength(80)]
    public string? Battery { get; set; }

    [StringLength(80)]
    public string? Weight { get; set; }

    [StringLength(120)]
    public string? OperatingSystem { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int SoldQuantity { get; set; }

    [StringLength(2500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string SuitableNeeds { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
