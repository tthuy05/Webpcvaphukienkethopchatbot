using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class AuditController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    public AuditController(ApplicationDbContext dbContext) => _dbContext = dbContext;
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _dbContext.AuditLogs.AsNoTracking().OrderByDescending(item => item.CreatedAt).Take(500).ToListAsync(cancellationToken));
}
