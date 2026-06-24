using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(220)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int BrandId { get; set; }

    [Range(0, 999999999)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

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

    [StringLength(2500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string SuitableNeeds { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string? ExistingImageUrl { get; set; }

    [ValidateNever]
    public IFormFile? ImageFile { get; set; }

    [ValidateNever]
    public IReadOnlyList<SelectListItem> Categories { get; set; } = new List<SelectListItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> Brands { get; set; } = new List<SelectListItem>();
}
