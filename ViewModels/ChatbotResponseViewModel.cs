namespace Webpcvaphukienkethopchatbot.ViewModels;

public class ChatbotResponseViewModel
{
    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public decimal? DetectedBudget { get; set; }

    public string? DetectedUsageNeed { get; set; }

    public IReadOnlyList<ChatbotProductSuggestionViewModel> Suggestions { get; set; } = new List<ChatbotProductSuggestionViewModel>();
}
