using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Models.Enums;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Controllers;

public sealed class ProductsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProductsController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [AllowAnonymous]
    [HttpGet("san-pham/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Brand)
            .SingleOrDefaultAsync(item => item.Slug == slug && item.IsActive, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        var reviews = await _dbContext.ProductReviews
            .AsNoTracking()
            .Include(review => review.ApplicationUser)
            .Where(review => review.ProductId == product.Id && review.IsVisible)
            .OrderByDescending(review => review.CreatedAt)
            .ToListAsync(cancellationToken);
        var canReview = userId is not null && await _dbContext.OrderDetails
            .AnyAsync(detail =>
                detail.ProductId == product.Id &&
                detail.Order!.ApplicationUserId == userId &&
                detail.Order.Status == OrderStatus.Completed,
                cancellationToken);
        var isWishlisted = userId is not null && await _dbContext.WishlistItems
            .AnyAsync(item => item.ProductId == product.Id && item.ApplicationUserId == userId, cancellationToken);

        var existingReview = reviews.FirstOrDefault(review => review.ApplicationUserId == userId);
        return View(new ProductDetailsViewModel
        {
            Product = product,
            CurrentPrice = product.GetCurrentPrice(DateTime.UtcNow),
            Reviews = reviews,
            AverageRating = reviews.Count == 0 ? null : reviews.Average(review => review.Rating),
            CanReview = canReview,
            IsWishlisted = isWishlisted,
            Rating = existingReview?.Rating ?? 5,
            Comment = existingReview?.Comment ?? string.Empty
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int productId, int rating, string comment, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;
        var product = await _dbContext.Products.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == productId && item.IsActive, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var verified = await _dbContext.OrderDetails.AnyAsync(detail =>
            detail.ProductId == productId &&
            detail.Order!.ApplicationUserId == userId &&
            detail.Order.Status == OrderStatus.Completed,
            cancellationToken);
        if (!verified)
        {
            TempData["ErrorMessage"] = "Bạn chỉ có thể đánh giá sản phẩm đã mua và hoàn thành đơn.";
            return RedirectToAction(nameof(Details), new { product.Slug });
        }

        comment = comment?.Trim() ?? string.Empty;
        if (rating is < 1 or > 5 || comment.Length is < 3 or > 1000)
        {
            TempData["ErrorMessage"] = "Đánh giá phải có 1–5 sao và nội dung từ 3 đến 1000 ký tự.";
            return RedirectToAction(nameof(Details), new { product.Slug });
        }

        var review = await _dbContext.ProductReviews
            .SingleOrDefaultAsync(item => item.ProductId == productId && item.ApplicationUserId == userId, cancellationToken);
        if (review is null)
        {
            _dbContext.ProductReviews.Add(new ProductReview
            {
                ProductId = productId,
                ApplicationUserId = userId,
                Rating = rating,
                Comment = comment,
                IsVerifiedPurchase = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            review.Rating = rating;
            review.Comment = comment;
            review.UpdatedAt = DateTime.UtcNow;
            review.IsVisible = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Đã lưu đánh giá của bạn.";
        return RedirectToAction(nameof(Details), new { product.Slug });
    }
}
