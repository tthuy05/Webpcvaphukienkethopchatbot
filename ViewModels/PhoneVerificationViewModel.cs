using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class PhoneVerificationViewModel
{
    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsConfirmed { get; set; }

    [Required]
    [StringLength(10, MinimumLength = 4)]
    [Display(Name = "Mã xác nhận")]
    public string Code { get; set; } = string.Empty;
}
