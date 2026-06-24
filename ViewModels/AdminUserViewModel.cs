namespace Webpcvaphukienkethopchatbot.ViewModels;

public class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool IsLocked { get; set; }

    public string Roles { get; set; } = string.Empty;
}
