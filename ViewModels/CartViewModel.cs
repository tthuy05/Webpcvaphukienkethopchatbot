namespace Webpcvaphukienkethopchatbot.ViewModels;

public class CartViewModel
{
    public IReadOnlyList<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();

    public decimal Total => Items.Sum(item => item.LineTotal);
}
