using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class TwoFactorLoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mã xác thực.")]
    [StringLength(7, MinimumLength = 6)]
    [Display(Name = "Mã xác thực")]
    public string Code { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    [Display(Name = "Ghi nhớ thiết bị này")]
    public bool RememberMachine { get; set; }

    public string? ReturnUrl { get; set; }
}
