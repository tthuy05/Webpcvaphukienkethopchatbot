using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;

namespace Webpcvaphukienkethopchatbot.Services;

public class UserPreferenceService : IUserPreferenceService
{
    private readonly ApplicationDbContext _dbContext;

    public UserPreferenceService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserPreference?> GetForUserAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(user);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult<UserPreference?>(null);
        }

        return _dbContext.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(preference => preference.ApplicationUserId == userId, cancellationToken);
    }

    public async Task SetLanguageAsync(ClaimsPrincipal user, string culture, CancellationToken cancellationToken = default)
    {
        if (culture is not ("vi-VN" or "en-US"))
        {
            culture = "vi-VN";
        }

        var preference = await GetOrCreateTrackedAsync(user, cancellationToken);
        if (preference is null)
        {
            return;
        }

        preference.PreferredLanguage = culture;
        preference.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetThemeAsync(ClaimsPrincipal user, string theme, CancellationToken cancellationToken = default)
    {
        if (theme is not ("light" or "dark"))
        {
            theme = "light";
        }

        var preference = await GetOrCreateTrackedAsync(user, cancellationToken);
        if (preference is null)
        {
            return;
        }

        preference.PreferredTheme = theme;
        preference.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserPreference?> GetOrCreateTrackedAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var preference = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(item => item.ApplicationUserId == userId, cancellationToken);

        if (preference is not null)
        {
            return preference;
        }

        preference = new UserPreference
        {
            ApplicationUserId = userId,
            PreferredLanguage = "vi-VN",
            PreferredTheme = "light",
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.UserPreferences.Add(preference);
        return preference;
    }

    private static string? GetUserId(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true
            ? user.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
    }
}
