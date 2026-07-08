using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public class OrderDetailViewModel
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public OrderStatus Status { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string ShippingAddress { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public IReadOnlyList<OrderDetailItemViewModel> Items { get; set; } = new List<OrderDetailItemViewModel>();
}
