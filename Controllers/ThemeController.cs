using Microsoft.AspNetCore.Mvc;
using Webpcvaphukienkethopchatbot.Services;

namespace Webpcvaphukienkethopchatbot.Controllers;

public class ThemeController : Controller
{
    private readonly IUserPreferenceService _userPreferenceService;

    public ThemeController(IUserPreferenceService userPreferenceService)
    {
        _userPreferenceService = userPreferenceService;
    }

    private static readonly HashSet<string> SupportedThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "light",
        "dark"
    };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Set(string theme, string returnUrl = "/")
    {
        if (!SupportedThemes.Contains(theme))
        {
            theme = "light";
        }

        Response.Cookies.Append(
            "preferred-theme",
            theme,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        await _userPreferenceService.SetThemeAsync(User, theme, HttpContext.RequestAborted);

        if (!Url.IsLocalUrl(returnUrl))
        {
            returnUrl = Url.Action("Index", "Home") ?? "/";
        }

        return LocalRedirect(returnUrl);
    }
}
