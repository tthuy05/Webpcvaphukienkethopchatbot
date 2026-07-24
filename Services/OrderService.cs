using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.Services;

public sealed class OrderService : IOrderService
{
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> AllowedTransitions =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
            [OrderStatus.Confirmed] = [OrderStatus.Shipping, OrderStatus.Cancelled],
            [OrderStatus.Shipping] = [OrderStatus.Completed],
            [OrderStatus.Completed] = [],
            [OrderStatus.Cancelled] = []
        };

    private readonly ApplicationDbContext _dbContext;
    private readonly CommerceOptions _options;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ApplicationDbContext dbContext,
        IOptions<CommerceOptions> options,
        ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OrderOperationResult> CheckoutAsync(
        string userId,
        string receiverName,
        string phoneNumber,
        string shippingAddress,
        PaymentMethod paymentMethod,
        string? couponCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return OrderOperationResult.Failure("Phiên đăng nhập không hợp lệ.");
        }

        await using var transaction = await BeginSerializableTransactionAsync(cancellationToken);

        try
        {
            var cart = await _dbContext.Carts
                .AsNoTracking()
                .Include(currentCart => currentCart.Items)
                .SingleOrDefaultAsync(currentCart => currentCart.ApplicationUserId == userId, cancellationToken);

            if (cart is null || cart.Items.Count == 0)
            {
                return await RollbackFailureAsync(transaction, "Giỏ hàng đang trống.", cancellationToken);
            }

            var cartLines = cart.Items
                .GroupBy(item => item.ProductId)
                .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
                .OrderBy(item => item.ProductId)
                .ToList();

            if (cartLines.Any(item => item.Quantity <= 0))
            {
                return await RollbackFailureAsync(transaction, "Số lượng sản phẩm không hợp lệ.", cancellationToken);
            }

            var productIds = cartLines.Select(item => item.ProductId).ToList();
            var products = await _dbContext.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id) && product.IsActive)
                .ToDictionaryAsync(product => product.Id, cancellationToken);

            if (products.Count != productIds.Count)
            {
                return await RollbackFailureAsync(
                    transaction,
                    "Một hoặc nhiều sản phẩm không còn được bán. Vui lòng cập nhật giỏ hàng.",
                    cancellationToken);
            }

            foreach (var line in cartLines)
            {
                var product = products[line.ProductId];
                if (product.StockQuantity < line.Quantity)
                {
                    return await RollbackFailureAsync(
                        transaction,
                        $"Sản phẩm “{product.Name}” chỉ còn {product.StockQuantity} sản phẩm.",
                        cancellationToken);
                }
            }

            var now = DateTime.UtcNow;
            var order = new Order
            {
                OrderNumber = CreateOrderNumber(now),
                ApplicationUserId = userId,
                ReceiverName = receiverName.Trim(),
                PhoneNumber = phoneNumber.Trim(),
                ShippingAddress = shippingAddress.Trim(),
                Status = OrderStatus.Pending,
                CreatedAt = now
            };

            foreach (var line in cartLines)
            {
                var product = products[line.ProductId];
                var unitPrice = product.GetCurrentPrice(now);
                order.Details.Add(new OrderDetail
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = unitPrice,
                    Quantity = line.Quantity,
                    LineTotal = unitPrice * line.Quantity
                });
            }

            order.SubtotalAmount = order.Details.Sum(detail => detail.LineTotal);
            var couponResult = await ResolveCouponAsync(couponCode, order.SubtotalAmount, now, cancellationToken);
            if (!couponResult.Succeeded)
            {
                return await RollbackFailureAsync(transaction, couponResult.ErrorMessage, cancellationToken);
            }

            order.CouponId = couponResult.Coupon?.Id;
            order.CouponCode = couponResult.Coupon?.Code;
            order.DiscountAmount = couponResult.DiscountAmount;
            order.ShippingFee = order.SubtotalAmount >= _options.FreeShippingThreshold
                ? 0
                : _options.FlatShippingFee;
            order.TotalAmount = Math.Max(0, order.SubtotalAmount - order.DiscountAmount + order.ShippingFee);
            order.Payment = CreatePayment(paymentMethod, order.TotalAmount, now);
            order.StatusHistory.Add(new OrderStatusHistory
            {
                FromStatus = null,
                ToStatus = OrderStatus.Pending,
                Note = "Khách hàng tạo đơn.",
                ChangedByUserId = userId,
                ChangedAt = now
            });

            foreach (var line in cartLines)
            {
                var quantity = line.Quantity;
                var affectedRows = await _dbContext.Products
                    .Where(product =>
                        product.Id == line.ProductId &&
                        product.IsActive &&
                        product.StockQuantity >= quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(product => product.StockQuantity, product => product.StockQuantity - quantity)
                        .SetProperty(product => product.SoldQuantity, product => product.SoldQuantity + quantity)
                        .SetProperty(product => product.UpdatedAt, now), cancellationToken);

                if (affectedRows != 1)
                {
                    return await RollbackFailureAsync(
                        transaction,
                        $"Sản phẩm “{products[line.ProductId].Name}” vừa hết hàng hoặc không đủ số lượng.",
                        cancellationToken);
                }
            }

            if (couponResult.Coupon is not null)
            {
                var couponUpdated = await _dbContext.Coupons
                    .Where(coupon =>
                        coupon.Id == couponResult.Coupon.Id &&
                        coupon.IsActive &&
                        (!coupon.UsageLimit.HasValue || coupon.UsedCount < coupon.UsageLimit.Value))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(coupon => coupon.UsedCount, coupon => coupon.UsedCount + 1), cancellationToken);

                if (couponUpdated != 1)
                {
                    return await RollbackFailureAsync(
                        transaction,
                        "Mã giảm giá vừa hết lượt sử dụng.",
                        cancellationToken);
                }
            }

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var line in cartLines)
            {
                var product = products[line.ProductId];
                _dbContext.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = line.ProductId,
                    Type = InventoryTransactionType.Sale,
                    QuantityDelta = -line.Quantity,
                    StockBefore = product.StockQuantity,
                    StockAfter = product.StockQuantity - line.Quantity,
                    ReferenceType = "Order",
                    ReferenceId = order.Id.ToString(),
                    Note = order.OrderNumber,
                    PerformedByUserId = userId,
                    CreatedAt = now
                });
            }

            await QueueOrderEmailAsync(
                userId,
                $"Đã nhận đơn {order.OrderNumber}",
                $"Đơn hàng {order.OrderNumber} trị giá {order.TotalAmount:N0} ₫ đã được tạo.",
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _dbContext.CartItems
                .Where(item => item.CartId == cart.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await _dbContext.Carts
                .Where(currentCart => currentCart.Id == cart.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(currentCart => currentCart.UpdatedAt, now), cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogInformation("Order {OrderNumber} created for user {UserId}; inventory persisted.", order.OrderNumber, userId);
            return OrderOperationResult.Success(order.Id);
        }
        catch (Exception exception)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            _logger.LogError(exception, "Checkout failed for user {UserId}; transaction rolled back.", userId);
            return OrderOperationResult.Failure("Không thể tạo đơn hàng lúc này. Không có tồn kho nào bị thay đổi.");
        }
    }

    public Task<OrderOperationResult> CancelAsync(
        int orderId,
        string userId,
        CancellationToken cancellationToken = default) =>
        CancelInternalAsync(orderId, userId, userId, enforceOwner: true, "Khách hàng hủy đơn.", cancellationToken);

    public async Task<OrderOperationResult> ChangeStatusAsync(
        int orderId,
        OrderStatus targetStatus,
        string actorUserId,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (targetStatus == OrderStatus.Cancelled)
        {
            return await CancelInternalAsync(
                orderId,
                ownerUserId: null,
                actorUserId,
                enforceOwner: false,
                string.IsNullOrWhiteSpace(note) ? "Quản trị viên hủy đơn." : note.Trim(),
                cancellationToken);
        }

        await using var transaction = await BeginSerializableTransactionAsync(cancellationToken);
        try
        {
            var order = await _dbContext.Orders
                .AsNoTracking()
                .SingleOrDefaultAsync(currentOrder => currentOrder.Id == orderId, cancellationToken);
            if (order is null)
            {
                return await RollbackFailureAsync(transaction, "Không tìm thấy đơn hàng.", cancellationToken, orderId);
            }

            if (!AllowedTransitions[order.Status].Contains(targetStatus))
            {
                return await RollbackFailureAsync(
                    transaction,
                    $"Không thể chuyển đơn từ {order.Status} sang {targetStatus}.",
                    cancellationToken,
                    orderId);
            }

            var now = DateTime.UtcNow;
            var trackingCode = targetStatus == OrderStatus.Shipping
                ? order.TrackingCode ?? $"TRK-{Guid.NewGuid():N}"[..16].ToUpperInvariant()
                : order.TrackingCode;
            var affected = await _dbContext.Orders
                .Where(currentOrder => currentOrder.Id == orderId && currentOrder.Status == order.Status)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(currentOrder => currentOrder.Status, targetStatus)
                    .SetProperty(currentOrder => currentOrder.TrackingCode, trackingCode)
                    .SetProperty(currentOrder => currentOrder.UpdatedAt, now), cancellationToken);

            if (affected != 1)
            {
                return await RollbackFailureAsync(
                    transaction,
                    "Trạng thái đơn vừa thay đổi. Vui lòng tải lại.",
                    cancellationToken,
                    orderId);
            }

            if (targetStatus == OrderStatus.Completed)
            {
                await _dbContext.Payments
                    .Where(payment => payment.OrderId == orderId && payment.Status != PaymentStatus.Cancelled)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(payment => payment.Status, PaymentStatus.Paid)
                        .SetProperty(payment => payment.PaidAt, now), cancellationToken);
            }

            _dbContext.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                FromStatus = order.Status,
                ToStatus = targetStatus,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                ChangedByUserId = actorUserId,
                ChangedAt = now
            });
            await QueueOrderEmailAsync(
                order.ApplicationUserId,
                $"Đơn {order.OrderNumber}: {targetStatus}",
                $"Trạng thái đơn hàng đã chuyển sang {targetStatus}." +
                (trackingCode is null ? string.Empty : $" Mã vận đơn: {trackingCode}."),
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return OrderOperationResult.Success(orderId);
        }
        catch (Exception exception)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            _logger.LogError(exception, "Failed to change order {OrderId} to {Status}.", orderId, targetStatus);
            return OrderOperationResult.Failure("Không thể cập nhật trạng thái đơn.", orderId);
        }
    }

    public async Task<int> CancelExpiredOrdersAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        var orderIds = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Status == OrderStatus.Pending && order.CreatedAt < olderThanUtc)
            .OrderBy(order => order.Id)
            .Select(order => order.Id)
            .ToListAsync(cancellationToken);

        var cancelledCount = 0;
        foreach (var orderId in orderIds)
        {
            var result = await CancelInternalAsync(
                orderId,
                ownerUserId: null,
                actorUserId: null,
                enforceOwner: false,
                "Hệ thống tự hủy đơn quá hạn.",
                cancellationToken);
            if (result.Succeeded)
            {
                cancelledCount++;
            }
        }

        return cancelledCount;
    }

    private async Task<OrderOperationResult> CancelInternalAsync(
        int orderId,
        string? ownerUserId,
        string? actorUserId,
        bool enforceOwner,
        string note,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginSerializableTransactionAsync(cancellationToken);
        try
        {
            var orderQuery = _dbContext.Orders
                .AsNoTracking()
                .Include(currentOrder => currentOrder.Details)
                .Where(currentOrder => currentOrder.Id == orderId);
            if (enforceOwner)
            {
                orderQuery = orderQuery.Where(currentOrder => currentOrder.ApplicationUserId == ownerUserId);
            }

            var order = await orderQuery.SingleOrDefaultAsync(cancellationToken);
            if (order is null)
            {
                return await RollbackFailureAsync(transaction, "Không tìm thấy đơn hàng.", cancellationToken, orderId);
            }

            if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            {
                return await RollbackFailureAsync(
                    transaction,
                    "Chỉ có thể hủy đơn đang chờ xử lý hoặc đã xác nhận.",
                    cancellationToken,
                    orderId);
            }

            var productIds = order.Details.Select(detail => detail.ProductId).Distinct().ToList();
            var inventoryBefore = await _dbContext.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id, product => product.StockQuantity, cancellationToken);

            var now = DateTime.UtcNow;
            var orderUpdated = await _dbContext.Orders
                .Where(currentOrder =>
                    currentOrder.Id == orderId &&
                    (!enforceOwner || currentOrder.ApplicationUserId == ownerUserId) &&
                    (currentOrder.Status == OrderStatus.Pending || currentOrder.Status == OrderStatus.Confirmed))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(currentOrder => currentOrder.Status, OrderStatus.Cancelled)
                    .SetProperty(currentOrder => currentOrder.UpdatedAt, now), cancellationToken);

            if (orderUpdated != 1)
            {
                return await RollbackFailureAsync(
                    transaction,
                    "Trạng thái đơn hàng vừa thay đổi. Vui lòng tải lại trang.",
                    cancellationToken,
                    orderId);
            }

            foreach (var detail in order.Details.OrderBy(detail => detail.ProductId))
            {
                var quantity = detail.Quantity;
                var productUpdated = await _dbContext.Products
                    .Where(product => product.Id == detail.ProductId && product.SoldQuantity >= quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(product => product.StockQuantity, product => product.StockQuantity + quantity)
                        .SetProperty(product => product.SoldQuantity, product => product.SoldQuantity - quantity)
                        .SetProperty(product => product.UpdatedAt, now), cancellationToken);

                if (productUpdated != 1 || !inventoryBefore.TryGetValue(detail.ProductId, out var stockBefore))
                {
                    throw new InvalidOperationException(
                        $"Inventory integrity error while restoring product {detail.ProductId} for order {orderId}.");
                }

                _dbContext.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = detail.ProductId,
                    Type = InventoryTransactionType.OrderCancellation,
                    QuantityDelta = quantity,
                    StockBefore = stockBefore,
                    StockAfter = stockBefore + quantity,
                    ReferenceType = "Order",
                    ReferenceId = orderId.ToString(),
                    Note = order.OrderNumber,
                    PerformedByUserId = actorUserId,
                    CreatedAt = now
                });
            }

            if (order.CouponId.HasValue)
            {
                await _dbContext.Coupons
                    .Where(coupon => coupon.Id == order.CouponId.Value && coupon.UsedCount > 0)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(coupon => coupon.UsedCount, coupon => coupon.UsedCount - 1), cancellationToken);
            }

            await _dbContext.Payments
                .Where(payment => payment.OrderId == orderId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(payment => payment.Status, PaymentStatus.Cancelled), cancellationToken);

            _dbContext.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                FromStatus = order.Status,
                ToStatus = OrderStatus.Cancelled,
                Note = note,
                ChangedByUserId = actorUserId,
                ChangedAt = now
            });
            await QueueOrderEmailAsync(
                order.ApplicationUserId,
                $"Đơn {order.OrderNumber} đã hủy",
                "Đơn hàng đã được hủy và tồn kho đã được hoàn lại.",
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogInformation("Order {OrderId} cancelled; inventory restored.", orderId);
            return OrderOperationResult.Success(orderId);
        }
        catch (Exception exception)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            _logger.LogError(exception, "Cancellation failed for order {OrderId}; transaction rolled back.", orderId);
            return OrderOperationResult.Failure("Không thể hủy đơn. Tồn kho chưa bị thay đổi.", orderId);
        }
    }

    private async Task<CouponResolution> ResolveCouponAsync(
        string? code,
        decimal subtotal,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return CouponResolution.None;
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var coupon = await _dbContext.Coupons
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Code == normalizedCode, cancellationToken);

        if (coupon is null || !coupon.IsActive ||
            (coupon.StartsAt.HasValue && coupon.StartsAt.Value > now) ||
            (coupon.EndsAt.HasValue && coupon.EndsAt.Value < now))
        {
            return CouponResolution.Failure("Mã giảm giá không tồn tại hoặc đã hết hạn.");
        }

        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
        {
            return CouponResolution.Failure("Mã giảm giá đã hết lượt sử dụng.");
        }

        if (subtotal < coupon.MinimumOrderAmount)
        {
            return CouponResolution.Failure(
                $"Đơn hàng phải đạt {coupon.MinimumOrderAmount:N0} ₫ để dùng mã này.");
        }

        var discount = coupon.DiscountType == DiscountType.Percentage
            ? subtotal * coupon.Value / 100m
            : coupon.Value;
        if (coupon.MaximumDiscountAmount.HasValue)
        {
            discount = Math.Min(discount, coupon.MaximumDiscountAmount.Value);
        }

        discount = Math.Min(discount, subtotal);
        return CouponResolution.Success(coupon, discount);
    }

    private async Task<IDbContextTransaction?> BeginSerializableTransactionAsync(CancellationToken cancellationToken) =>
        _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

    private static async Task<OrderOperationResult> RollbackFailureAsync(
        IDbContextTransaction? transaction,
        string message,
        CancellationToken cancellationToken,
        int? orderId = null)
    {
        await SafeRollbackAsync(transaction, cancellationToken);
        return OrderOperationResult.Failure(message, orderId);
    }

    private static async Task SafeRollbackAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    private static string CreateOrderNumber(DateTime createdAt) =>
        $"ORD-{createdAt:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();

    private static Payment CreatePayment(PaymentMethod method, decimal amount, DateTime now) => new()
    {
        Method = method,
        Amount = amount,
        Status = method == PaymentMethod.CashOnDelivery ? PaymentStatus.Unpaid : PaymentStatus.Simulated,
        TransactionCode = method == PaymentMethod.SimulatedBankTransfer
            ? $"SIM-{Guid.NewGuid():N}"[..20].ToUpperInvariant()
            : null,
        CreatedAt = now
    };

    private async Task QueueOrderEmailAsync(
        string userId,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var recipient = await _dbContext.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.Email)
            .SingleOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(recipient))
        {
            _dbContext.EmailOutbox.Add(new EmailOutbox
            {
                Recipient = recipient,
                Subject = subject,
                Body = body,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private sealed record CouponResolution(bool Succeeded, Coupon? Coupon, decimal DiscountAmount, string ErrorMessage)
    {
        public static CouponResolution None => new(true, null, 0, string.Empty);

        public static CouponResolution Success(Coupon coupon, decimal discount) => new(true, coupon, discount, string.Empty);

        public static CouponResolution Failure(string message) => new(false, null, 0, message);
    }
}
