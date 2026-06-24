using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RevenuePointViewModel>> GetRevenueByMonthAsync(CancellationToken cancellationToken = default);
}
