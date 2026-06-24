using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Webpcvaphukienkethopchatbot.Services;

namespace Webpcvaphukienkethopchatbot.Controllers;

public class LanguageController : Controller
{
    private readonly IUserPreferenceService _userPreferenceService;

    public LanguageController(IUserPreferenceService userPreferenceService)
    {
        _userPreferenceService = userPreferenceService;
    }

    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "vi-VN",
        "en-US"
    };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Set(string culture, string returnUrl = "/")
    {
        if (!SupportedCultures.Contains(culture))
        {
            culture = "vi-VN";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        await _userPreferenceService.SetLanguageAsync(User, culture, HttpContext.RequestAborted);

        if (!Url.IsLocalUrl(returnUrl))
        {
            returnUrl = Url.Action("Index", "Home") ?? "/";
        }

        return LocalRedirect(returnUrl);
    }
}
