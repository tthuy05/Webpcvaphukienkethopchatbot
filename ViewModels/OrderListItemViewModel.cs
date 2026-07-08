using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public class OrderListItemViewModel
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }
}
