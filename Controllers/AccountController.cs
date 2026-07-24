using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Controllers;

public sealed class AccountController : Controller
{
    private const string LoginCaptcha = "login";
    private const string RegisterCaptcha = "register";

    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IDemoEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly ICaptchaService _captchaService;
    private readonly IGuestCartService _guestCartService;
    private readonly DemoFeaturesOptions _demoFeatures;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IDemoEmailService emailService,
        IAuditService auditService,
        ICaptchaService captchaService,
        IGuestCartService guestCartService,
        IOptions<DemoFeaturesOptions> demoFeatures,
        ILogger<AccountController> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _auditService = auditService;
        _captchaService = captchaService;
        _guestCartService = guestCartService;
        _demoFeatures = demoFeatures.Value;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(PrepareLoginModel(new LoginViewModel { ReturnUrl = returnUrl }));
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(PrepareLoginModel(model));
        }

        if (!_captchaService.Validate(LoginCaptcha, model.CaptchaAnswer))
        {
            ModelState.AddModelError(nameof(model.CaptchaAnswer), "Kết quả xác minh không đúng.");
            return View(PrepareLoginModel(model));
        }

        var email = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            await _auditService.WriteAsync("Login", details: email, succeeded: false, cancellationToken: cancellationToken);
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            return View(PrepareLoginModel(model));
        }

        if (!user.IsActive)
        {
            await _auditService.WriteAsync("Login", user.Id, details: "Inactive account", succeeded: false, cancellationToken: cancellationToken);
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
            return View(PrepareLoginModel(model));
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _guestCartService.MergeIntoUserCartAsync(user.Id, cancellationToken);
            await _auditService.WriteAsync("Login", user.Id, succeeded: true, cancellationToken: cancellationToken);
            _logger.LogInformation("User {UserId} logged in.", user.Id);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToAction(nameof(LoginWithTwoFactor), new
            {
                model.ReturnUrl,
                model.RememberMe
            });
        }

        await _auditService.WriteAsync("Login", user.Id, succeeded: false, cancellationToken: cancellationToken);
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản tạm khóa 15 phút do đăng nhập sai quá nhiều lần.");
        }
        else if (result.IsNotAllowed && !user.EmailConfirmed)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng xác nhận email trước khi đăng nhập.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
        }

        return View(PrepareLoginModel(model));
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> LoginWithTwoFactor(bool rememberMe, string? returnUrl = null)
    {
        if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new TwoFactorLoginViewModel { RememberMe = rememberMe, ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithTwoFactor(TwoFactorLoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var code = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            code,
            model.RememberMe,
            model.RememberMachine);

        if (result.Succeeded)
        {
            await _guestCartService.MergeIntoUserCartAsync(user.Id, cancellationToken);
            await _auditService.WriteAsync("Login2FA", user.Id, cancellationToken: cancellationToken);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đang tạm khóa.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Mã xác thực không đúng.");
        }

        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> LoginWithRecoveryCode(string? returnUrl = null)
    {
        if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
        {
            return RedirectToAction(nameof(Login));
        }
        return View(new RecoveryCodeLoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithRecoveryCode(RecoveryCodeLoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToAction(nameof(Login));
        var code = model.RecoveryCode.Replace(" ", string.Empty);
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Mã khôi phục không hợp lệ.");
            return View(model);
        }
        await _guestCartService.MergeIntoUserCartAsync(user.Id, cancellationToken);
        await _auditService.WriteAsync("LoginRecoveryCode", user.Id, cancellationToken: cancellationToken);
        return RedirectToLocal(model.ReturnUrl);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(PrepareRegisterModel(new RegisterViewModel()));
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(PrepareRegisterModel(model));
        }

        if (!_captchaService.Validate(RegisterCaptcha, model.CaptchaAnswer))
        {
            ModelState.AddModelError(nameof(model.CaptchaAnswer), "Kết quả xác minh không đúng.");
            return View(PrepareRegisterModel(model));
        }

        var email = model.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
            return View(PrepareRegisterModel(model));
        }

        await using var transaction = await BeginTransactionIfSupportedAsync();
        try
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = model.FullName.Trim(),
                PhoneNumber = NullIfWhiteSpace(model.PhoneNumber),
                Address = NullIfWhiteSpace(model.Address),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmed = false
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                AddIdentityErrors(createResult);
                return View(PrepareRegisterModel(model));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "User");
            if (!roleResult.Succeeded)
            {
                AddIdentityErrors(roleResult);
                return View(PrepareRegisterModel(model));
            }

            _dbContext.Carts.Add(new Cart
            {
                ApplicationUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            _dbContext.UserPreferences.Add(new UserPreference
            {
                ApplicationUserId = user.Id,
                PreferredLanguage = "vi-VN",
                PreferredTheme = "light",
                UpdatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);

            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationUrl = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new { userId = user.Id, token = confirmationToken },
                Request.Scheme)!;
            await _emailService.QueueAsync(
                email,
                "Xác nhận tài khoản PC Store",
                "Nhấn liên kết để xác nhận tài khoản của bạn.",
                confirmationUrl,
                cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            await _auditService.WriteAsync("Register", user.Id, "ApplicationUser", user.Id, email, cancellationToken: cancellationToken);
            _logger.LogInformation("New user {UserId} registered and persisted.", user.Id);
            return RedirectToAction(nameof(RegistrationComplete), new { email });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Registration failed for email {Email}.", email);
            ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản lúc này. Vui lòng thử lại.");
            return View(PrepareRegisterModel(model));
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> RegistrationComplete(string email, CancellationToken cancellationToken)
    {
        ViewBag.Email = email;
        ViewBag.ActionUrl = await GetDemoActionUrlAsync(email, "Xác nhận", cancellationToken);
        return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || string.IsNullOrWhiteSpace(token))
        {
            return View("EmailConfirmationResult", false);
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        await _auditService.WriteAsync(
            "ConfirmEmail",
            user.Id,
            "ApplicationUser",
            user.Id,
            succeeded: result.Succeeded,
            cancellationToken: cancellationToken);
        return View("EmailConfirmationResult", result.Succeeded);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null && user.IsActive && user.EmailConfirmed)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Action(nameof(ResetPassword), "Account", new { email, token }, Request.Scheme)!;
            await _emailService.QueueAsync(
                email,
                "Đặt lại mật khẩu PC Store",
                "Nhấn liên kết để đặt lại mật khẩu.",
                resetUrl,
                cancellationToken);
            await _auditService.WriteAsync("ForgotPassword", user.Id, cancellationToken: cancellationToken);
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation), new { email });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ForgotPasswordConfirmation(string email, CancellationToken cancellationToken)
    {
        ViewBag.ActionUrl = await GetDemoActionUrlAsync(email, "Đặt lại", cancellationToken);
        return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword(string email, string token) =>
        View(new ResetPasswordViewModel { Email = email, Token = token });

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        await _auditService.WriteAsync("ResetPassword", user.Id, cancellationToken: cancellationToken);
        TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        await _auditService.WriteAsync("Logout", userId, cancellationToken: cancellationToken);
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    private LoginViewModel PrepareLoginModel(LoginViewModel model)
    {
        model.CaptchaAnswer = null;
        model.CaptchaQuestion = _captchaService.CreateQuestion(LoginCaptcha);
        return model;
    }

    private RegisterViewModel PrepareRegisterModel(RegisterViewModel model)
    {
        model.CaptchaAnswer = null;
        model.CaptchaQuestion = _captchaService.CreateQuestion(RegisterCaptcha);
        return model;
    }

    private async Task<string?> GetDemoActionUrlAsync(
        string email,
        string subjectPrefix,
        CancellationToken cancellationToken)
    {
        if (!_demoFeatures.ShowEmailActionLinks || string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _dbContext.EmailOutbox
            .AsNoTracking()
            .Where(message => message.Recipient == email && message.Subject.StartsWith(subjectPrefix))
            .OrderByDescending(message => message.CreatedAt)
            .Select(message => message.ActionUrl)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync() =>
        _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync()
            : null;

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home")!;

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, TranslateIdentityError(error));
        }
    }

    private static string TranslateIdentityError(IdentityError error) => error.Code switch
    {
        "DuplicateEmail" or "DuplicateUserName" => "Email này đã được sử dụng.",
        "PasswordTooShort" => "Mật khẩu phải có ít nhất 8 ký tự.",
        "PasswordRequiresDigit" => "Mật khẩu phải có ít nhất một chữ số.",
        "PasswordRequiresLower" => "Mật khẩu phải có ít nhất một chữ thường.",
        "PasswordRequiresUpper" => "Mật khẩu phải có ít nhất một chữ hoa.",
        "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải có ít nhất một ký tự đặc biệt.",
        _ => error.Description
    };

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
