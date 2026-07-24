using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Models.Enums;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class CouponsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    public CouponsController(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _dbContext.Coupons.AsNoTracking().OrderByDescending(item => item.CreatedAt).ToListAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue) return View(new CouponAdminViewModel());
        var item = await _dbContext.Coupons.AsNoTracking().SingleOrDefaultAsync(coupon => coupon.Id == id, cancellationToken);
        return item is null ? NotFound() : View(new CouponAdminViewModel
        {
            Id = item.Id, Code = item.Code, Description = item.Description, DiscountType = item.DiscountType, Value = item.Value,
            MinimumOrderAmount = item.MinimumOrderAmount, MaximumDiscountAmount = item.MaximumDiscountAmount,
            UsageLimit = item.UsageLimit, StartsAt = item.StartsAt, EndsAt = item.EndsAt, IsActive = item.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CouponAdminViewModel model, CancellationToken cancellationToken)
    {
        if (model.DiscountType == DiscountType.Percentage && model.Value > 100)
            ModelState.AddModelError(nameof(model.Value), "Phần trăm giảm không được vượt quá 100.");
        if (model.StartsAt.HasValue && model.EndsAt.HasValue && model.EndsAt <= model.StartsAt)
            ModelState.AddModelError(nameof(model.EndsAt), "Ngày kết thúc phải sau ngày bắt đầu.");
        var code = model.Code.Trim().ToUpperInvariant();
        if (await _dbContext.Coupons.AnyAsync(item => item.Code == code && item.Id != model.Id, cancellationToken))
            ModelState.AddModelError(nameof(model.Code), "Mã giảm giá đã tồn tại.");
        if (!ModelState.IsValid) return View(model);

        Coupon entity;
        if (model.Id == 0)
        {
            entity = new Coupon { CreatedAt = DateTime.UtcNow };
            _dbContext.Coupons.Add(entity);
        }
        else
        {
            entity = await _dbContext.Coupons.SingleOrDefaultAsync(item => item.Id == model.Id, cancellationToken) ?? throw new InvalidOperationException("Coupon not found.");
        }
        entity.Code = code; entity.Description = model.Description?.Trim(); entity.DiscountType = model.DiscountType;
        entity.Value = model.Value; entity.MinimumOrderAmount = model.MinimumOrderAmount; entity.MaximumDiscountAmount = model.MaximumDiscountAmount;
        entity.UsageLimit = model.UsageLimit; entity.StartsAt = model.StartsAt?.ToUniversalTime(); entity.EndsAt = model.EndsAt?.ToUniversalTime(); entity.IsActive = model.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Đã lưu mã giảm giá.";
        return RedirectToAction(nameof(Index));
    }
}
