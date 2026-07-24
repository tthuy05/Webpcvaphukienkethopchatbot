using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class ProductAdminViewModel
{
    public int Id { get; set; }

    [Required, StringLength(220)]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [StringLength(240)]
    public string? Slug { get; set; }

    [Required]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Required]
    [Display(Name = "Thương hiệu")]
    public int BrandId { get; set; }

    [Range(0, 999999999)]
    [Display(Name = "Giá niêm yết")]
    public decimal Price { get; set; }

    [Range(0, 999999999)]
    [Display(Name = "Giá khuyến mãi")]
    public decimal? SalePrice { get; set; }

    [Display(Name = "Bắt đầu khuyến mãi")]
    public DateTime? SaleStartAt { get; set; }

    [Display(Name = "Kết thúc khuyến mãi")]
    public DateTime? SaleEndAt { get; set; }

    [Required, StringLength(500)]
    [Display(Name = "Ảnh chính")]
    public string MainImageUrl { get; set; } = "/images/products/placeholder.svg";

    [StringLength(120)] public string? Cpu { get; set; }
    [StringLength(80)] public string? Ram { get; set; }
    [StringLength(80)] public string? Ssd { get; set; }
    [StringLength(120)] public string? Gpu { get; set; }
    [StringLength(120)] public string? Screen { get; set; }
    [StringLength(80)] public string? Battery { get; set; }
    [StringLength(80)] public string? Weight { get; set; }
    [StringLength(120)] public string? OperatingSystem { get; set; }

    [StringLength(2500)]
    [Display(Name = "Mô tả")]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Nhu cầu phù hợp")]
    public string SuitableNeeds { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    [Display(Name = "Tồn kho ban đầu")]
    public int InitialStock { get; set; }

    [Display(Name = "Đang bán")]
    public bool IsActive { get; set; } = true;
}
