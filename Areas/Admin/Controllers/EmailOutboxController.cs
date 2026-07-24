using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class EmailOutboxController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    public EmailOutboxController(ApplicationDbContext dbContext) => _dbContext = dbContext;
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _dbContext.EmailOutbox.AsNoTracking().OrderByDescending(item => item.CreatedAt).Take(200).ToListAsync(cancellationToken));
}
