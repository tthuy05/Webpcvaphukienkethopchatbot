using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Models.Enums;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICartService _cartService;

    public OrderService(ApplicationDbContext dbContext, ICartService cartService)
    {
        _dbContext = dbContext;
        _cartService = cartService;
    }

    public async Task<CheckoutViewModel> BuildCheckoutAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(user);
        var appUser = await _dbContext.Users.AsNoTracking().FirstAsync(item => item.Id == userId, cancellationToken);

        return new CheckoutViewModel
        {
            ReceiverName = appUser.FullName,
            PhoneNumber = appUser.PhoneNumber ?? string.Empty,
            ShippingAddress = appUser.Address ?? string.Empty,
            Cart = await _cartService.GetCartAsync(user, cancellationToken)
        };
    }

    public async Task<int> PlaceOrderAsync(ClaimsPrincipal user, CheckoutViewModel model, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(user);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var cart = await _dbContext.Carts
            .Include(item => item.Items)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(item => item.ApplicationUserId == userId, cancellationToken);

        if (cart is null || !cart.Items.Any())
        {
            throw new InvalidOperationException("Gio hang dang trong.");
        }

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            ApplicationUserId = userId,
            ReceiverName = model.ReceiverName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            ShippingAddress = model.ShippingAddress.Trim(),
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in cart.Items)
        {
            if (item.Product is null || !item.Product.IsActive)
            {
                throw new InvalidOperationException("Gio hang co san pham khong kha dung.");
            }

            if (item.Quantity <= 0 || item.Quantity > item.Product.StockQuantity)
            {
                throw new InvalidOperationException("So luong trong gio hang khong hop le.");
            }

            item.Product.StockQuantity -= item.Quantity;
            item.Product.SoldQuantity += item.Quantity;

            order.Details.Add(new OrderDetail
            {
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                UnitPrice = item.Product.Price,
                Quantity = item.Quantity,
                LineTotal = item.Product.Price * item.Quantity
            });
        }

        order.TotalAmount = order.Details.Sum(detail => detail.LineTotal);
        order.Payment = new Payment
        {
            Method = model.PaymentMethod,
            Status = model.PaymentMethod == PaymentMethod.CashOnDelivery ? PaymentStatus.Unpaid : PaymentStatus.Simulated,
            Amount = order.TotalAmount,
            TransactionCode = model.PaymentMethod == PaymentMethod.SimulatedBankTransfer ? $"SIM-{DateTime.UtcNow:yyyyMMddHHmmss}" : null,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Orders.Add(order);
        _dbContext.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return order.Id;
    }

    public async Task<IReadOnlyList<OrderListItemViewModel>> GetOrdersForUserAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(user);
        return await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.ApplicationUserId == userId)
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new OrderListItemViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CreatedAt = order.CreatedAt,
                Status = order.Status,
                TotalAmount = order.TotalAmount
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderDetailViewModel?> GetOrderDetailForUserAsync(ClaimsPrincipal user, int orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(user);
        return await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId && order.ApplicationUserId == userId)
            .Select(order => new OrderDetailViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CreatedAt = order.CreatedAt,
                Status = order.Status,
                ReceiverName = order.ReceiverName,
                PhoneNumber = order.PhoneNumber,
                ShippingAddress = order.ShippingAddress,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.Payment != null ? order.Payment.Method : PaymentMethod.CashOnDelivery,
                PaymentStatus = order.Payment != null ? order.Payment.Status : PaymentStatus.Pending,
                Items = order.Details.Select(detail => new OrderDetailItemViewModel
                {
                    ProductName = detail.ProductName,
                    UnitPrice = detail.UnitPrice,
                    Quantity = detail.Quantity,
                    LineTotal = detail.LineTotal
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string GetUserId(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Can dang nhap de dat hang.");
        }

        return userId;
    }
}
