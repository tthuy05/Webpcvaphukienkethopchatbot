using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models.Enums;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public class AdminOrderService : IAdminOrderService
{
    private readonly ApplicationDbContext _dbContext;

    public AdminOrderService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AdminOrderListItemViewModel>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.ApplicationUser)
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new AdminOrderListItemViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerEmail = order.ApplicationUser != null ? order.ApplicationUser.Email ?? string.Empty : string.Empty,
                CreatedAt = order.CreatedAt,
                Status = order.Status,
                TotalAmount = order.TotalAmount
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminOrderDetailViewModel?> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == id)
            .Select(order => new AdminOrderDetailViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CreatedAt = order.CreatedAt,
                Status = order.Status,
                ReceiverName = order.ReceiverName,
                PhoneNumber = order.PhoneNumber,
                ShippingAddress = order.ShippingAddress,
                TotalAmount = order.TotalAmount,
                CustomerEmail = order.ApplicationUser != null ? order.ApplicationUser.Email ?? string.Empty : string.Empty,
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

    public async Task UpdateStatusAsync(int id, OrderStatus status, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(status))
        {
            throw new InvalidOperationException("Trạng thái đơn hàng không hợp lệ.");
        }

        var order = await _dbContext.Orders.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        }

        if (order.Status == OrderStatus.Completed && status != OrderStatus.Completed)
        {
            throw new InvalidOperationException("Không thể sửa đơn hàng đã hoàn thành.");
        }

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
