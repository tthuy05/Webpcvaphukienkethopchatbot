using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _dbContext;

    public CartService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CartViewModel> GetCartAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(user);
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);

        var items = await _dbContext.CartItems
            .AsNoTracking()
            .Include(item => item.Product)
            .Where(item => item.CartId == cart.Id)
            .OrderBy(item => item.Product != null ? item.Product.Name : string.Empty)
            .Select(item => new CartItemViewModel
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product != null ? item.Product.Name : string.Empty,
                ImageUrl = item.Product != null ? item.Product.MainImageUrl : string.Empty,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                StockQuantity = item.Product != null ? item.Product.StockQuantity : 0
            })
            .ToListAsync(cancellationToken);

        return new CartViewModel { Items = items };
    }

    public async Task AddToCartAsync(ClaimsPrincipal user, int productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("So luong phai lon hon 0.");
        }

        var product = await _dbContext.Products.FirstOrDefaultAsync(item => item.Id == productId, cancellationToken);
        if (product is null || !product.IsActive)
        {
            throw new InvalidOperationException("San pham khong kha dung.");
        }

        if (product.StockQuantity < quantity)
        {
            throw new InvalidOperationException("So luong vuot qua ton kho.");
        }

        var cart = await GetOrCreateCartAsync(GetUserId(user), cancellationToken);
        var item = await _dbContext.CartItems.FirstOrDefaultAsync(cartItem => cartItem.CartId == cart.Id && cartItem.ProductId == productId, cancellationToken);

        if (item is null)
        {
            _dbContext.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price
            });
        }
        else
        {
            var nextQuantity = item.Quantity + quantity;
            if (nextQuantity > product.StockQuantity)
            {
                throw new InvalidOperationException("So luong vuot qua ton kho.");
            }

            item.Quantity = nextQuantity;
            item.UnitPrice = product.Price;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateQuantityAsync(ClaimsPrincipal user, int cartItemId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("So luong phai lon hon 0.");
        }

        var userId = GetUserId(user);
        var item = await _dbContext.CartItems
            .Include(cartItem => cartItem.Cart)
            .Include(cartItem => cartItem.Product)
            .FirstOrDefaultAsync(cartItem => cartItem.Id == cartItemId && cartItem.Cart != null && cartItem.Cart.ApplicationUserId == userId, cancellationToken);

        if (item is null || item.Product is null)
        {
            throw new InvalidOperationException("Khong tim thay san pham trong gio hang.");
        }

        if (!item.Product.IsActive || quantity > item.Product.StockQuantity)
        {
            throw new InvalidOperationException("So luong khong hop le hoac vuot ton kho.");
        }

        item.Quantity = quantity;
        item.UnitPrice = item.Product.Price;
        item.Cart!.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveItemAsync(ClaimsPrincipal user, int cartItemId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(user);
        var item = await _dbContext.CartItems
            .Include(cartItem => cartItem.Cart)
            .FirstOrDefaultAsync(cartItem => cartItem.Id == cartItemId && cartItem.Cart != null && cartItem.Cart.ApplicationUserId == userId, cancellationToken);

        if (item is null)
        {
            return;
        }

        _dbContext.CartItems.Remove(item);
        item.Cart!.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Cart> GetOrCreateCartAsync(string userId, CancellationToken cancellationToken)
    {
        var cart = await _dbContext.Carts.FirstOrDefaultAsync(item => item.ApplicationUserId == userId, cancellationToken);
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

    private static string GetUserId(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Can dang nhap de su dung gio hang.");
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Khong xac dinh duoc nguoi dung.");
        }

        return userId;
    }
}
