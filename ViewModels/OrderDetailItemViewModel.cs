namespace Webpcvaphukienkethopchatbot.ViewModels;

public class OrderDetailItemViewModel
{
    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
