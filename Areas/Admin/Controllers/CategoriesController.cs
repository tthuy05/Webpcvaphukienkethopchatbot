using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class CategoriesController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    public CategoriesController(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _dbContext.Categories.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue) return View(new CatalogEntityViewModel());
        var entity = await _dbContext.Categories.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity is null ? NotFound() : View(new CatalogEntityViewModel { Id = entity.Id, Name = entity.Name, Slug = entity.Slug, Description = entity.Description, IsActive = entity.IsActive });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CatalogEntityViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var slug = SlugHelper.Generate(model.Slug ?? model.Name);
        if (await _dbContext.Categories.AnyAsync(item => item.Slug == slug && item.Id != model.Id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug đã tồn tại.");
            return View(model);
        }

        Category entity;
        if (model.Id == 0)
        {
            entity = new Category { CreatedAt = DateTime.UtcNow };
            _dbContext.Categories.Add(entity);
        }
        else
        {
            entity = await _dbContext.Categories.SingleOrDefaultAsync(item => item.Id == model.Id, cancellationToken) ?? throw new InvalidOperationException("Category not found.");
        }
        entity.Name = model.Name.Trim(); entity.Slug = slug; entity.Description = model.Description?.Trim(); entity.IsActive = model.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Đã lưu danh mục.";
        return RedirectToAction(nameof(Index));
    }
}
