using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Services;

namespace Webpcvaphukienkethopchatbot.Controllers;

[Authorize]
public sealed class OrdersController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOrderService _orderService;

    public OrdersController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IOrderService orderService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.ApplicationUserId == GetUserId())
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(currentOrder => currentOrder.Details)
            .Include(currentOrder => currentOrder.Payment)
            .Include(currentOrder => currentOrder.StatusHistory.OrderBy(history => history.ChangedAt))
            .SingleOrDefaultAsync(
                currentOrder => currentOrder.Id == id && currentOrder.ApplicationUserId == GetUserId(),
                cancellationToken);

        return order is null ? NotFound() : View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Invoice(int id, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders.AsNoTracking()
            .Include(currentOrder => currentOrder.Details)
            .Include(currentOrder => currentOrder.Payment)
            .SingleOrDefaultAsync(currentOrder => currentOrder.Id == id && currentOrder.ApplicationUserId == GetUserId(), cancellationToken);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.CancelAsync(id, GetUserId(), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Đã hủy đơn và hoàn lại tồn kho."
            : result.ErrorMessage;

        return RedirectToAction(nameof(Details), new { id });
    }

    private string GetUserId() => _userManager.GetUserId(User)
        ?? throw new InvalidOperationException("Authenticated user does not have an id.");
}
