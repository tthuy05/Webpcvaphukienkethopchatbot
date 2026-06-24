using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Webpcvaphukienkethopchatbot.Services;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _dashboardService.GetDashboardAsync(HttpContext.RequestAborted));
    }

    public async Task<IActionResult> RevenueByMonth()
    {
        return Json(await _dashboardService.GetRevenueByMonthAsync(HttpContext.RequestAborted));
    }
}
