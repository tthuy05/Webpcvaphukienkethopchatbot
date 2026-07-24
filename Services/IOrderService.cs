using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.Services;

public interface IOrderService
{
    Task<OrderOperationResult> CheckoutAsync(
        string userId,
        string receiverName,
        string phoneNumber,
        string shippingAddress,
        PaymentMethod paymentMethod,
        string? couponCode = null,
        CancellationToken cancellationToken = default);

    Task<OrderOperationResult> CancelAsync(
        int orderId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<OrderOperationResult> ChangeStatusAsync(
        int orderId,
        OrderStatus targetStatus,
        string actorUserId,
        string? note = null,
        CancellationToken cancellationToken = default);

    Task<int> CancelExpiredOrdersAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default);
}

public sealed record OrderOperationResult(bool Succeeded, int? OrderId, string ErrorMessage)
{
    public static OrderOperationResult Success(int orderId) => new(true, orderId, string.Empty);

    public static OrderOperationResult Failure(string message, int? orderId = null) => new(false, orderId, message);
}
