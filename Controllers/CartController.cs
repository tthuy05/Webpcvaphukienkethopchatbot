using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Webpcvaphukienkethopchatbot.Services;

namespace Webpcvaphukienkethopchatbot.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _cartService.GetCartAsync(User, HttpContext.RequestAborted));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        try
        {
            await _cartService.AddToCartAsync(User, productId, quantity, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int cartItemId, int quantity)
    {
        try
        {
            await _cartService.UpdateQuantityAsync(User, cartItemId, quantity, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Đã cập nhật giỏ hàng.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        await _cartService.RemoveItemAsync(User, cartItemId, HttpContext.RequestAborted);
        TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }
}
