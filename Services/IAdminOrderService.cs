using Webpcvaphukienkethopchatbot.Models.Enums;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public interface IAdminOrderService
{
    Task<IReadOnlyList<AdminOrderListItemViewModel>> GetOrdersAsync(CancellationToken cancellationToken = default);

    Task<AdminOrderDetailViewModel?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(int id, OrderStatus status, CancellationToken cancellationToken = default);
}
