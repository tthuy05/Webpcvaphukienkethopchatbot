using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;

namespace Webpcvaphukienkethopchatbot.Controllers;

[Authorize]
public sealed class WishlistController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public WishlistController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;
        var items = await _dbContext.WishlistItems
            .AsNoTracking()
            .Include(item => item.Product)
            .Where(item => item.ApplicationUserId == userId && item.Product!.IsActive)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId, string? returnUrl, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;
        if (!await _dbContext.Products.AnyAsync(product => product.Id == productId && product.IsActive, cancellationToken))
        {
            return NotFound();
        }

        var item = await _dbContext.WishlistItems
            .SingleOrDefaultAsync(current => current.ApplicationUserId == userId && current.ProductId == productId, cancellationToken);
        if (item is null)
        {
            _dbContext.WishlistItems.Add(new WishlistItem
            {
                ApplicationUserId = userId,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            _dbContext.WishlistItems.Remove(item);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
