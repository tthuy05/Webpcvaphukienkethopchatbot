namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class ProductCatalogViewModel
{
    public string? Search { get; init; }

    public int? CategoryId { get; init; }

    public int? BrandId { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public string Sort { get; init; } = "newest";

    public int Page { get; init; } = 1;

    public int TotalPages { get; init; }

    public IReadOnlyList<ProductCardViewModel> Products { get; init; } = [];

    public IReadOnlyList<LookupItemViewModel> Categories { get; init; } = [];

    public IReadOnlyList<LookupItemViewModel> Brands { get; init; } = [];
}

public sealed class ProductCardViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ImageUrl { get; init; } = string.Empty;

    public decimal OriginalPrice { get; init; }

    public decimal CurrentPrice { get; init; }

    public int StockQuantity { get; init; }

    public double? AverageRating { get; init; }
}

public sealed class LookupItemViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
