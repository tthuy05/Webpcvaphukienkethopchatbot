using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _orderService.GetOrdersForUserAsync(User, HttpContext.RequestAborted));
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderService.GetOrderDetailForUserAsync(User, id, HttpContext.RequestAborted);
        return order is null ? NotFound() : View(order);
    }

    public async Task<IActionResult> Checkout()
    {
        var model = await _orderService.BuildCheckoutAsync(User, HttpContext.RequestAborted);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var current = await _orderService.BuildCheckoutAsync(User, HttpContext.RequestAborted);
            model.Cart = current.Cart;
            return View(model);
        }

        try
        {
            var orderId = await _orderService.PlaceOrderAsync(User, model, HttpContext.RequestAborted);
            return RedirectToAction(nameof(Success), new { id = orderId });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            var current = await _orderService.BuildCheckoutAsync(User, HttpContext.RequestAborted);
            model.Cart = current.Cart;
            return View(model);
        }
    }

    public async Task<IActionResult> Success(int id)
    {
        var order = await _orderService.GetOrderDetailForUserAsync(User, id, HttpContext.RequestAborted);
        return order is null ? NotFound() : View(order);
    }
}
