using System.Security.Claims;
using Webpcvaphukienkethopchatbot.Models;

namespace Webpcvaphukienkethopchatbot.Services;

public interface IUserPreferenceService
{
    Task<UserPreference?> GetForUserAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    Task SetLanguageAsync(ClaimsPrincipal user, string culture, CancellationToken cancellationToken = default);

    Task SetThemeAsync(ClaimsPrincipal user, string theme, CancellationToken cancellationToken = default);
}
