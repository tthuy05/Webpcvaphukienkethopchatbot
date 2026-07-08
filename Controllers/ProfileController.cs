using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var model = new ProfileViewModel
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            RecentOrders = await _dbContext.Orders
                .Where(order => order.ApplicationUserId == user.Id)
                .OrderByDescending(order => order.CreatedAt)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            model.RecentOrders = await _dbContext.Orders
                .Where(order => order.ApplicationUserId == user.Id)
                .OrderByDescending(order => order.CreatedAt)
                .Take(5)
                .ToListAsync();
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();
        user.Address = model.Address?.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = "Đã cập nhật thông tin cá nhân.";
        return RedirectToAction(nameof(Index));
    }
}
