using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Models.Enums;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class ProductsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public ProductsController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Products.AsNoTracking().Include(product => product.Category).Include(product => product.Brand).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(product => product.Name.Contains(search.Trim()));
        }
        return View(await query.OrderBy(product => product.Name).ToListAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await PopulateLookupsAsync(cancellationToken);
        return View("Edit", new ProductAdminViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductAdminViewModel model, CancellationToken cancellationToken)
    {
        ValidateProduct(model);
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(cancellationToken);
            return View("Edit", model);
        }

        var slug = await EnsureUniqueSlugAsync(model.Slug ?? model.Name, null, cancellationToken);
        var product = new Product { CreatedAt = DateTime.UtcNow, StockQuantity = model.InitialStock, SoldQuantity = 0 };
        Map(model, product, slug);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (model.InitialStock > 0)
        {
            _dbContext.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                Type = InventoryTransactionType.StockReceipt,
                QuantityDelta = model.InitialStock,
                StockBefore = 0,
                StockAfter = model.InitialStock,
                ReferenceType = "Product",
                ReferenceId = product.Id.ToString(),
                Note = "Tồn kho ban đầu",
                PerformedByUserId = _userManager.GetUserId(User),
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await _auditService.WriteAsync("CreateProduct", _userManager.GetUserId(User), "Product", product.Id.ToString(), product.Name, cancellationToken: cancellationToken);
        TempData["SuccessMessage"] = "Đã tạo sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null) return NotFound();
        await PopulateLookupsAsync(cancellationToken);
        return View(new ProductAdminViewModel
        {
            Id = product.Id, Name = product.Name, Slug = product.Slug, CategoryId = product.CategoryId, BrandId = product.BrandId,
            Price = product.Price, SalePrice = product.SalePrice, SaleStartAt = product.SaleStartAt, SaleEndAt = product.SaleEndAt,
            MainImageUrl = product.MainImageUrl, Cpu = product.Cpu, Ram = product.Ram, Ssd = product.Ssd, Gpu = product.Gpu,
            Screen = product.Screen, Battery = product.Battery, Weight = product.Weight, OperatingSystem = product.OperatingSystem,
            Description = product.Description, SuitableNeeds = product.SuitableNeeds, IsActive = product.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductAdminViewModel model, CancellationToken cancellationToken)
    {
        ValidateProduct(model);
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(cancellationToken);
            return View(model);
        }

        var product = await _dbContext.Products.SingleOrDefaultAsync(item => item.Id == model.Id, cancellationToken);
        if (product is null) return NotFound();
        var slug = await EnsureUniqueSlugAsync(model.Slug ?? model.Name, model.Id, cancellationToken);
        Map(model, product, slug);
        product.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("UpdateProduct", _userManager.GetUserId(User), "Product", product.Id.ToString(), product.Name, cancellationToken: cancellationToken);
        TempData["SuccessMessage"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(int id, int quantityDelta, string? note, CancellationToken cancellationToken)
    {
        if (quantityDelta == 0 || quantityDelta is < -100000 or > 100000)
        {
            TempData["ErrorMessage"] = "Số lượng điều chỉnh không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var product = await _dbContext.Products.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null) return NotFound();
        if (product.StockQuantity + quantityDelta < 0)
        {
            TempData["ErrorMessage"] = "Không thể điều chỉnh làm tồn kho âm.";
            return RedirectToAction(nameof(Index));
        }

        var affected = await _dbContext.Products.Where(item => item.Id == id && item.StockQuantity + quantityDelta >= 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.StockQuantity, item => item.StockQuantity + quantityDelta)
                .SetProperty(item => item.UpdatedAt, DateTime.UtcNow), cancellationToken);
        if (affected != 1) throw new DbUpdateConcurrencyException("Stock changed concurrently.");

        _dbContext.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = id,
            Type = quantityDelta > 0 ? InventoryTransactionType.StockReceipt : InventoryTransactionType.ManualAdjustment,
            QuantityDelta = quantityDelta,
            StockBefore = product.StockQuantity,
            StockAfter = product.StockQuantity + quantityDelta,
            ReferenceType = "AdminAdjustment",
            ReferenceId = id.ToString(),
            Note = string.IsNullOrWhiteSpace(note) ? "Điều chỉnh kho" : note.Trim(),
            PerformedByUserId = _userManager.GetUserId(User),
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await _auditService.WriteAsync("AdjustStock", _userManager.GetUserId(User), "Product", id.ToString(), quantityDelta.ToString(), cancellationToken: cancellationToken);
        TempData["SuccessMessage"] = "Đã cập nhật tồn kho.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.Categories = await _dbContext.Categories.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken);
        ViewBag.Brands = await _dbContext.Brands.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken);
    }

    private void ValidateProduct(ProductAdminViewModel model)
    {
        if (model.SalePrice.HasValue && model.SalePrice.Value >= model.Price)
            ModelState.AddModelError(nameof(model.SalePrice), "Giá khuyến mãi phải thấp hơn giá niêm yết.");
        if (model.SaleStartAt.HasValue && model.SaleEndAt.HasValue && model.SaleEndAt <= model.SaleStartAt)
            ModelState.AddModelError(nameof(model.SaleEndAt), "Thời gian kết thúc phải sau thời gian bắt đầu.");
    }

    private async Task<string> EnsureUniqueSlugAsync(string source, int? currentId, CancellationToken cancellationToken)
    {
        var baseSlug = SlugHelper.Generate(source);
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "san-pham";
        var slug = baseSlug;
        var suffix = 2;
        while (await _dbContext.Products.AnyAsync(item => item.Slug == slug && item.Id != currentId, cancellationToken))
            slug = $"{baseSlug}-{suffix++}";
        return slug;
    }

    private static void Map(ProductAdminViewModel source, Product target, string slug)
    {
        target.Name = source.Name.Trim(); target.Slug = slug; target.CategoryId = source.CategoryId; target.BrandId = source.BrandId;
        target.Price = source.Price; target.SalePrice = source.SalePrice; target.SaleStartAt = source.SaleStartAt?.ToUniversalTime();
        target.SaleEndAt = source.SaleEndAt?.ToUniversalTime(); target.MainImageUrl = source.MainImageUrl.Trim();
        target.Cpu = source.Cpu?.Trim(); target.Ram = source.Ram?.Trim(); target.Ssd = source.Ssd?.Trim(); target.Gpu = source.Gpu?.Trim();
        target.Screen = source.Screen?.Trim(); target.Battery = source.Battery?.Trim(); target.Weight = source.Weight?.Trim();
        target.OperatingSystem = source.OperatingSystem?.Trim(); target.Description = source.Description.Trim();
        target.SuitableNeeds = source.SuitableNeeds.Trim(); target.IsActive = source.IsActive;
    }
}
