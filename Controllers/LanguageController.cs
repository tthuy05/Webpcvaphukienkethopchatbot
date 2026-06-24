using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Webpcvaphukienkethopchatbot.Controllers;

public class LanguageController : Controller
{
    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "vi-VN",
        "en-US"
    };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string culture, string returnUrl = "/")
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

        if (!Url.IsLocalUrl(returnUrl))
        {
            returnUrl = Url.Action("Index", "Home") ?? "/";
        }

        return LocalRedirect(returnUrl);
    }
}
