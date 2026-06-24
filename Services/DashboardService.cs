using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models.Enums;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var completedOrders = _dbContext.Orders.AsNoTracking().Where(order => order.Status == OrderStatus.Completed);

        return new DashboardViewModel
        {
            TotalRevenue = await completedOrders.SumAsync(order => (decimal?)order.TotalAmount, cancellationToken) ?? 0,
            TotalOrders = await _dbContext.Orders.CountAsync(cancellationToken),
            TodayOrders = await _dbContext.Orders.CountAsync(order => order.CreatedAt >= today, cancellationToken),
            MonthOrders = await _dbContext.Orders.CountAsync(order => order.CreatedAt >= monthStart, cancellationToken),
            BestSellingProducts = await GetBestSellingProductsAsync(cancellationToken),
            LowStockProducts = await GetLowStockProductsAsync(cancellationToken),
            RevenueByDay = await GetRevenueByDayAsync(cancellationToken),
            OrdersByStatus = await GetOrdersByStatusAsync(cancellationToken)
        };
    }

    public async Task<IReadOnlyList<RevenuePointViewModel>> GetRevenueByMonthAsync(CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.Date.AddMonths(-11);
        var points = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Status == OrderStatus.Completed && order.CreatedAt >= from)
            .GroupBy(order => new { order.CreatedAt.Year, order.CreatedAt.Month })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Month)
            .Select(group => new
            {
                group.Key.Month,
                group.Key.Year,
                Revenue = group.Sum(order => order.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        return points
            .Select(point => new RevenuePointViewModel
            {
                Label = $"{point.Month:00}/{point.Year}",
                Revenue = point.Revenue
            })
            .ToList();
    }

    private async Task<IReadOnlyList<ProductStatisticViewModel>> GetBestSellingProductsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.OrderDetails
            .AsNoTracking()
            .GroupBy(detail => detail.ProductName)
            .Select(group => new ProductStatisticViewModel
            {
                ProductName = group.Key,
                Quantity = group.Sum(detail => detail.Quantity),
                Revenue = group.Sum(detail => detail.LineTotal)
            })
            .OrderByDescending(item => item.Quantity)
            .Take(5)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ProductStatisticViewModel>> GetLowStockProductsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(product => product.StockQuantity <= 5)
            .OrderBy(product => product.StockQuantity)
            .Select(product => new ProductStatisticViewModel
            {
                ProductName = product.Name,
                Quantity = product.StockQuantity,
                Revenue = product.Price
            })
            .Take(5)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<RevenuePointViewModel>> GetRevenueByDayAsync(CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.Date.AddDays(-13);
        var points = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Status == OrderStatus.Completed && order.CreatedAt >= from)
            .GroupBy(order => order.CreatedAt.Date)
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                Day = group.Key,
                Revenue = group.Sum(order => order.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        return points
            .Select(point => new RevenuePointViewModel
            {
                Label = point.Day.ToString("dd/MM"),
                Revenue = point.Revenue
            })
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardMetricViewModel>> GetOrdersByStatusAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .GroupBy(order => order.Status)
            .Select(group => new DashboardMetricViewModel
            {
                Label = group.Key.ToString(),
                Value = group.Count()
            })
            .ToListAsync(cancellationToken);
    }
}
