using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class InventoryController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    public InventoryController(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<IActionResult> Index(int? productId, CancellationToken cancellationToken)
    {
        var query = _dbContext.InventoryTransactions.AsNoTracking().Include(item => item.Product).AsQueryable();
        if (productId.HasValue) query = query.Where(item => item.ProductId == productId.Value);
        return View(await query.OrderByDescending(item => item.CreatedAt).Take(500).ToListAsync(cancellationToken));
    }
}
