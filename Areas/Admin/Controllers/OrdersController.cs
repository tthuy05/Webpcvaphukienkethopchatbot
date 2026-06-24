using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Webpcvaphukienkethopchatbot.Models.Enums;
using Webpcvaphukienkethopchatbot.Services;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly IAdminOrderService _adminOrderService;

    public OrdersController(IAdminOrderService adminOrderService)
    {
        _adminOrderService = adminOrderService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _adminOrderService.GetOrdersAsync(HttpContext.RequestAborted));
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _adminOrderService.GetDetailAsync(id, HttpContext.RequestAborted);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        try
        {
            await _adminOrderService.UpdateStatusAsync(id, status, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Da cap nhat trang thai don hang.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
