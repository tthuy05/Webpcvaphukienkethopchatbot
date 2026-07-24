using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly ApplicationDbContext _dbContext;

    public UsersController(UserManager<ApplicationUser> userManager, IAuditService auditService, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _auditService = auditService;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(user => user.Email).ToListAsync();
        var model = new List<AdminUserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new AdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                IsActive = user.IsActive,
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                Roles = string.Join(", ", roles)
            });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["ErrorMessage"] = "Admin khong the tu khoa tai khoan dang su dung.";
            return RedirectToAction(nameof(Index));
        }

        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var lockoutEnabledResult = await _userManager.SetLockoutEnabledAsync(user, true);
        if (!lockoutEnabledResult.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", lockoutEnabledResult.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Index));
        }

        var lockoutResult = await _userManager.SetLockoutEndDateAsync(
            user,
            isLocked ? null : DateTimeOffset.UtcNow.AddYears(50));
        if (!lockoutResult.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", lockoutResult.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = isLocked;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", updateResult.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Index));
        }

        var securityStampResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!securityStampResult.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", securityStampResult.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Index));
        }

        await _auditService.WriteAsync(isLocked ? "UnlockUser" : "LockUser", _userManager.GetUserId(User), "ApplicationUser", user.Id);
        await transaction.CommitAsync();
        TempData["SuccessMessage"] = isLocked ? "Da mo khoa tai khoan." : "Da khoa tai khoan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        if (role is not ("Admin" or "User"))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == _userManager.GetUserId(User) && role != "Admin")
        {
            TempData["ErrorMessage"] = "Admin không thể tự gỡ quyền quản trị của mình.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        if (currentRoles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join("; ", removeResult.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Index));
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", addResult.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Index));
        }

        await _userManager.UpdateSecurityStampAsync(user);
        await _auditService.WriteAsync("SetRole", _userManager.GetUserId(User), "ApplicationUser", user.Id, role);
        await transaction.CommitAsync();
        TempData["SuccessMessage"] = "Đã cập nhật vai trò người dùng.";
        return RedirectToAction(nameof(Index));
    }
}
