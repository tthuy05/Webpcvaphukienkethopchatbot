using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public class AdminOrderDetailViewModel : OrderDetailViewModel
{
    public string CustomerEmail { get; set; } = string.Empty;

    public IReadOnlyList<OrderStatus> StatusOptions { get; set; } = Enum.GetValues<OrderStatus>();
}
