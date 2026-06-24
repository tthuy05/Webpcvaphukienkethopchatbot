using System.Globalization;
using Webpcvaphukienkethopchatbot.Services;

namespace Webpcvaphukienkethopchatbot.Middleware;

public class UserPreferenceMiddleware
{
    private readonly RequestDelegate _next;

    public UserPreferenceMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var preferenceService = context.RequestServices.GetRequiredService<IUserPreferenceService>();
            var preference = await preferenceService.GetForUserAsync(context.User, context.RequestAborted);

            if (preference is not null)
            {
                var cultureName = preference.PreferredLanguage is "vi-VN" or "en-US" ? preference.PreferredLanguage : "vi-VN";
                var culture = new CultureInfo(cultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                context.Items["PreferredTheme"] = preference.PreferredTheme == "dark" ? "dark" : "light";
            }
        }

        await _next(context);
    }
}
