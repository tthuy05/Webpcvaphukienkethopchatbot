using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;
}
