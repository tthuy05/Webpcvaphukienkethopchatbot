using System.Security.Claims;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public interface IOrderService
{
    Task<CheckoutViewModel> BuildCheckoutAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    Task<int> PlaceOrderAsync(ClaimsPrincipal user, CheckoutViewModel model, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderListItemViewModel>> GetOrdersForUserAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    Task<OrderDetailViewModel?> GetOrderDetailForUserAsync(ClaimsPrincipal user, int orderId, CancellationToken cancellationToken = default);
}
