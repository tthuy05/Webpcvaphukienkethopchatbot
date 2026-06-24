using Microsoft.AspNetCore.Mvc.Rendering;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public class ProductFilterViewModel
{
    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    public int? BrandId { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public string? Cpu { get; set; }

    public string? Ram { get; set; }

    public string? Ssd { get; set; }

    public string? Gpu { get; set; }

    public string? UsageNeed { get; set; }

    public string SortBy { get; set; } = "newest";

    public IReadOnlyList<ProductCardViewModel> Products { get; set; } = new List<ProductCardViewModel>();

    public IReadOnlyList<SelectListItem> Categories { get; set; } = new List<SelectListItem>();

    public IReadOnlyList<SelectListItem> Brands { get; set; } = new List<SelectListItem>();
}
