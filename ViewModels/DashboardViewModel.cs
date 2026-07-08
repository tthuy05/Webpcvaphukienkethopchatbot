namespace Webpcvaphukienkethopchatbot.ViewModels;

public class DashboardViewModel
{
    public decimal TotalRevenue { get; set; }

    public int TotalOrders { get; set; }

    public int TodayOrders { get; set; }

    public int MonthOrders { get; set; }

    public IReadOnlyList<ProductStatisticViewModel> BestSellingProducts { get; set; } = new List<ProductStatisticViewModel>();

    public IReadOnlyList<ProductStatisticViewModel> LowStockProducts { get; set; } = new List<ProductStatisticViewModel>();

    public IReadOnlyList<RevenuePointViewModel> RevenueByDay { get; set; } = new List<RevenuePointViewModel>();

    public IReadOnlyList<DashboardMetricViewModel> OrdersByStatus { get; set; } = new List<DashboardMetricViewModel>();
}
