using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public class AdminOrderListItemViewModel
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }
}
