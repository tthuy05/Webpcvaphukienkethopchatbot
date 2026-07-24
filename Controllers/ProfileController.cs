using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuditService _auditService;

    public ProfileController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
        _auditService = auditService;
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

        TempData["SuccessMessage"] = "Da cap nhat thong tin ca nhan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        await _auditService.WriteAsync("ChangePassword", user.Id, cancellationToken: cancellationToken);
        TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> TwoFactor()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(await BuildTwoFactorModelAsync(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableTwoFactor(TwoFactorSetupViewModel model, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var code = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var valid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            code);
        if (!valid)
        {
            ModelState.AddModelError(nameof(model.Code), "Mã xác thực không đúng.");
            return View("TwoFactor", await BuildTwoFactorModelAsync(user));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 5);
        var resultModel = await BuildTwoFactorModelAsync(user);
        resultModel.RecoveryCodes = codes?.ToList() ?? [];
        await _auditService.WriteAsync("Enable2FA", user.Id, cancellationToken: cancellationToken);
        return View("TwoFactor", resultModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTwoFactor(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _auditService.WriteAsync("Disable2FA", user.Id, cancellationToken: cancellationToken);
        TempData["SuccessMessage"] = "Đã tắt xác thực hai bước.";
        return RedirectToAction(nameof(TwoFactor));
    }

    [HttpGet]
    public async Task<IActionResult> VerifyPhone()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        return View(new PhoneVerificationViewModel
        {
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            IsConfirmed = user.PhoneNumberConfirmed
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPhoneCode()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            TempData["ErrorMessage"] = "Vui lòng cập nhật số điện thoại trước.";
            return RedirectToAction(nameof(VerifyPhone));
        }

        var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber);
        TempData["DemoPhoneCode"] = code;
        TempData["SuccessMessage"] = "Mã xác nhận demo đã được tạo.";
        return RedirectToAction(nameof(VerifyPhone));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPhone(PhoneVerificationViewModel model, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            TempData["ErrorMessage"] = "Vui lòng cập nhật số điện thoại trước.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, model.Code.Trim());
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(model.Code), "Mã xác nhận không đúng hoặc đã hết hạn.");
            model.PhoneNumber = user.PhoneNumber;
            model.IsConfirmed = user.PhoneNumberConfirmed;
            return View(model);
        }

        await _auditService.WriteAsync("ConfirmPhone", user.Id, cancellationToken: cancellationToken);
        TempData["SuccessMessage"] = "Đã xác nhận số điện thoại.";
        return RedirectToAction(nameof(VerifyPhone));
    }

    private async Task<TwoFactorSetupViewModel> BuildTwoFactorModelAsync(ApplicationUser user)
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        key ??= string.Empty;
        var email = user.Email ?? user.UserName ?? user.Id;
        return new TwoFactorSetupViewModel
        {
            IsEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
            SharedKey = string.Join(" ", Enumerable.Range(0, (key.Length + 3) / 4)
                .Select(index => key.Substring(index * 4, Math.Min(4, key.Length - index * 4)))),
            AuthenticatorUri = $"otpauth://totp/{Uri.EscapeDataString("PC Store")}:{Uri.EscapeDataString(email)}" +
                               $"?secret={key}&issuer={Uri.EscapeDataString("PC Store")}&digits=6"
        };
    }
}
