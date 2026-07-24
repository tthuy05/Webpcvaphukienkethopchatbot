namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class CartViewModel
{
    public IReadOnlyList<CartLineViewModel> Items { get; init; } = [];

    public decimal TotalAmount => Items.Sum(item => item.LineTotal);
}

public sealed class CartLineViewModel
{
    public int Id { get; init; }

    public int ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string ImageUrl { get; init; } = string.Empty;

    public decimal UnitPrice { get; init; }

    public int Quantity { get; init; }

    public int StockQuantity { get; init; }

    public bool IsAvailable { get; init; }

    public decimal LineTotal => UnitPrice * Quantity;
}
