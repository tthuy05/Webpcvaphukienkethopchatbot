using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class RecoveryCodeLoginViewModel
{
    [Required]
    [Display(Name = "Mã khôi phục")]
    public string RecoveryCode { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
