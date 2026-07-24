using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Controllers;

public sealed class CartController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOrderService _orderService;
    private readonly IGuestCartService _guestCartService;

    public CartController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IOrderService orderService,
        IGuestCartService guestCartService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _orderService = orderService;
        _guestCartService = guestCartService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(User.Identity?.IsAuthenticated == true
            ? await BuildCartViewModelAsync(GetUserId(), cancellationToken)
            : await BuildGuestCartViewModelAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1, CancellationToken cancellationToken = default)
    {
        if (quantity is < 1 or > 99)
        {
            TempData["ErrorMessage"] = "Số lượng phải từ 1 đến 99.";
            return RedirectToAction(nameof(Index));
        }

        var product = await _dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == productId && item.IsActive, cancellationToken);

        if (product is null)
        {
            TempData["ErrorMessage"] = "Sản phẩm không tồn tại hoặc đã ngừng bán.";
            return RedirectToAction("Index", "Home");
        }

        if (User.Identity?.IsAuthenticated != true)
        {
            _guestCartService.Add(productId, quantity, product.StockQuantity);
            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng tạm.";
            return RedirectToAction(nameof(Index));
        }

        var cart = await GetOrCreateCartAsync(GetUserId(), cancellationToken);
        var existingItem = cart.Items.SingleOrDefault(item => item.ProductId == productId);
        var requestedQuantity = (existingItem?.Quantity ?? 0) + quantity;

        if (requestedQuantity > product.StockQuantity)
        {
            TempData["ErrorMessage"] = $"Sản phẩm “{product.Name}” chỉ còn {product.StockQuantity} sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price
            });
        }
        else
        {
            existingItem.Quantity = requestedQuantity;
            existingItem.UnitPrice = product.Price;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int itemId, int quantity, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var product = await _dbContext.Products.AsNoTracking().SingleOrDefaultAsync(item => item.Id == itemId, cancellationToken);
            if (product is null) return NotFound();
            _guestCartService.Update(itemId, quantity, product.StockQuantity);
            TempData["SuccessMessage"] = "Đã cập nhật giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        var userId = GetUserId();
        var item = await _dbContext.CartItems
            .Include(cartItem => cartItem.Cart)
            .Include(cartItem => cartItem.Product)
            .SingleOrDefaultAsync(
                cartItem => cartItem.Id == itemId && cartItem.Cart!.ApplicationUserId == userId,
                cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        if (quantity <= 0)
        {
            _dbContext.CartItems.Remove(item);
        }
        else if (quantity > 99 || item.Product is null || !item.Product.IsActive || quantity > item.Product.StockQuantity)
        {
            TempData["ErrorMessage"] = item.Product is null
                ? "Sản phẩm không còn tồn tại."
                : $"Số lượng không hợp lệ. Sản phẩm hiện còn {item.Product.StockQuantity}.";
            return RedirectToAction(nameof(Index));
        }
        else
        {
            item.Quantity = quantity;
            item.UnitPrice = item.Product.Price;
        }

        item.Cart!.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Đã cập nhật giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int itemId, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            _guestCartService.Remove(itemId);
            return RedirectToAction(nameof(Index));
        }

        var userId = GetUserId();
        var item = await _dbContext.CartItems
            .Include(cartItem => cartItem.Cart)
            .SingleOrDefaultAsync(
                cartItem => cartItem.Id == itemId && cartItem.Cart!.ApplicationUserId == userId,
                cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        item.Cart!.UpdatedAt = DateTime.UtcNow;
        _dbContext.CartItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var cart = await BuildCartViewModelAsync(userId, cancellationToken);
        if (cart.Items.Count == 0)
        {
            TempData["ErrorMessage"] = "Giỏ hàng đang trống.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        return View(new CheckoutViewModel
        {
            ReceiverName = user?.FullName ?? string.Empty,
            PhoneNumber = user?.PhoneNumber ?? string.Empty,
            ShippingAddress = user?.Address ?? string.Empty,
            Cart = cart
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!ModelState.IsValid)
        {
            model.Cart = await BuildCartViewModelAsync(userId, cancellationToken);
            return View(model);
        }

        var result = await _orderService.CheckoutAsync(
            userId,
            model.ReceiverName,
            model.PhoneNumber,
            model.ShippingAddress,
            model.PaymentMethod,
            model.CouponCode,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            model.Cart = await BuildCartViewModelAsync(userId, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "Đặt hàng thành công. Tồn kho đã được cập nhật.";
        return RedirectToAction("Details", "Orders", new { id = result.OrderId });
    }

    private string GetUserId() => _userManager.GetUserId(User)
        ?? throw new InvalidOperationException("Authenticated user does not have an id.");

    private async Task<Cart> GetOrCreateCartAsync(string userId, CancellationToken cancellationToken)
    {
        var cart = await _dbContext.Carts
            .Include(currentCart => currentCart.Items)
            .SingleOrDefaultAsync(currentCart => currentCart.ApplicationUserId == userId, cancellationToken);

        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart
        {
            ApplicationUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private async Task<CartViewModel> BuildCartViewModelAsync(string userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var items = await _dbContext.CartItems
            .AsNoTracking()
            .Where(item => item.Cart!.ApplicationUserId == userId)
            .OrderBy(item => item.Id)
            .Select(item => new CartLineViewModel
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product!.Name,
                ImageUrl = item.Product.MainImageUrl,
                UnitPrice = item.Product.SalePrice.HasValue && item.Product.SalePrice < item.Product.Price &&
                            (!item.Product.SaleStartAt.HasValue || item.Product.SaleStartAt <= now) &&
                            (!item.Product.SaleEndAt.HasValue || item.Product.SaleEndAt >= now)
                    ? item.Product.SalePrice.Value
                    : item.Product.Price,
                Quantity = item.Quantity,
                StockQuantity = item.Product.StockQuantity,
                IsAvailable = item.Product.IsActive && item.Product.StockQuantity >= item.Quantity
            })
            .ToListAsync(cancellationToken);

        return new CartViewModel { Items = items };
    }

    private async Task<CartViewModel> BuildGuestCartViewModelAsync(CancellationToken cancellationToken)
    {
        var guestItems = _guestCartService.GetItems();
        if (guestItems.Count == 0) return new CartViewModel();
        var quantities = guestItems.ToDictionary(item => item.ProductId, item => item.Quantity);
        var productIds = quantities.Keys.ToList();
        var products = await _dbContext.Products.AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        return new CartViewModel
        {
            Items = products.Select(product => new CartLineViewModel
            {
                Id = product.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                ImageUrl = product.MainImageUrl,
                UnitPrice = product.GetCurrentPrice(now),
                Quantity = quantities[product.Id],
                StockQuantity = product.StockQuantity,
                IsAvailable = product.IsActive && product.StockQuantity >= quantities[product.Id]
            }).ToList()
        };
    }
}
