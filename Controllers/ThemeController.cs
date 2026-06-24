using Microsoft.AspNetCore.Mvc;

namespace Webpcvaphukienkethopchatbot.Controllers;

public class ThemeController : Controller
{
    private static readonly HashSet<string> SupportedThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "light",
        "dark"
    };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string theme, string returnUrl = "/")
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

        if (!Url.IsLocalUrl(returnUrl))
        {
            returnUrl = Url.Action("Index", "Home") ?? "/";
        }

        return LocalRedirect(returnUrl);
    }
}
