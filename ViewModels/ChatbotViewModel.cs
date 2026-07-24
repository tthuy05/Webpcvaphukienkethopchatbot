using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class ChatbotViewModel
{
    [Required, StringLength(1000, MinimumLength = 2)]
    [Display(Name = "Bạn đang cần sản phẩm như thế nào?")]
    public string Question { get; set; } = string.Empty;

    public string? Answer { get; set; }

    public IReadOnlyList<ProductCardViewModel> Recommendations { get; set; } = [];
}
