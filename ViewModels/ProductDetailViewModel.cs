namespace Webpcvaphukienkethopchatbot.ViewModels;

public class ProductDetailViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string BrandName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string MainImageUrl { get; set; } = string.Empty;

    public string? Cpu { get; set; }

    public string? Ram { get; set; }

    public string? Ssd { get; set; }

    public string? Gpu { get; set; }

    public string? Screen { get; set; }

    public string? Battery { get; set; }

    public string? Weight { get; set; }

    public string? OperatingSystem { get; set; }

    public int StockQuantity { get; set; }

    public string Description { get; set; } = string.Empty;

    public string SuitableNeeds { get; set; } = string.Empty;
}
