using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class TwoFactorSetupViewModel
{
    public bool IsEnabled { get; set; }

    public string SharedKey { get; set; } = string.Empty;

    public string AuthenticatorUri { get; set; } = string.Empty;

    [StringLength(7, MinimumLength = 6)]
    [Display(Name = "Mã từ ứng dụng xác thực")]
    public string Code { get; set; } = string.Empty;

    public IReadOnlyList<string> RecoveryCodes { get; set; } = [];
}
