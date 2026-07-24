using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class ReviewsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    public ReviewsController(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(
        await _dbContext.ProductReviews.AsNoTracking().Include(item => item.Product).Include(item => item.ApplicationUser)
            .OrderByDescending(item => item.CreatedAt).Take(500).ToListAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
    {
        var review = await _dbContext.ProductReviews.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (review is null) return NotFound();
        review.IsVisible = !review.IsVisible;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
