using System.Security.Claims;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public interface ICartService
{
    Task<CartViewModel> GetCartAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    Task AddToCartAsync(ClaimsPrincipal user, int productId, int quantity, CancellationToken cancellationToken = default);

    Task UpdateQuantityAsync(ClaimsPrincipal user, int cartItemId, int quantity, CancellationToken cancellationToken = default);

    Task RemoveItemAsync(ClaimsPrincipal user, int cartItemId, CancellationToken cancellationToken = default);
}
