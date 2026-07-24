using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Models.Enums;
using Webpcvaphukienkethopchatbot.Services;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class OrdersController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public OrdersController(ApplicationDbContext dbContext, IOrderService orderService, UserManager<ApplicationUser> userManager, IAuditService auditService)
    {
        _dbContext = dbContext; _orderService = orderService; _userManager = userManager; _auditService = auditService;
    }

    public async Task<IActionResult> Index(OrderStatus? status, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Orders.AsNoTracking().Include(order => order.ApplicationUser).AsQueryable();
        if (status.HasValue) query = query.Where(order => order.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(order => order.OrderNumber.Contains(search.Trim()) || order.ReceiverName.Contains(search.Trim()));
        ViewBag.Status = status; ViewBag.Search = search;
        return View(await query.OrderByDescending(order => order.CreatedAt).ToListAsync(cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders.AsNoTracking().Include(item => item.ApplicationUser).Include(item => item.Details)
            .Include(item => item.Payment).Include(item => item.StatusHistory.OrderBy(history => history.ChangedAt))
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus status, string? note, CancellationToken cancellationToken)
    {
        var actorId = _userManager.GetUserId(User)!;
        var result = await _orderService.ChangeStatusAsync(id, status, actorId, note, cancellationToken);
        await _auditService.WriteAsync("ChangeOrderStatus", actorId, "Order", id.ToString(), status.ToString(), result.Succeeded, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Đã cập nhật trạng thái đơn." : result.ErrorMessage;
        return RedirectToAction(nameof(Details), new { id });
    }
}
