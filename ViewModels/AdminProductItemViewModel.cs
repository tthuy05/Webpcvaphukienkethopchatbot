namespace Webpcvaphukienkethopchatbot.ViewModels;

public class AdminProductItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string BrandName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }

    public string MainImageUrl { get; set; } = string.Empty;
}
